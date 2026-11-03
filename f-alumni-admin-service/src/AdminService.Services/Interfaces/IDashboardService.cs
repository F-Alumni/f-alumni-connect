using AdminService.Services.DTOs.Dashboard;

namespace AdminService.Services.Interfaces;

/// <summary>
/// Service contract for administrative summary metrics and activity timeline visualization (AD-01).
/// </summary>
public interface IDashboardService
{
    Task<AdminDashboardSummaryDto> GetSummaryAsync(
        string? bearerToken = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActivityTimelineItemDto>> GetActivityTimelineAsync(
        int days = 30,
        CancellationToken cancellationToken = default);
}
