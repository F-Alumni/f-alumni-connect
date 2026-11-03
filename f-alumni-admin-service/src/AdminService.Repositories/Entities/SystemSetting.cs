namespace AdminService.Repositories.Entities;

/// <summary>
/// Dynamic global system settings configurable by administrators.
/// </summary>
public class SystemSetting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; } // General, Security, Notifications, Maintenance
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? UpdatedByAdminId { get; set; }
}
