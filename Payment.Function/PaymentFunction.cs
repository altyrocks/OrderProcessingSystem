using Shared.Messaging;
using System.Text.Json;
using Payment.Function.Services;
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
        var envelope = JsonSerializer.Deserialize<EventEnvelope<OrderCreatedEvent>>(message);

        // Only process OrderCreated events
        if (envelope == null || envelope.EventType != "OrderCreated")
        {
            return;
        }

        var order = envelope.Data;
        var correlationId = envelope.CorrelationId;

        _logger.LogInformation($"🔗 CorrelationId: {correlationId}");
        _logger.LogInformation($"💳 Processing Payment for Order: {order?.OrderId}");

        var success = new Random().Next(0, 2) == 0;

        if (success)
        {
            _logger.LogInformation("✅ Payment succeeded");

            var result = new EventEnvelope<PaymentSucceededEvent>
            {
                EventType = "PaymentSucceeded",
                CorrelationId = correlationId,
                Data = new PaymentSucceededEvent
                {
                    OrderId = order!.OrderId
                }
            };

            await _publisher.PublishAsync(result);
        }
        else
        {
            _logger.LogInformation("❌ Payment failed");

            var result = new EventEnvelope<PaymentFailedEvent>
            {
                EventType = "PaymentFailed",
                CorrelationId = correlationId,
                Data = new PaymentFailedEvent
                {
                    OrderId = order!.OrderId,
                    Reason = "Card declined"
                }
            };

            await _publisher.PublishAsync(result);
        }
    }
}