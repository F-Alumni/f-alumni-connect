using AdminService.Repositories.Interfaces;
using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Dashboard;
using AdminService.Services.DTOs.Users;
using AdminService.Services.HttpClients;
using AdminService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminService.Services.Implementations;

public class DashboardService : IDashboardService
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly IModerationReportRepository _reportRepository;
    private readonly ISystemAnnouncementRepository _announcementRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        IUserServiceClient userServiceClient,
        IModerationReportRepository reportRepository,
        ISystemAnnouncementRepository announcementRepository,
        IAuditLogRepository auditLogRepository,
        ILogger<DashboardService> logger)
    {
        _userServiceClient = userServiceClient;
        _reportRepository = reportRepository;
        _announcementRepository = announcementRepository;
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<AdminDashboardSummaryDto> GetSummaryAsync(
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Compiling administrative dashboard summary metrics.");

        // 1. Fetch user metrics from User Service (HTTP calls can safely run concurrently)
        var totalUsersTask = _userServiceClient.GetUsersDirectoryAsync(
            new UserFilterQuery { PageSize = 1 }, bearerToken, cancellationToken);
        var activeUsersTask = _userServiceClient.GetUsersDirectoryAsync(
            new UserFilterQuery { Status = "Active", PageSize = 1 }, bearerToken, cancellationToken);
        var suspendedUsersTask = _userServiceClient.GetUsersDirectoryAsync(
            new UserFilterQuery { Status = "Suspended", PageSize = 1 }, bearerToken, cancellationToken);

        await Task.WhenAll(totalUsersTask, activeUsersTask, suspendedUsersTask);

        // 2. Fetch admin repository metrics sequentially to avoid concurrent operations on the same EF Core DbContext instance
        var pendingReports = await _reportRepository.GetPendingCountAsync(cancellationToken);
        var publishedAnnouncements = await _announcementRepository.GetPublishedCountAsync(cancellationToken);
        var totalAuditLogs = await _auditLogRepository.GetTotalCountAsync(cancellationToken);
        var recentLogsRaw = await _auditLogRepository.GetRecentActionsAsync(10, cancellationToken);

        var recentLogs = recentLogsRaw.Select(l => new AuditLogResponse
        {
            Id = l.Id,
            AdminUserId = l.AdminUserId,
            AdminEmail = l.AdminEmail,
            Action = l.Action,
            TargetEntity = l.TargetEntity,
            TargetId = l.TargetId,
            Reason = l.Reason,
            Details = l.Details,
            IpAddress = l.IpAddress,
            CreatedAtUtc = l.CreatedAtUtc
        }).ToList();

        return new AdminDashboardSummaryDto
        {
            TotalUsers = (await totalUsersTask).TotalCount,
            ActiveUsers = (await activeUsersTask).TotalCount,
            SuspendedUsers = (await suspendedUsersTask).TotalCount,
            PendingReports = pendingReports,
            PublishedAnnouncements = publishedAnnouncements,
            TotalAuditLogsCount = totalAuditLogs,
            RecentActivityFeed = recentLogs,
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<IReadOnlyList<ActivityTimelineItemDto>> GetActivityTimelineAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        var clampedDays = Math.Clamp(days, 1, 90);
        var sinceUtc = DateTime.UtcNow.Date.AddDays(-clampedDays + 1);

        var logs = await _auditLogRepository.GetLogsSinceAsync(sinceUtc, cancellationToken);

        var grouped = logs
            .GroupBy(l => l.CreatedAtUtc.ToString("yyyy-MM-dd"))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<ActivityTimelineItemDto>();
        for (var i = 0; i < clampedDays; i++)
        {
            var dateKey = sinceUtc.AddDays(i).ToString("yyyy-MM-dd");
            if (grouped.TryGetValue(dateKey, out var dayLogs))
            {
                var breakdown = dayLogs
                    .GroupBy(l => l.Action)
                    .ToDictionary(g => g.Key, g => g.Count());

                result.Add(new ActivityTimelineItemDto
                {
                    Date = dateKey,
                    TotalActions = dayLogs.Count,
                    ActionBreakdown = breakdown
                });
            }
            else
            {
                result.Add(new ActivityTimelineItemDto
                {
                    Date = dateKey,
                    TotalActions = 0,
                    ActionBreakdown = new()
                });
            }
        }

        return result;
    }
}
