using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Shared.Messaging;

namespace Orders.Api.Services;

public class OrderEventsSubscriber : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderEventsSubscriber> _logger;
    private readonly OrderStore _orderStore;
    private readonly OrderReadCache _orderReadCache;
    private readonly ProcessedEventStore _processedEventStore;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public OrderEventsSubscriber(
        IConfiguration configuration,
        ILogger<OrderEventsSubscriber> logger,
        OrderStore orderStore,
        OrderReadCache orderReadCache,
        ProcessedEventStore processedEventStore)
    {
        _configuration = configuration;
        _logger = logger;
        _orderStore = orderStore;
        _orderReadCache = orderReadCache;
        _processedEventStore = processedEventStore;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration["ServiceBus:ConnectionString"];
        var topicName = _configuration["ServiceBus:TopicName"];
        var subscriptionName = _configuration["ServiceBus:SubscriptionName"];

        if (string.IsNullOrWhiteSpace(connectionString) ||
            string.IsNullOrWhiteSpace(topicName) ||
            string.IsNullOrWhiteSpace(subscriptionName))
        {
            _logger.LogWarning("Order event subscriber is disabled because Service Bus settings are missing.");
            return;
        }

        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions());
        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken);

        _logger.LogInformation("Order event subscriber started for subscription {SubscriptionName}.", subscriptionName);

        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        string? eventKey = null;

        try
        {
            var message = args.Message.Body.ToString();
            var envelope = JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(message);

            if (envelope == null)
            {
                _logger.LogWarning("Received an invalid event message.");
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            eventKey = $"{envelope.EventType}:{envelope.CorrelationId}";

            if (!_processedEventStore.TryBegin(eventKey))
            {
                _logger.LogInformation("Skipping duplicate order status event {EventKey}", eventKey);
                await args.CompleteMessageAsync(args.Message);
                return;
            }

            var updated = envelope.EventType switch
            {
                "InventoryReserved" => await UpdateOrderStatusAsync<InventoryReservedEvent>(envelope, "Inventory Reserved"),
                "PaymentSucceeded" => await UpdateOrderStatusAsync<PaymentSucceededEvent>(envelope, "Payment Succeeded"),
                "PaymentFailed" => await UpdateOrderStatusAsync<PaymentFailedEvent>(envelope, "Payment Failed"),
                "InventoryReleased" => await UpdateOrderStatusAsync<InventoryReleasedEvent>(envelope, "Inventory Released"),
                _ => false
            };

            if (!updated)
            {
                _logger.LogInformation("Ignored event type {EventType} for order status updates.", envelope.EventType);
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            if (eventKey != null)
            {
                _processedEventStore.MarkFailed(eventKey);
            }

            _logger.LogError(ex, "Order event subscriber error while processing a message.");
            throw;
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Order event subscriber error from source {ErrorSource}.", args.ErrorSource);
        return Task.CompletedTask;
    }

    private async Task<bool> UpdateOrderStatusAsync<T>(EventEnvelope<JsonElement> envelope, string status) where T : class
    {
        var data = envelope.Data.Deserialize<T>();

        var orderId = data switch
        {
            InventoryReservedEvent inventoryReserved => inventoryReserved.OrderId,
            PaymentSucceededEvent paymentSucceeded => paymentSucceeded.OrderId,
            PaymentFailedEvent paymentFailed => paymentFailed.OrderId,
            InventoryReleasedEvent inventoryReleased => inventoryReleased.OrderId,
            _ => Guid.Empty
        };

        if (orderId == Guid.Empty)
        {
            _logger.LogWarning("Could not resolve order id for event type {EventType}.", envelope.EventType);
            return false;
        }

        var updatedOrder = _orderStore.UpdateStatus(orderId, status);

        if (updatedOrder == null)
        {
            return false;
        }

        await _orderReadCache.RefreshAsync(updatedOrder, _orderStore.GetAll());
        _logger.LogInformation("Order {OrderId} updated to status {Status}.", orderId, status);

        return true;
    }
}