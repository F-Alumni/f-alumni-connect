using ForumService.Services.DTOs.Responses;

namespace ForumService.Services.Interfaces;

public interface INotificationBroadcaster
{
    Task BroadcastNotificationAsync(Guid recipientId, NotificationResponse notification, int unreadCount, CancellationToken cancellationToken = default);
    Task BroadcastUnreadCountAsync(Guid recipientId, int unreadCount, CancellationToken cancellationToken = default);
}
