using System.Text.Json.Serialization;

namespace Contracts.Events;

/// <summary>
/// Domain event payload emitted when a user likes/reacts to a forum post.
/// Topic: forum.post_reacted.v1
/// </summary>
public class PostReactedEventV1
{
    public const string EventTypeName = "forum.post_reacted.v1";
    public const string CurrentVersion = "1.0";
    public const string DefaultTopic = "forum.post_reacted.v1";

    [JsonPropertyName("postId")]
    public Guid PostId { get; set; }

    [JsonPropertyName("postTitle")]
    public string PostTitle { get; set; } = string.Empty;

    [JsonPropertyName("postAuthorId")]
    public Guid PostAuthorId { get; set; }

    [JsonPropertyName("actorUserId")]
    public Guid ActorUserId { get; set; }

    [JsonPropertyName("actorName")]
    public string ActorName { get; set; } = string.Empty;

    [JsonPropertyName("reactionType")]
    public string ReactionType { get; set; } = "Like";

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
