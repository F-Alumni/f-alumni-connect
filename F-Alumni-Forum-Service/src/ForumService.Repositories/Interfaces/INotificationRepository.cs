using ForumService.Repositories.Entities;

namespace ForumService.Repositories.Interfaces;

public interface INotificationRepository
{
    Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetByUserAsync(
        Guid recipientId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default);
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default);
}
