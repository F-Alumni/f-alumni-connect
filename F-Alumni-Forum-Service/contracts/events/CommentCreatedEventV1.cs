using System.Text.Json.Serialization;

namespace Contracts.Events;

/// <summary>
/// Domain event payload emitted when a comment or reply is added to a forum post.
/// Topic: forum.comment_created.v1
/// </summary>
public class CommentCreatedEventV1
{
    public const string EventTypeName = "forum.comment_created.v1";
    public const string CurrentVersion = "1.0";
    public const string DefaultTopic = "forum.comment_created.v1";

    [JsonPropertyName("commentId")]
    public Guid CommentId { get; set; }

    [JsonPropertyName("postId")]
    public Guid PostId { get; set; }

    [JsonPropertyName("postTitle")]
    public string PostTitle { get; set; } = string.Empty;

    [JsonPropertyName("postAuthorId")]
    public Guid PostAuthorId { get; set; }

    [JsonPropertyName("parentCommentId")]
    public Guid? ParentCommentId { get; set; }

    [JsonPropertyName("parentCommentAuthorId")]
    public Guid? ParentCommentAuthorId { get; set; }

    [JsonPropertyName("actorUserId")]
    public Guid ActorUserId { get; set; }

    [JsonPropertyName("actorName")]
    public string ActorName { get; set; } = string.Empty;

    [JsonPropertyName("contentPreview")]
    public string ContentPreview { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
