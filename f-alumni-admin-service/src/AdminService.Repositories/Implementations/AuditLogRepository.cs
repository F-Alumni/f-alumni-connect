using Microsoft.EntityFrameworkCore;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;

namespace AdminService.Repositories.Implementations;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AdminServiceDbContext _dbContext;

    public AuditLogRepository(AdminServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLog> AddAsync(AuditLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.AuditLogs.AddAsync(log, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string? action = null,
        string? targetEntity = null,
        Guid? adminUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(x => x.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(targetEntity))
        {
            query = query.Where(x => x.TargetEntity == targetEntity);
        }

        if (adminUserId.HasValue)
        {
            query = query.Where(x => x.AdminUserId == adminUserId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentActionsAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs.CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetLogsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.CreatedAtUtc >= sinceUtc)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
