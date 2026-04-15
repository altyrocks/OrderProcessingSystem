namespace Shared.Messaging
{
    public class InventoryReservedEvent
    {
        public Guid OrderId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }
}