using AdminService.Repositories.Entities;

namespace AdminService.Repositories.Interfaces;

public interface ISystemAnnouncementRepository
{
    Task<SystemAnnouncement> AddAsync(SystemAnnouncement announcement, CancellationToken cancellationToken = default);
    Task<SystemAnnouncement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SystemAnnouncement> Items, int TotalCount)> GetPagedAsync(
        bool includeUnpublished = false,
        string? type = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<SystemAnnouncement> UpdateAsync(SystemAnnouncement announcement, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default);
}
