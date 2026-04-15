using System.Text.Json;
using Shared.Messaging;
using Inventory.Function.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace Inventory.Function;

public class InventoryFunction
{
    private readonly ILogger _logger;
    private readonly ServiceBusPublisher _publisher;

    public InventoryFunction(ILoggerFactory loggerFactory, ServiceBusPublisher publisher)
    {
        _logger = loggerFactory.CreateLogger<InventoryFunction>();
        _publisher = publisher;
    }

    [Function("InventoryFunction")]
    public async Task Run(
        [ServiceBusTrigger("orders", "inventory-sub", Connection = "ServiceBusConnection")]
        string message)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(message);

            if (envelope == null)
            {
                _logger.LogWarning("Invalid message format");

                return;
            }

            _logger.LogInformation($"CorrelationId: {envelope.CorrelationId}");

            switch (envelope.EventType)
            {
                case "OrderCreated":
                    var order = envelope.Data.Deserialize<OrderCreatedEvent>();

                    _logger.LogInformation($"Reserving inventory for Order: {order?.OrderId}");
                    _logger.LogInformation($"Product: {order?.ProductName}, Qty: {order?.Quantity}");

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

                    _logger.LogInformation($"Inventory reserved for Order: {order.OrderId}");

                    break;

                case "PaymentFailed":
                    var failed = envelope.Data.Deserialize<PaymentFailedEvent>();

                    _logger.LogInformation($"Releasing inventory for Order: {failed?.OrderId}");
                    _logger.LogInformation($"Reason: {failed?.Reason}");

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

                    _logger.LogInformation($"Inventory released for Order: {failed.OrderId}");

                    break;

                default:
                    _logger.LogInformation("Ignoring non-relevant event");

                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error processing message: {ex.Message}");
        }
    }
}