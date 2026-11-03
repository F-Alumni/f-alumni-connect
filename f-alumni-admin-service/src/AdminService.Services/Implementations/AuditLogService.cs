using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;
using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Common;
using AdminService.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdminService.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        ILogger<AuditLogService> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    public async Task<AuditLogResponse> LogActionAsync(
        Guid adminId,
        string adminEmail,
        string action,
        string targetEntity,
        string targetId,
        string? reason = null,
        string? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            AdminUserId = adminId,
            AdminEmail = adminEmail,
            Action = action,
            TargetEntity = targetEntity,
            TargetId = targetId,
            Reason = reason,
            Details = details,
            IpAddress = ipAddress,
            CreatedAtUtc = DateTime.UtcNow
        };

        var saved = await _auditLogRepository.AddAsync(log, cancellationToken);
        _logger.LogInformation("Audit log recorded: Admin {Email} performed {Action} on {TargetEntity} #{TargetId}",
            adminEmail, action, targetEntity, targetId);

        return MapToResponse(saved);
    }

    public async Task<PagedResponse<AuditLogResponse>> GetAuditLogsAsync(
        AuditLogFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _auditLogRepository.GetPagedAsync(
            action: query.Action,
            targetEntity: query.TargetEntity,
            adminUserId: query.AdminUserId,
            fromDate: query.FromDate,
            toDate: query.ToDate,
            pageNumber: query.PageNumber,
            pageSize: query.PageSize,
            cancellationToken: cancellationToken);

        return new PagedResponse<AuditLogResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AuditLogResponse?> GetAuditLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var log = await _auditLogRepository.GetByIdAsync(id, cancellationToken);
        return log is null ? null : MapToResponse(log);
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetRecentActionsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var list = await _auditLogRepository.GetRecentActionsAsync(count, cancellationToken);
        return list.Select(MapToResponse).ToList();
    }

    private static AuditLogResponse MapToResponse(AuditLog log) => new()
    {
        Id = log.Id,
        AdminUserId = log.AdminUserId,
        AdminEmail = log.AdminEmail,
        Action = log.Action,
        TargetEntity = log.TargetEntity,
        TargetId = log.TargetId,
        Reason = log.Reason,
        Details = log.Details,
        IpAddress = log.IpAddress,
        CreatedAtUtc = log.CreatedAtUtc
    };
}
