namespace Shared.Messaging
{
    public class EventEnvelope<T>
    {
        public string EventType { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public T Data { get; set; } = default!;
    }
}