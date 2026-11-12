using System.Text.Json;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;
using AdminService.Services.Interfaces;
using Contracts.Events;
using Microsoft.Extensions.Logging;

namespace AdminService.Services.Implementations;

/// <summary>
/// Consumes <see cref="ReportCreatedEventV1"/> envelopes from Kafka and persists
/// corresponding <see cref="ModerationReport"/> records idempotently.
/// </summary>
public class ReportEventProcessor : IReportEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IModerationReportRepository _reportRepository;
    private readonly ILogger<ReportEventProcessor> _logger;

    public ReportEventProcessor(
        IModerationReportRepository reportRepository,
        ILogger<ReportEventProcessor> logger)
    {
        _reportRepository = reportRepository;
        _logger = logger;
    }

    public async Task<bool> ProcessRawEventAsync(string topic, string rawJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return false;
        }

        var (payload, envelopeEventId, envelopeEventType) = DeserializePayload(topic, rawJson);
        if (payload is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(envelopeEventType)
            && !string.Equals(envelopeEventType.Trim(), ReportCreatedEventV1.EventTypeName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Ignoring unsupported event type {EventType} on topic {Topic}", envelopeEventType, topic);
            return false;
        }

        var reportId = payload.ReportId != Guid.Empty ? payload.ReportId : envelopeEventId;
        if (reportId == Guid.Empty || payload.TargetId == Guid.Empty)
        {
            _logger.LogWarning("Skipping invalid ReportCreatedEventV1 with empty ReportId or TargetId on topic {Topic}", topic);
            return false;
        }

        if (await _reportRepository.ExistsAsync(reportId, cancellationToken))
        {
            _logger.LogDebug("ModerationReport {ReportId} already exists; skipping duplicate event.", reportId);
            return false;
        }

        var report = new ModerationReport
        {
            Id = reportId,
            ReporterUserId = payload.ReporterId,
            ReporterEmail = Truncate(string.IsNullOrWhiteSpace(payload.ReporterName) ? "unknown" : payload.ReporterName.Trim(), 256),
            TargetType = Truncate(string.IsNullOrWhiteSpace(payload.TargetType) ? "Post" : payload.TargetType.Trim(), 50),
            TargetId = payload.TargetId,
            Reason = Truncate(string.IsNullOrWhiteSpace(payload.Reason) ? "Other" : payload.Reason.Trim(), 200),
            Details = string.IsNullOrWhiteSpace(payload.Details) ? null : Truncate(payload.Details.Trim(), 2000),
            Status = "Pending",
            CreatedAtUtc = NormalizeUtc(payload.CreatedAt)
        };

        await _reportRepository.AddAsync(report, cancellationToken);
        _logger.LogInformation("Persisted ModerationReport {ReportId} for {TargetType} {TargetId} from topic {Topic}",
            report.Id, report.TargetType, report.TargetId, topic);

        return true;
    }

    private (ReportCreatedEventV1? Payload, Guid EnvelopeEventId, string? EnvelopeEventType) DeserializePayload(string topic, string rawJson)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(rawJson, JsonOptions);
            if (envelope is not null && envelope.Data.ValueKind is JsonValueKind.Object)
            {
                var data = envelope.Data.Deserialize<ReportCreatedEventV1>(JsonOptions);
                return (data, envelope.EventId, envelope.EventType);
            }

            // Fallback: direct payload without EventEnvelope wrapper
            var direct = JsonSerializer.Deserialize<ReportCreatedEventV1>(rawJson, JsonOptions);
            return (direct, Guid.Empty, null);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Skipping malformed Kafka JSON on topic {Topic}", topic);
            return (null, Guid.Empty, null);
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        if (value == default)
        {
            return DateTime.UtcNow;
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static string Truncate(string input, int maxLength)
    {
        return input.Length <= maxLength ? input : input[..maxLength];
    }
}
