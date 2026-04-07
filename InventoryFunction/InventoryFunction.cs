using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shared.Messaging;

namespace Inventory.Function;

public class InventoryFunction
{
    private readonly ILogger _logger;

    public InventoryFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<InventoryFunction>();
    }

    [Function("InventoryFunction")]
    public void Run(
        [ServiceBusTrigger("orders", "inventory-sub", Connection = "ServiceBusConnection")]
        string message)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(message);

            if (envelope == null)
            {
                _logger.LogWarning("⚠️ Invalid message format");
                return;
            }

            _logger.LogInformation($"🔗 CorrelationId: {envelope.CorrelationId}");

            switch (envelope.EventType)
            {
                case "OrderCreated":
                    var order = envelope.Data.Deserialize<OrderCreatedEvent>();

                    _logger.LogInformation($"📦 Reserving inventory for Order: {order?.OrderId}");
                    _logger.LogInformation($"Product: {order?.ProductName}, Qty: {order?.Quantity}");
                    break;

                case "PaymentFailed":
                    var failed = envelope.Data.Deserialize<PaymentFailedEvent>();

                    _logger.LogInformation($"🔄 Releasing inventory for Order: {failed?.OrderId}");
                    _logger.LogInformation($"Reason: {failed?.Reason}");
                    break;

                default:
                    _logger.LogInformation("ℹ️ Ignoring non-relevant event");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"💥 Error processing message: {ex.Message}");
        }
    }
}