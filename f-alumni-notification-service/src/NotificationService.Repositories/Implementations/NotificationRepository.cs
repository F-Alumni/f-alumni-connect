using Microsoft.EntityFrameworkCore;
using NotificationService.Repositories.Context;
using NotificationService.Repositories.Entities;
using NotificationService.Repositories.Interfaces;

namespace NotificationService.Repositories.Implementations;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationServiceDbContext _dbContext;

    public NotificationRepository(NotificationServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetByRecipientAsync(
        Guid recipientUserId,
        bool? unreadOnly = null,
        string? type = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var baseQuery = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == recipientUserId);

        var unreadCount = await baseQuery.CountAsync(n => !n.IsRead, cancellationToken);

        var filtered = baseQuery;
        if (unreadOnly == true)
        {
            filtered = filtered.Where(n => !n.IsRead);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalizedType = type.Trim().ToLowerInvariant();
            if (normalizedType == "interaction" || normalizedType == "forum")
            {
                filtered = filtered.Where(n => n.Type == "post_like" || n.Type == "post_comment" || n.Type == "comment_reply");
            }
            else if (normalizedType == "chat")
            {
                filtered = filtered.Where(n => n.Type == "chat_message");
            }
            else
            {
                filtered = filtered.Where(n => n.Type == normalizedType);
            }
        }

        var totalCount = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, unreadCount);
    }

    public Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientUserId == recipientUserId && !n.IsRead, cancellationToken);
    }

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public Task<bool> ExistsBySourceEventAsync(string sourceEventId, Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Notifications
            .AsNoTracking()
            .AnyAsync(n => n.SourceEventId == sourceEventId && n.RecipientUserId == recipientUserId, cancellationToken);
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> MarkAllAsReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        var unreadItems = await _dbContext.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadItems.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var item in unreadItems)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return unreadItems.Count;
    }

    public async Task<int> DeleteReadAsync(Guid recipientUserId, CancellationToken cancellationToken = default)
    {
        var readItems = await _dbContext.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && n.IsRead)
            .ToListAsync(cancellationToken);

        if (readItems.Count == 0)
        {
            return 0;
        }

        _dbContext.Notifications.RemoveRange(readItems);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return readItems.Count;
    }

    public async Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
