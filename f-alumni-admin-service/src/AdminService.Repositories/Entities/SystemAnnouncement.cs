namespace AdminService.Repositories.Entities;

/// <summary>
/// System-wide announcements published by administrators to all alumni and students (AD-08).
/// </summary>
public class SystemAnnouncement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "General"; // General, Urgent, Academic, Event, Maintenance
    public string Priority { get; set; } = "Normal"; // Normal, High, Urgent
    public Guid CreatedByAdminId { get; set; }
    public string CreatedByAdminEmail { get; set; } = string.Empty;
    public bool IsPublished { get; set; } = true;
    public DateTime? PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
