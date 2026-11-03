namespace AdminService.Repositories.Entities;

/// <summary>
/// Immutable audit log trail for administrator actions (AD-07).
/// Tracks disciplinary actions (e.g. user suspend/unsuspend), content moderation, and announcements.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AdminUserId { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // SuspendUser, UnsuspendUser, ResolveReport, DismissReport, BroadcastAnnouncement, UpdateCategory
    public string TargetEntity { get; set; } = string.Empty; // User, Post, Comment, Announcement, Category
    public string TargetId { get; set; } = string.Empty; // Identifier of the target entity
    public string? Reason { get; set; }
    public string? Details { get; set; } // Additional metadata/snapshot in JSON format
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
