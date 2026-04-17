using System.Collections.Concurrent;

namespace Orders.Api.Services;

public class ProcessedEventStore
{
    private readonly ConcurrentDictionary<string, byte> _processedEvents = new();

    public bool TryBegin(string eventKey)
    {
        return _processedEvents.TryAdd(eventKey, 0);
    }

    public void MarkFailed(string eventKey)
    {
        _processedEvents.TryRemove(eventKey, out _);
    }
}