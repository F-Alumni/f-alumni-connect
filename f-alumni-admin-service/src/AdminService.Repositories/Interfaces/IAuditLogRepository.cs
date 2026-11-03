using AdminService.Repositories.Entities;

namespace AdminService.Repositories.Interfaces;

public interface IAuditLogRepository
{
    Task<AuditLog> AddAsync(AuditLog log, CancellationToken cancellationToken = default);
    Task<AuditLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string? action = null,
        string? targetEntity = null,
        Guid? adminUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetRecentActionsAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetLogsSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}
