namespace AdminService.Repositories.Entities;

/// <summary>
/// Moderation queue items for reported posts, comments, or abusive users (AD-05).
/// </summary>
public class ModerationReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterUserId { get; set; }
    public string ReporterEmail { get; set; } = string.Empty;
    public string TargetType { get; set; } = "Post"; // Post, Comment, User
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty; // Spam, Harassment, Inappropriate, HateSpeech, Other
    public string? Details { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Dismissed
    public Guid? ResolvedByAdminId { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
