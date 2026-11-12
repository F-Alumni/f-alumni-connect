using System.Text;
using AdminService.Repositories.Interfaces;
using AdminService.Services.DTOs.AuditLogs;
using AdminService.Services.DTOs.Users;
using AdminService.Services.HttpClients;
using AdminService.Services.Interfaces;

namespace AdminService.Services.Implementations;

public class DataExportService : IDataExportService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUserServiceClient _userServiceClient;

    public DataExportService(
        IAuditLogRepository auditLogRepository,
        IUserServiceClient userServiceClient)
    {
        _auditLogRepository = auditLogRepository;
        _userServiceClient = userServiceClient;
    }

    public async Task<byte[]> ExportAuditLogsCsvAsync(
        AuditLogFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        // Export up to 5000 records
        var (items, _) = await _auditLogRepository.GetPagedAsync(
            action: query.Action,
            targetEntity: query.TargetEntity,
            adminUserId: query.AdminUserId,
            fromDate: query.FromDate,
            toDate: query.ToDate,
            pageNumber: 1,
            pageSize: 5000,
            cancellationToken: cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,AdminEmail,Action,TargetEntity,TargetId,Reason,Details,IpAddress,CreatedAtUtc");

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(item.Id.ToString()),
                EscapeCsv(item.AdminEmail),
                EscapeCsv(item.Action),
                EscapeCsv(item.TargetEntity),
                EscapeCsv(item.TargetId),
                EscapeCsv(item.Reason ?? string.Empty),
                EscapeCsv(item.Details ?? string.Empty),
                EscapeCsv(item.IpAddress ?? string.Empty),
                EscapeCsv(item.CreatedAtUtc.ToString("o"))
            ));
        }

        // Return UTF-8 bytes with BOM for Excel compatibility
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    public async Task<byte[]> ExportUsersSummaryCsvAsync(
        UserFilterQuery query,
        string? bearerToken = null,
        CancellationToken cancellationToken = default)
    {
        query.PageSize = 5000;
        var pagedUsers = await _userServiceClient.GetUsersDirectoryAsync(query, bearerToken, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Email,FullName,Role,AccountType,Status,Campus,Major,CurrentCompany,JobTitle,CreatedAtUtc");

        foreach (var user in pagedUsers.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(user.Id.ToString()),
                EscapeCsv(user.Email),
                EscapeCsv(user.FullName),
                EscapeCsv(user.Role),
                EscapeCsv(user.AccountType),
                EscapeCsv(user.Status),
                EscapeCsv(user.Campus ?? string.Empty),
                EscapeCsv(user.Major ?? string.Empty),
                EscapeCsv(user.CurrentCompany ?? string.Empty),
                EscapeCsv(user.JobTitle ?? string.Empty),
                EscapeCsv(user.CreatedAtUtc.ToString("o"))
            ));
        }

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
