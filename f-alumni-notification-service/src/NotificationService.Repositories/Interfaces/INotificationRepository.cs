using NotificationService.Repositories.Entities;

namespace NotificationService.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetByRecipientAsync(
        Guid recipientUserId,
        bool? unreadOnly = null,
        string? type = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySourceEventAsync(string sourceEventId, Guid recipientUserId, CancellationToken cancellationToken = default);
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);
    Task<int> DeleteReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default);
}
