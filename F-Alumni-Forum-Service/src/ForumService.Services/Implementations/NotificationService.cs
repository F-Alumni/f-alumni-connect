using Microsoft.Extensions.Logging;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;
using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationBroadcaster? _broadcaster;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notificationRepository,
        ILogger<NotificationService> logger,
        INotificationBroadcaster? broadcaster = null)
    {
        _notificationRepository = notificationRepository;
        _logger = logger;
        _broadcaster = broadcaster;
    }

    public async Task<PagedResponse<NotificationResponse>> GetMyNotificationsAsync(
        Guid recipientId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, totalCount, _) = await _notificationRepository.GetByUserAsync(
            recipientId, unreadOnly, page, pageSize, cancellationToken);

        var responses = items.Select(MapToResponse).ToList();
        return new PagedResponse<NotificationResponse>(responses, totalCount, page, pageSize);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        return await _notificationRepository.GetUnreadCountAsync(recipientId, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default)
    {
        var result = await _notificationRepository.MarkAsReadAsync(id, recipientId, cancellationToken);
        if (result && _broadcaster != null)
        {
            try
            {
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(recipientId, cancellationToken);
                await _broadcaster.BroadcastUnreadCountAsync(recipientId, unreadCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR unread count broadcast failed on MarkAsRead for user {UserId}", recipientId);
            }
        }
        return result;
    }

    public async Task<int> MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        var updated = await _notificationRepository.MarkAllAsReadAsync(recipientId, cancellationToken);
        if (updated > 0 && _broadcaster != null)
        {
            try
            {
                await _broadcaster.BroadcastUnreadCountAsync(recipientId, 0, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR unread count broadcast failed on MarkAllAsRead for user {UserId}", recipientId);
            }
        }
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default)
    {
        var deleted = await _notificationRepository.DeleteAsync(id, recipientId, cancellationToken);
        if (deleted && _broadcaster != null)
        {
            try
            {
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(recipientId, cancellationToken);
                await _broadcaster.BroadcastUnreadCountAsync(recipientId, unreadCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR unread count broadcast failed on Delete for user {UserId}", recipientId);
            }
        }
        return deleted;
    }

    public async Task<NotificationResponse> CreateNotificationAsync(
        Guid recipientId,
        Guid senderId,
        string senderName,
        string senderAvatarUrl,
        string type,
        string title,
        string body,
        Guid? postId = null,
        Guid? commentId = null,
        Guid? eventId = null,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (recipientId == Guid.Empty)
        {
            throw new ArgumentException("RecipientId cannot be empty.", nameof(recipientId));
        }

        var resolvedLink = linkUrl;
        if (string.IsNullOrWhiteSpace(resolvedLink))
        {
            if (postId.HasValue && commentId.HasValue)
            {
                resolvedLink = $"/posts/{postId}#comment-{commentId}";
            }
            else if (postId.HasValue)
            {
                resolvedLink = $"/posts/{postId}";
            }
            else if (eventId.HasValue)
            {
                resolvedLink = $"/events/{eventId}";
            }
        }

        var resolvedSenderName = string.IsNullOrWhiteSpace(senderName) ? "Thành viên FPTU" : senderName.Trim();
        var resolvedAvatarUrl = senderAvatarUrl ?? string.Empty;

        var notification = new Notification
        {
            RecipientId = recipientId,
            SenderId = senderId,
            SenderName = Truncate(resolvedSenderName, 200),
            SenderAvatarUrl = Truncate(resolvedAvatarUrl, 500),
            Type = Truncate(type, 50),
            Title = Truncate(title, 250),
            Body = Truncate(body, 1000),
            PostId = postId,
            CommentId = commentId,
            EventId = eventId,
            LinkUrl = resolvedLink != null ? Truncate(resolvedLink, 500) : null,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _notificationRepository.AddAsync(notification, cancellationToken);
        var response = MapToResponse(saved);

        _logger.LogInformation("Created notification {NotificationId} ({Type}) for user {RecipientId}", saved.Id, type, recipientId);

        if (_broadcaster != null)
        {
            try
            {
                var unreadCount = await _notificationRepository.GetUnreadCountAsync(recipientId, cancellationToken);
                await _broadcaster.BroadcastNotificationAsync(recipientId, response, unreadCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SignalR notification broadcast failed for user {UserId}", recipientId);
            }
        }

        return response;
    }

    private static NotificationResponse MapToResponse(Notification n) => new()
    {
        Id = n.Id,
        RecipientId = n.RecipientId,
        SenderId = n.SenderId,
        SenderName = n.SenderName,
        SenderAvatarUrl = n.SenderAvatarUrl,
        Type = n.Type,
        Title = n.Title,
        Body = n.Body,
        PostId = n.PostId,
        CommentId = n.CommentId,
        EventId = n.EventId,
        LinkUrl = n.LinkUrl,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt
    };

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return value[..maxLength];
    }
}
