using Microsoft.EntityFrameworkCore;
using ForumService.Repositories.Context;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;

namespace ForumService.Repositories.Implementations;

public class NotificationRepository : INotificationRepository
{
    private readonly ForumServiceDbContext _dbContext;

    public NotificationRepository(ForumServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount, int UnreadCount)> GetByUserAsync(
        Guid recipientId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == recipientId);

        var unreadCount = await baseQuery.CountAsync(n => !n.IsRead, cancellationToken);

        var filterQuery = baseQuery;
        if (unreadOnly == true)
        {
            filterQuery = filterQuery.Where(n => !n.IsRead);
        }

        var totalCount = await filterQuery.CountAsync(cancellationToken);

        var items = await filterQuery
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, unreadCount);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientId == recipientId && !n.IsRead, cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientId == recipientId, cancellationToken);
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task<bool> MarkAsReadAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default)
    {
        var notification = await GetByIdAsync(id, recipientId, cancellationToken);
        if (notification == null) return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default)
    {
        var unread = await _dbContext.Notifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0) return 0;

        foreach (var n in unread)
        {
            n.IsRead = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default)
    {
        var notification = await GetByIdAsync(id, recipientId, cancellationToken);
        if (notification == null) return false;

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
