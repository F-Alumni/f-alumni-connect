using ForumService.Repositories.Entities;

namespace ForumService.Repositories.Interfaces;

public interface IInteractionRepository
{
    // Reactions
    Task<bool> ToggleReactionAsync(Guid postId, Guid userId, string reactionType = "Like", CancellationToken cancellationToken = default);
    Task<bool> HasUserReactedAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);

    // Bookmarks
    Task<bool> HasUserBookmarkedAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ToggleBookmarkAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetBookmarkedPostsAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default);

    // Reports
    Task<Report> AddReportAsync(Report report, CancellationToken cancellationToken = default);

    // Categories
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default);
}
