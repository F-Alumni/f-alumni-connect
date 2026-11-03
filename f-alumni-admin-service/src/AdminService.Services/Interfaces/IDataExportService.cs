using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Users;

namespace AdminService.Services.Interfaces;

/// <summary>
/// Service contract for exporting administrative audit logs and user activity data to CSV formats (AD-09).
/// </summary>
public interface IDataExportService
{
    Task<byte[]> ExportAuditLogsCsvAsync(
        AuditLogFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<byte[]> ExportUsersSummaryCsvAsync(
        UserFilterQuery query,
        string? bearerToken = null,
        CancellationToken cancellationToken = default);
}
