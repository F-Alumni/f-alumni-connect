namespace NotificationWorker.Services;

/// <summary>
/// Abstraction for tracking processed event IDs to ensure idempotent message processing.
/// In production, this can be backed by a database table (ProcessedEvents) or Redis cache.
/// </summary>
public interface IProcessedEventTracker
{
    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of IProcessedEventTracker for development and automated testing.
/// </summary>
public class InMemoryProcessedEventTracker : IProcessedEventTracker
{
    private readonly HashSet<Guid> _processedEventIds = [];

    public Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (_processedEventIds)
        {
            return Task.FromResult(_processedEventIds.Contains(eventId));
        }
    }

    public Task MarkAsProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        lock (_processedEventIds)
        {
            _processedEventIds.Add(eventId);
            return Task.CompletedTask;
        }
    }
}
