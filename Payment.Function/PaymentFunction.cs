using System.Text.Json;
using Payment.Function.Services;
using Payment.Function.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace Payment.Function;

public class PaymentFunction(ILoggerFactory loggerFactory, ServiceBusPublisher publisher)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PaymentFunction>();
    private readonly ServiceBusPublisher _publisher = publisher;

    [Function("PaymentFunction")]
    public async Task Run(
        [ServiceBusTrigger("orders", "payment-sub", Connection = "ServiceBusConnection")]
        string message)
    {
        // Ignore non-OrderCreated events
        if (!message.Contains("ProductName"))
            return; 

        var order = JsonSerializer.Deserialize<OrderCreatedEvent>(message);

        _logger.LogInformation($"💳 Processing Payment for Order: {order?.OrderId}");

        var success = new Random().Next(0, 2) == 0;

        if (success)
        {
            _logger.LogInformation("✅ Payment succeeded");

            var evt = new PaymentSucceededEvent
            {
                OrderId = order!.OrderId
            };

            await _publisher.PublishAsync(evt);
        }
        else
        {
            _logger.LogInformation("❌ Payment failed");

            var evt = new PaymentFailedEvent
            {
                OrderId = order!.OrderId,
                Reason = "Card declined"
            };

            await _publisher.PublishAsync(evt);
        }
    }
}