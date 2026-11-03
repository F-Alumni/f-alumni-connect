using System.Text.Json;
using KafkaDemo.Contracts;

namespace KafkaConsumer;

public enum ProcessResult
{
    Success,
    Duplicate,
    Malformed
}

/// <summary>
/// Handles deserialization, validation, and idempotent deduplication of Kafka domain events.
/// </summary>
public class EventProcessor
{
    private readonly HashSet<Guid> _processedEventIds = [];

    /// <summary>
    /// Processes a raw string message consumed from Kafka.
    /// Handles malformed JSON gracefully and prevents duplicate execution using eventId tracking.
    /// </summary>
    public ProcessResult ProcessRawMessage(
        string rawJson,
        out EventEnvelope<UserRegisteredEventV1>? envelope,
        out string? errorMessage)
    {
        errorMessage = null;
        envelope = null;

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            errorMessage = "Message payload is empty or whitespace.";
            return ProcessResult.Malformed;
        }

        // 1. Safe Deserialization
        try
        {
            var parsed = JsonSerializer.Deserialize<EventEnvelope<UserRegisteredEventV1>>(rawJson);
            if (parsed is null || parsed.EventId == Guid.Empty || parsed.Data is null || string.IsNullOrWhiteSpace(parsed.EventType))
            {
                envelope = null;
                errorMessage = "Payload missing required envelope fields or Data is null.";
                return ProcessResult.Malformed;
            }

            envelope = parsed;
        }
        catch (JsonException ex)
        {
            envelope = null;
            errorMessage = $"Malformed JSON payload: {ex.Message}";
            return ProcessResult.Malformed;
        }

        // 2. Idempotency Check: Verify if eventId was already processed
        lock (_processedEventIds)
        {
            if (!_processedEventIds.Add(envelope.EventId))
            {
                return ProcessResult.Duplicate;
            }
        }

        return ProcessResult.Success;
    }

    /// <summary>
    /// Returns true if the eventId was already recorded as processed.
    /// </summary>
    public bool IsProcessed(Guid eventId)
    {
        lock (_processedEventIds)
        {
            return _processedEventIds.Contains(eventId);
        }
    }
}
