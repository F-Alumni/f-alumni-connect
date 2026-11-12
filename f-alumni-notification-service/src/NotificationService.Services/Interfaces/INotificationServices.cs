using NotificationService.Services.DTOs;

namespace NotificationService.Services.Interfaces;

public interface INotificationRealtimePusher
{
    Task PushToUserAsync(Guid recipientUserId, NotificationResponse notification, CancellationToken cancellationToken = default);
}

public interface INotificationAppService
{
    Task<PagedNotificationsResponse> GetMyNotificationsAsync(
        Guid userId,
        bool? unreadOnly = null,
        string? type = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationResponse> MarkAsReadAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<int> DeleteReadAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}

public interface INotificationEventProcessor
{
    Task<int> ProcessRawEventAsync(string topic, string rawJson, CancellationToken cancellationToken = default);
}
