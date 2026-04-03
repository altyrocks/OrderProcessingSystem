namespace Payment.Function.Messaging
{
    public class OrderCreatedEvent
    {
        public Guid OrderId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}