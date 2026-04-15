namespace Shared.Messaging
{
    public class InventoryReleasedEvent
    {
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}