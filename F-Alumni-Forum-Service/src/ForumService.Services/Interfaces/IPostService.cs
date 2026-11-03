using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;

namespace ForumService.Services.Interfaces;

public interface IPostService
{
    Task<PostResponse> CreatePostAsync(CreatePostRequest request, Guid authorId, string authorName, string authorAvatarUrl, CancellationToken cancellationToken = default);
    Task<PostResponse> GetPostByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResponse<PostResponse>> GetFeedAsync(
        string? campus = null,
        string? major = null,
        string? postType = null,
        string? search = null,
        bool? isAnswered = null,
        string sort = "latest",
        int page = 1,
        int pageSize = 10,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default);
    Task<PagedResponse<PostResponse>> GetMyPostsAsync(Guid authorId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid id, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);
    Task MarkAnsweredAsync(Guid id, bool isAnswered, Guid currentUserId, CancellationToken cancellationToken = default);
}
