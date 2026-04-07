namespace Shared.Messaging;

public class PaymentFailedEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}