using System.Text.Json.Serialization;

namespace Contracts.Events;

/// <summary>
/// Domain event payload emitted when a user submits a report in Forum Service.
/// Topic: forum.report_created.v1
/// Partition Key: ReportId
/// </summary>
public class ReportCreatedEventV1
{
    public const string EventTypeName = "forum.report_created.v1";
    public const string CurrentVersion = "1.0";
    public const string DefaultTopic = "forum.report_created.v1";

    [JsonPropertyName("reportId")]
    public Guid ReportId { get; set; }

    [JsonPropertyName("targetType")]
    public string TargetType { get; set; } = "Post";

    [JsonPropertyName("targetId")]
    public Guid TargetId { get; set; }

    [JsonPropertyName("reporterId")]
    public Guid ReporterId { get; set; }

    [JsonPropertyName("reporterName")]
    public string ReporterName { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public string? Details { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
