using Template.Repositories.Entities;

namespace Template.Repositories.Interfaces;

/// <summary>
/// Data access abstraction for SampleItem entity.
/// </summary>
public interface ISampleItemRepository
{
    Task<IReadOnlyList<SampleItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<SampleItem> Items, int TotalCount)> GetPagedAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<SampleItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SampleItem> CreateAsync(SampleItem item, CancellationToken cancellationToken = default);
    Task<SampleItem?> UpdateAsync(SampleItem item, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
