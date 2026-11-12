using NotificationService.Repositories.Entities;
using NotificationService.Repositories.Interfaces;
using NotificationService.Services.DTOs;
using NotificationService.Services.Interfaces;

namespace NotificationService.Services.Implementations;

public class NotificationAppService : INotificationAppService
{
    private readonly INotificationRepository _repository;

    public NotificationAppService(INotificationRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedNotificationsResponse> GetMyNotificationsAsync(
        Guid userId,
        bool? unreadOnly = null,
        string? type = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User identity is required.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount, unreadCount) = await _repository.GetByRecipientAsync(
            userId, unreadOnly, type, page, pageSize, cancellationToken);

        return new PagedNotificationsResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = totalCount,
            UnreadCount = unreadCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User identity is required.");
        }

        return _repository.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task<NotificationResponse> MarkAsReadAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var notification = await GetOwnedNotificationOrThrowAsync(id, currentUserId, cancellationToken);
        await _repository.MarkAsReadAsync(notification, cancellationToken);
        return MapToResponse(notification);
    }

    public Task<int> MarkAllAsReadAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User identity is required.");
        }

        return _repository.MarkAllAsReadAsync(currentUserId, cancellationToken);
    }

    public Task<int> DeleteReadAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User identity is required.");
        }

        return _repository.DeleteReadAsync(currentUserId, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var notification = await GetOwnedNotificationOrThrowAsync(id, currentUserId, cancellationToken);
        await _repository.DeleteAsync(notification, cancellationToken);
    }

    private async Task<Notification> GetOwnedNotificationOrThrowAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User identity is required.");
        }

        var notification = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Notification '{id}' was not found.");

        if (notification.RecipientUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác thông báo này.");
        }

        return notification;
    }

    public static NotificationResponse MapToResponse(Notification n)
    {
        return new NotificationResponse
        {
            Id = n.Id,
            RecipientUserId = n.RecipientUserId,
            ActorUserId = n.ActorUserId,
            ActorName = n.ActorName,
            Type = n.Type,
            Title = n.Title,
            Message = n.Message,
            Link = n.Link,
            ReferenceId = n.ReferenceId,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreatedAt = n.CreatedAt
        };
    }
}
