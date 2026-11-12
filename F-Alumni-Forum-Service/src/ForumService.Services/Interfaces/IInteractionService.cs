using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;

namespace ForumService.Services.Interfaces;

public interface IInteractionService
{
    Task<bool> ToggleReactionAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ToggleReactionAsync(Guid postId, Guid userId, string? actorName, CancellationToken cancellationToken = default);
    Task<bool> ToggleBookmarkAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResponse<PostResponse>> GetMyBookmarksAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task CreateReportAsync(CreateReportRequest request, Guid reporterId, string reporterName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
