using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Moderation;
using AdminService.Services.Exceptions;
using AdminService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminService.Services.Implementations;

public class ModerationReportService : IModerationReportService
{
    private readonly IModerationReportRepository _reportRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ModerationReportService> _logger;

    public ModerationReportService(
        IModerationReportRepository reportRepository,
        IAuditLogService auditLogService,
        ILogger<ModerationReportService> logger)
    {
        _reportRepository = reportRepository;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<PagedResponse<ModerationReportResponse>> GetReportsAsync(
        ModerationReportFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _reportRepository.GetPagedAsync(
            status: query.Status,
            targetType: query.TargetType,
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            cancellationToken: cancellationToken);

        return new PagedResponse<ModerationReportResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ModerationReportResponse?> GetReportByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(id, cancellationToken);
        return report is null ? null : MapToResponse(report);
    }

    public async Task<ModerationReportResponse> ResolveReportAsync(
        Guid adminId,
        string adminEmail,
        Guid reportId,
        ResolveReportRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report is null)
        {
            throw new NotFoundException($"Moderation report '{reportId}' was not found.");
        }

        report.Status = "Resolved";
        report.ResolvedByAdminId = adminId;
        report.ResolutionNotes = $"Action: {request.ActionTaken} | Notes: {request.ResolutionNotes}";
        report.ResolvedAtUtc = DateTime.UtcNow;

        var updated = await _reportRepository.UpdateAsync(report, cancellationToken);

        await _auditLogService.LogActionAsync(
            adminId: adminId,
            adminEmail: adminEmail,
            action: "REPORT_RESOLVE",
            targetEntity: report.TargetType,
            targetId: report.TargetId.ToString(),
            reason: request.ResolutionNotes,
            details: $"ReportId: {reportId}, ActionTaken: {request.ActionTaken}",
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Moderation report {ReportId} resolved by admin {Email} with action {Action}",
            reportId, adminEmail, request.ActionTaken);

        return MapToResponse(updated);
    }

    public async Task<ModerationReportResponse> DismissReportAsync(
        Guid adminId,
        string adminEmail,
        Guid reportId,
        DismissReportRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, cancellationToken);
        if (report is null)
        {
            throw new NotFoundException($"Moderation report '{reportId}' was not found.");
        }

        report.Status = "Dismissed";
        report.ResolvedByAdminId = adminId;
        report.ResolutionNotes = request.DismissalReason;
        report.ResolvedAtUtc = DateTime.UtcNow;

        var updated = await _reportRepository.UpdateAsync(report, cancellationToken);

        await _auditLogService.LogActionAsync(
            adminId: adminId,
            adminEmail: adminEmail,
            action: "REPORT_DISMISS",
            targetEntity: report.TargetType,
            targetId: report.TargetId.ToString(),
            reason: request.DismissalReason,
            details: $"ReportId: {reportId}",
            ipAddress: ipAddress,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Moderation report {ReportId} dismissed by admin {Email}. Reason: {Reason}",
            reportId, adminEmail, request.DismissalReason);

        return MapToResponse(updated);
    }

    private static ModerationReportResponse MapToResponse(ModerationReport r) => new()
    {
        Id = r.Id,
        ReporterUserId = r.ReporterUserId,
        ReporterEmail = r.ReporterEmail,
        TargetType = r.TargetType,
        TargetId = r.TargetId,
        Reason = r.Reason,
        Details = r.Details,
        Status = r.Status,
        ResolvedByAdminId = r.ResolvedByAdminId,
        ResolutionNotes = r.ResolutionNotes,
        ResolvedAtUtc = r.ResolvedAtUtc,
        CreatedAtUtc = r.CreatedAtUtc
    };
}
