namespace Shared.Messaging
{
    public class PaymentSucceededEvent
    {
        public Guid OrderId { get; set; }
    }
}