namespace AdminService.Services.DTOs.AuditLogs;

public class AuditLogResponse
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class AuditLogFilterQuery
{
    public string? Action { get; set; }
    public string? TargetEntity { get; set; }
    public Guid? AdminUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
