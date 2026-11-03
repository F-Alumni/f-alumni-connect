using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Common;

namespace AdminService.Services.Interfaces;

/// <summary>
/// Service contract for recording and querying administrative audit trails (AD-07).
/// </summary>
public interface IAuditLogService
{
    Task<AuditLogResponse> LogActionAsync(
        Guid adminId,
        string adminEmail,
        string action,
        string targetEntity,
        string targetId,
        string? reason = null,
        string? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<AuditLogResponse>> GetAuditLogsAsync(
        AuditLogFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<AuditLogResponse?> GetAuditLogByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuditLogResponse>> GetRecentActionsAsync(
        int count = 10,
        CancellationToken cancellationToken = default);
}
