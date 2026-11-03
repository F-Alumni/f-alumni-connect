using UserService.Services.DTOs.Common;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;

namespace UserService.Services.Interfaces;

/// <summary>
/// Business logic contract for SampleItem operations.
/// </summary>
public interface ISampleItemService
{
    Task<IReadOnlyList<SampleItemResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResponse<SampleItemResponse>> GetPagedAsync(string? search, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<SampleItemResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SampleItemResponse> CreateAsync(CreateSampleItemRequest request, CancellationToken cancellationToken = default);
    Task<SampleItemResponse> UpdateAsync(Guid id, UpdateSampleItemRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
