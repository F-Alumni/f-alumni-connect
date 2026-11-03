using Microsoft.EntityFrameworkCore;
using AdminService.Repositories.Context;
using AdminService.Repositories.Entities;
using AdminService.Repositories.Interfaces;

namespace AdminService.Repositories.Implementations;

public class ModerationReportRepository : IModerationReportRepository
{
    private readonly AdminServiceDbContext _dbContext;

    public ModerationReportRepository(AdminServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ModerationReport> AddAsync(ModerationReport report, CancellationToken cancellationToken = default)
    {
        await _dbContext.ModerationReports.AddAsync(report, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<ModerationReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ModerationReports
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<ModerationReport> Items, int TotalCount)> GetPagedAsync(
        string? status = null,
        string? targetType = null,
        int pageNumber = 1,
        int pageSize = 15,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ModerationReports.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            query = query.Where(x => x.TargetType == targetType);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ModerationReport> UpdateAsync(ModerationReport report, CancellationToken cancellationToken = default)
    {
        _dbContext.ModerationReports.Update(report);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ModerationReports
            .CountAsync(x => x.Status == "Pending", cancellationToken);
    }
}
