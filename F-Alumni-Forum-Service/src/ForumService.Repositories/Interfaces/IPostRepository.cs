using ForumService.Repositories.Entities;

namespace ForumService.Repositories.Interfaces;

public interface IPostRepository
{
    Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetFeedAsync(
        string? campus = null,
        string? major = null,
        string? postType = null,
        string? search = null,
        bool? isAnswered = null,
        string sort = "latest",
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetByAuthorAsync(Guid authorId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);
    Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default);
    Task UpdateAsync(Post post, CancellationToken cancellationToken = default);
    Task DeleteAsync(Post post, CancellationToken cancellationToken = default);
    Task IncrementViewCountAsync(Guid postId, CancellationToken cancellationToken = default);
}
