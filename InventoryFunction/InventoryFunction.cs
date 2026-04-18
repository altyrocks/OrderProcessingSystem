using System.Text.Json;
using Shared.Messaging;
using Inventory.Function.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace Inventory.Function;

public class InventoryFunction
{
    private readonly ILogger _logger;
    private readonly ProcessedEventStore _processedEventStore;
    private readonly ServiceBusPublisher _publisher;

    public InventoryFunction(
        ILoggerFactory loggerFactory,
        ProcessedEventStore processedEventStore,
        ServiceBusPublisher publisher)
    {
        _logger = loggerFactory.CreateLogger<InventoryFunction>();
        _processedEventStore = processedEventStore;
        _publisher = publisher;
    }

    [Function("InventoryFunction")]
    public async Task Run(
        [ServiceBusTrigger("orders", "inventory-sub", Connection = "ServiceBusConnection")]
        string message)
    {
        string? eventKey = null;

        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(message);

            if (envelope == null)
            {
                _logger.LogWarning("Invalid message format");
                return;
            }

            eventKey = $"{envelope.EventType}:{envelope.CorrelationId}";

            if (!_processedEventStore.TryBegin(eventKey))
            {
                _logger.LogInformation("Skipping duplicate inventory event {EventKey}", eventKey);
                return;
            }

            _logger.LogInformation("CorrelationId: {CorrelationId}", envelope.CorrelationId);

            switch (envelope.EventType)
            {
                case "OrderCreated":
                    var order = envelope.Data.Deserialize<OrderCreatedEvent>();

                    _logger.LogInformation("Reserving inventory for Order: {OrderId}", order?.OrderId);
                    _logger.LogInformation("Product: {ProductName}, Qty: {Quantity}", order?.ProductName, order?.Quantity);

                    if (order == null)
                    {
                        _logger.LogWarning("OrderCreated event data was missing");
                        return;
                    }

                    var reservedEnvelope = new EventEnvelope<InventoryReservedEvent>
                    {
                        EventType = "InventoryReserved",
                        CorrelationId = envelope.CorrelationId,
                        Data = new InventoryReservedEvent
                        {
                            OrderId = order.OrderId,
                            ProductName = order.ProductName,
                            Quantity = order.Quantity
                        }
                    };

                    await _publisher.PublishAsync(reservedEnvelope);
                    _logger.LogInformation("Inventory reserved for Order: {OrderId}", order.OrderId);
                    break;

                case "PaymentFailed":
                    var failed = envelope.Data.Deserialize<PaymentFailedEvent>();

                    _logger.LogInformation("Releasing inventory for Order: {OrderId}", failed?.OrderId);
                    _logger.LogInformation("Reason: {Reason}", failed?.Reason);

                    if (failed == null)
                    {
                        _logger.LogWarning("PaymentFailed event data was missing");
                        return;
                    }

                    var releasedEnvelope = new EventEnvelope<InventoryReleasedEvent>
                    {
                        EventType = "InventoryReleased",
                        CorrelationId = envelope.CorrelationId,
                        Data = new InventoryReleasedEvent
                        {
                            OrderId = failed.OrderId,
                            Reason = failed.Reason
                        }
                    };

                    await _publisher.PublishAsync(releasedEnvelope);
                    _logger.LogInformation("Inventory released for Order: {OrderId}", failed.OrderId);
                    break;

                default:
                    _logger.LogInformation("Ignoring non-relevant event");
                    break;
            }
        }
        catch (Exception ex)
        {
            if (eventKey != null)
            {
                _processedEventStore.MarkFailed(eventKey);
            }

            _logger.LogError(ex, "Error processing inventory event");

            throw;
        }
    }
}