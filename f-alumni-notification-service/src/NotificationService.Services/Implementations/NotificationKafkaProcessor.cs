using System.Text.Json;
using Contracts.Events;
using Microsoft.Extensions.Logging;
using NotificationService.Repositories.Entities;
using NotificationService.Repositories.Interfaces;
using NotificationService.Services.Interfaces;

namespace NotificationService.Services.Implementations;

public class NotificationKafkaProcessor : INotificationEventProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly INotificationRepository _repository;
    private readonly INotificationRealtimePusher _realtimePusher;
    private readonly ILogger<NotificationKafkaProcessor> _logger;

    public NotificationKafkaProcessor(
        INotificationRepository repository,
        INotificationRealtimePusher realtimePusher,
        ILogger<NotificationKafkaProcessor> logger)
    {
        _repository = repository;
        _realtimePusher = realtimePusher;
        _logger = logger;
    }

    public async Task<int> ProcessRawEventAsync(string topic, string rawJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return 0;
        }

        EventEnvelope<JsonElement>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope<JsonElement>>(rawJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Skipping malformed Kafka JSON on topic {Topic}", topic);
            return 0;
        }

        if (envelope == null || envelope.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return 0;
        }

        var eventType = string.IsNullOrWhiteSpace(envelope.EventType) ? topic : envelope.EventType.Trim();
        var eventKey = envelope.EventId != Guid.Empty
            ? envelope.EventId.ToString()
            : $"{eventType}:{Guid.NewGuid()}";

        var notifications = BuildNotifications(eventType, eventKey, envelope.Data);
        if (notifications.Count == 0)
        {
            return 0;
        }

        var createdCount = 0;
        foreach (var item in notifications)
        {
            var exists = await _repository.ExistsBySourceEventAsync(item.SourceEventId!, item.RecipientUserId, cancellationToken);
            if (exists)
            {
                continue;
            }

            var saved = await _repository.AddAsync(item, cancellationToken);
            createdCount++;

            try
            {
                await _realtimePusher.PushToUserAsync(
                    saved.RecipientUserId,
                    NotificationAppService.MapToResponse(saved),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Realtime SignalR push failed for NotificationId={NotificationId}; DB record is saved.", saved.Id);
            }
        }

        return createdCount;
    }

    private List<Notification> BuildNotifications(string eventType, string eventKey, JsonElement data)
    {
        return eventType switch
        {
            "chat.message_sent.v1" => BuildChatNotifications(eventKey, data),
            "forum.post_reacted.v1" => BuildPostReactedNotifications(eventKey, data),
            "forum.comment_created.v1" => BuildCommentCreatedNotifications(eventKey, data),
            _ => new List<Notification>()
        };
    }

    private List<Notification> BuildChatNotifications(string eventKey, JsonElement data)
    {
        var payload = data.Deserialize<ChatMessageSentEventData>(JsonOptions);
        if (payload == null || payload.RecipientUserIds.Count == 0)
        {
            return new List<Notification>();
        }

        var senderName = string.IsNullOrWhiteSpace(payload.SenderDisplayName) ? "Người dùng" : payload.SenderDisplayName.Trim();
        var preview = string.IsNullOrWhiteSpace(payload.ContentPreview)
            ? (payload.HasAttachment ? "[Đã gửi tệp đính kèm]" : "Đã gửi một tin nhắn mới.")
            : payload.ContentPreview.Trim();

        var results = new List<Notification>();
        foreach (var recipientId in payload.RecipientUserIds.Where(id => id != Guid.Empty && id != payload.SenderUserId).Distinct())
        {
            results.Add(new Notification
            {
                RecipientUserId = recipientId,
                ActorUserId = payload.SenderUserId,
                ActorName = senderName,
                Type = "chat_message",
                Title = $"{senderName} đã gửi tin nhắn mới",
                Message = Truncate(preview, 220),
                Link = $"/app/chat?conversationId={payload.ConversationId}",
                ReferenceId = payload.ConversationId.ToString(),
                SourceEventId = $"{eventKey}:chat",
                CreatedAt = payload.SentAtUtc == default ? DateTime.UtcNow : payload.SentAtUtc
            });
        }

        return results;
    }

    private List<Notification> BuildPostReactedNotifications(string eventKey, JsonElement data)
    {
        var payload = data.Deserialize<PostReactedEventData>(JsonOptions);
        if (payload == null || payload.PostAuthorId == Guid.Empty || payload.PostAuthorId == payload.ActorUserId)
        {
            return new List<Notification>();
        }

        var actorName = string.IsNullOrWhiteSpace(payload.ActorName) ? "Một thành viên" : payload.ActorName.Trim();
        var postTitle = string.IsNullOrWhiteSpace(payload.PostTitle) ? "bài viết của bạn" : payload.PostTitle.Trim();

        return new List<Notification>
        {
            new()
            {
                RecipientUserId = payload.PostAuthorId,
                ActorUserId = payload.ActorUserId,
                ActorName = actorName,
                Type = "post_like",
                Title = $"{actorName} đã thích bài viết của bạn",
                Message = Truncate(postTitle, 220),
                Link = $"/app/posts/{payload.PostId}",
                ReferenceId = payload.PostId.ToString(),
                SourceEventId = $"{eventKey}:like",
                CreatedAt = payload.OccurredAt == default ? DateTime.UtcNow : payload.OccurredAt
            }
        };
    }

    private List<Notification> BuildCommentCreatedNotifications(string eventKey, JsonElement data)
    {
        var payload = data.Deserialize<CommentCreatedEventData>(JsonOptions);
        if (payload == null)
        {
            return new List<Notification>();
        }

        var actorName = string.IsNullOrWhiteSpace(payload.ActorName) ? "Một thành viên" : payload.ActorName.Trim();
        var preview = string.IsNullOrWhiteSpace(payload.ContentPreview) ? payload.PostTitle : payload.ContentPreview.Trim();
        var createdAt = payload.CreatedAt == default ? DateTime.UtcNow : payload.CreatedAt;

        var results = new List<Notification>();
        var notifiedUsers = new HashSet<Guid>();

        if (payload.ParentCommentAuthorId.HasValue
            && payload.ParentCommentAuthorId.Value != Guid.Empty
            && payload.ParentCommentAuthorId.Value != payload.ActorUserId)
        {
            notifiedUsers.Add(payload.ParentCommentAuthorId.Value);
            results.Add(new Notification
            {
                RecipientUserId = payload.ParentCommentAuthorId.Value,
                ActorUserId = payload.ActorUserId,
                ActorName = actorName,
                Type = "comment_reply",
                Title = $"{actorName} đã trả lời bình luận của bạn",
                Message = Truncate(preview, 220),
                Link = $"/app/posts/{payload.PostId}",
                ReferenceId = payload.PostId.ToString(),
                SourceEventId = $"{eventKey}:reply",
                CreatedAt = createdAt
            });
        }

        if (payload.PostAuthorId != Guid.Empty
            && payload.PostAuthorId != payload.ActorUserId
            && notifiedUsers.Add(payload.PostAuthorId))
        {
            var postTitle = string.IsNullOrWhiteSpace(payload.PostTitle) ? "" : $" ({Truncate(payload.PostTitle, 60)})";
            results.Add(new Notification
            {
                RecipientUserId = payload.PostAuthorId,
                ActorUserId = payload.ActorUserId,
                ActorName = actorName,
                Type = "post_comment",
                Title = $"{actorName} đã bình luận bài viết của bạn{postTitle}",
                Message = Truncate(preview, 220),
                Link = $"/app/posts/{payload.PostId}",
                ReferenceId = payload.PostId.ToString(),
                SourceEventId = $"{eventKey}:comment",
                CreatedAt = createdAt
            });
        }

        return results;
    }

    private static string Truncate(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Length <= maxLength ? input : input[..maxLength] + "...";
    }
}
