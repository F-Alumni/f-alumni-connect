using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Moderation;

namespace AdminService.Services.Interfaces;

/// <summary>
/// Service contract for reviewing and resolving reported user violations and forum content (AD-05, AD-06).
/// </summary>
public interface IModerationReportService
{
    Task<PagedResponse<ModerationReportResponse>> GetReportsAsync(
        ModerationReportFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<ModerationReportResponse?> GetReportByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ModerationReportResponse> ResolveReportAsync(
        Guid adminId,
        string adminEmail,
        Guid reportId,
        ResolveReportRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<ModerationReportResponse> DismissReportAsync(
        Guid adminId,
        string adminEmail,
        Guid reportId,
        DismissReportRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
