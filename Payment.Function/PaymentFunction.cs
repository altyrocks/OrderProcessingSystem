using Shared.Messaging;
using System.Text.Json;
using Payment.Function.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace Payment.Function;

public class PaymentFunction(ILoggerFactory loggerFactory, ProcessedEventStore processedEventStore, ServiceBusPublisher publisher)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PaymentFunction>();
    private readonly ProcessedEventStore _processedEventStore = processedEventStore;
    private readonly ServiceBusPublisher _publisher = publisher;

    [Function("PaymentFunction")]
    public async Task Run(
        [ServiceBusTrigger("orders", "payment-sub", Connection = "ServiceBusConnection")]
        string message)
    {
        string? eventKey = null;

        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope<InventoryReservedEvent>>(message);

            if (envelope == null || envelope.EventType != "InventoryReserved")
            {
                return;
            }

            eventKey = $"{envelope.EventType}:{envelope.CorrelationId}";

            if (!_processedEventStore.TryBegin(eventKey))
            {
                _logger.LogInformation("Skipping duplicate payment event {EventKey}", eventKey);
                return;
            }

            var order = envelope.Data;
            var correlationId = envelope.CorrelationId;

            _logger.LogInformation("CorrelationId: {CorrelationId}", correlationId);
            _logger.LogInformation("Processing Payment for Order: {OrderId}", order?.OrderId);

            var success = new Random().Next(0, 2) == 0;

            if (success)
            {
                _logger.LogInformation("Payment succeeded");

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
                _logger.LogInformation("Payment failed");

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
        catch (Exception ex)
        {
            if (eventKey != null)
            {
                _processedEventStore.MarkFailed(eventKey);
            }

            _logger.LogError(ex, "Error processing payment event");

            throw;
        }
    }
}