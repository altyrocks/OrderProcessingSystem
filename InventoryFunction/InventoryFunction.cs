using System.Text.Json;
using Inventory.Function.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace Inventory.Function;

public class InventoryFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<InventoryFunction>();

    [Function("InventoryFunction")]
    public void Run(
        [ServiceBusTrigger("orders", "inventory-sub", Connection = "ServiceBusConnection")]
    string message)
    {
        try
        {
            // Handle PaymentFailed (compensation)
            if (message.Contains("Reason"))
            {
                var failed = JsonSerializer.Deserialize<PaymentFailedEvent>(message);

                if (failed == null)
                {
                    _logger.LogWarning("⚠️ Failed to deserialize PaymentFailedEvent");
                    return;
                }

                _logger.LogInformation($"🔄 Releasing inventory for Order: {failed.OrderId}");
                _logger.LogInformation($"Reason: {failed.Reason}");

                return;
            }

            // Handle OrderCreated
            if (message.Contains("ProductName"))
            {
                var order = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

                if (order == null)
                {
                    _logger.LogWarning("⚠️ Failed to deserialize OrderCreatedEvent");
                    return;
                }

                _logger.LogInformation($"📦 Reserving inventory for Order: {order.OrderId}");
                _logger.LogInformation($"Product: {order.ProductName}, Qty: {order.Quantity}");

                return;
            }

            // Unknown message type
            _logger.LogInformation("ℹ️ Ignoring non-relevant event");
        }
        catch (Exception ex)
        {
            _logger.LogError($"💥 Error processing message: {ex.Message}");
        }
    }
}