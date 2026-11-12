using Microsoft.EntityFrameworkCore;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;

namespace AdminService.Repositories.Implementations;

public class SystemAnnouncementRepository : ISystemAnnouncementRepository
{
    private readonly AdminServiceDbContext _dbContext;

    public SystemAnnouncementRepository(AdminServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SystemAnnouncement> AddAsync(SystemAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        await _dbContext.SystemAnnouncements.AddAsync(announcement, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return announcement;
    }

    public async Task<SystemAnnouncement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemAnnouncements
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<SystemAnnouncement> Items, int TotalCount)> GetPagedAsync(
        bool includeUnpublished = false,
        string? type = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SystemAnnouncements.AsNoTracking().AsQueryable();

        if (!includeUnpublished)
        {
            query = query.Where(x => x.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(x => x.Type == type);
        }

        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.PublishedAtUtc ?? x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<SystemAnnouncement> UpdateAsync(SystemAnnouncement announcement, CancellationToken cancellationToken = default)
    {
        _dbContext.SystemAnnouncements.Update(announcement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return announcement;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.SystemAnnouncements.FindAsync(new object[] { id }, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _dbContext.SystemAnnouncements.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SystemAnnouncements
            .CountAsync(x => x.IsPublished, cancellationToken);
    }
}
