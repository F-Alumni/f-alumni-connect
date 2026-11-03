using AdminService.Services.DTOs.AuditLogs;

namespace AdminService.Services.DTOs.Dashboard;

public class AdminDashboardSummaryDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int PendingReports { get; set; }
    public int PublishedAnnouncements { get; set; }
    public int TotalAuditLogsCount { get; set; }
    public IReadOnlyList<AuditLogResponse> RecentActivityFeed { get; set; } = new List<AuditLogResponse>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ActivityTimelineItemDto
{
    public string Date { get; set; } = string.Empty;
    public int TotalActions { get; set; }
    public Dictionary<string, int> ActionBreakdown { get; set; } = new();
}
