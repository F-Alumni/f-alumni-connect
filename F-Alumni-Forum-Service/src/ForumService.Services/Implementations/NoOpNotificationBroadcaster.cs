using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Implementations;

public class NoOpNotificationBroadcaster : INotificationBroadcaster
{
    public Task BroadcastNotificationAsync(Guid recipientId, NotificationResponse notification, int unreadCount, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task BroadcastUnreadCountAsync(Guid recipientId, int unreadCount, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
