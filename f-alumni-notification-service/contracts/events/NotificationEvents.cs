using System.Text.Json.Serialization;

namespace Contracts.Events;

public class EventEnvelope<T>
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("producer")]
    public string Producer { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;
}

public class ChatMessageSentEventData
{
    public Guid MessageId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public string ContentPreview { get; set; } = string.Empty;
    public bool HasAttachment { get; set; }
    public List<Guid> RecipientUserIds { get; set; } = new();
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
}

public class PostReactedEventData
{
    public Guid PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public Guid PostAuthorId { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ReactionType { get; set; } = "Like";
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class CommentCreatedEventData
{
    public Guid CommentId { get; set; }
    public Guid PostId { get; set; }
    public string PostTitle { get; set; } = string.Empty;
    public Guid PostAuthorId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public Guid? ParentCommentAuthorId { get; set; }
    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ContentPreview { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
