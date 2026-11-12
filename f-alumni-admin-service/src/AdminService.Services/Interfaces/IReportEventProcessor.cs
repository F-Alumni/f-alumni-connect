namespace AdminService.Services.Interfaces;

/// <summary>
/// Processes incoming domain events for forum reports and persists them into AdminService's moderation queue.
/// </summary>
public interface IReportEventProcessor
{
    /// <summary>
    /// Parses and idempotently persists a raw Kafka message payload for a forum report event.
    /// Returns true if a new ModerationReport was inserted, false if skipped or already present.
    /// </summary>
    Task<bool> ProcessRawEventAsync(string topic, string rawJson, CancellationToken cancellationToken = default);
}
