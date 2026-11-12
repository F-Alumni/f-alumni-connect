namespace NotificationService.Repositories.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientUserId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;

    /// <summary>
    /// Category: "chat_message", "post_like", "post_comment", "comment_reply", "system"
    /// </summary>
    public string Type { get; set; } = "system";

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Link { get; set; } = "/app/notifications";
    public string? ReferenceId { get; set; }
    public string? SourceEventId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
