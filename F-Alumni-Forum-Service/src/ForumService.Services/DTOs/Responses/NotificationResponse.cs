namespace ForumService.Services.DTOs.Responses;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string SenderAvatarUrl { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // POST_COMMENT, POST_REACTION, COMMENT_REPLY, EVENT_RSVP, SYSTEM
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? EventId { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
