using Microsoft.EntityFrameworkCore;
using ForumService.Repositories.Context;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;

namespace ForumService.Repositories.Implementations;

public class InteractionRepository : IInteractionRepository
{
    private readonly ForumServiceDbContext _dbContext;

    public InteractionRepository(ForumServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ToggleReactionAsync(Guid postId, Guid userId, string reactionType = "Like", CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Reactions
            .FirstOrDefaultAsync(r => r.PostId == postId && r.UserId == userId, cancellationToken);

        var post = await _dbContext.Posts.FindAsync([postId], cancellationToken);

        if (existing != null)
        {
            _dbContext.Reactions.Remove(existing);
            if (post != null && post.LikeCount > 0)
            {
                post.LikeCount--;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false; // unliked
        }
        else
        {
            await _dbContext.Reactions.AddAsync(new Reaction
            {
                PostId = postId,
                UserId = userId,
                ReactionType = reactionType
            }, cancellationToken);

            if (post != null)
            {
                post.LikeCount++;
            }
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true; // liked
        }
    }

    public async Task<bool> HasUserReactedAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Reactions
            .AnyAsync(r => r.PostId == postId && r.UserId == userId, cancellationToken);
    }

    public async Task<bool> ToggleBookmarkAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Bookmarks
            .FirstOrDefaultAsync(b => b.PostId == postId && b.UserId == userId, cancellationToken);

        if (existing != null)
        {
            _dbContext.Bookmarks.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false; // removed
        }
        else
        {
            await _dbContext.Bookmarks.AddAsync(new Bookmark
            {
                PostId = postId,
                UserId = userId
            }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true; // saved
        }
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetBookmarkedPostsAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Bookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Post != null && !b.Post.IsHidden);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);
        var bookmarks = await query
            .Include(b => b.Post!)
                .ThenInclude(p => p.Images)
            .Include(b => b.Post!)
                .ThenInclude(p => p.Tags)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = bookmarks.Select(b => b.Post!).ToList();
        return (items, totalCount);
    }

    public async Task<Report> AddReportAsync(Report report, CancellationToken cancellationToken = default)
    {
        await _dbContext.Reports.AddAsync(report, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return report;
    }

    public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _dbContext.Categories.AddAsync(category, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return category;
    }
}
