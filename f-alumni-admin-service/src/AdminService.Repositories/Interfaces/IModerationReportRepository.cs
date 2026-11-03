using AdminService.Repositories.Entities;

namespace AdminService.Repositories.Interfaces;

public interface IModerationReportRepository
{
    Task<ModerationReport> AddAsync(ModerationReport report, CancellationToken cancellationToken = default);
    Task<ModerationReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<ModerationReport> Items, int TotalCount)> GetPagedAsync(
        string? status = null,
        string? targetType = null,
        int pageNumber = 1,
        int pageSize = 15,
        CancellationToken cancellationToken = default);
    Task<ModerationReport> UpdateAsync(ModerationReport report, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}
