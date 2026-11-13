using Microsoft.EntityFrameworkCore;
using ForumService.Repositories.Context;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Helpers;
using ForumService.Repositories.Interfaces;

namespace ForumService.Repositories.Implementations;

public class PostRepository : IPostRepository
{
    private readonly ForumServiceDbContext _dbContext;

    public PostRepository(ForumServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .Include(p => p.Images)
            .Include(p => p.Tags)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsHidden, cancellationToken);
    }

    public async Task<Post?> GetByIdIncludingHiddenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Posts
            .Include(p => p.Images)
            .Include(p => p.Tags)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetFeedAsync(
        string? campus = null,
        string? major = null,
        string? postType = null,
        string? search = null,
        bool? isAnswered = null,
        string sort = "latest",
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default,
        bool includeHidden = false)
    {
        var query = _dbContext.Posts
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Tags)
            .Where(p => includeHidden || !p.IsHidden);

        if (!string.IsNullOrWhiteSpace(campus) && !campus.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var campusCandidates = CampusNormalizer.GetQueryCandidates(campus);
            if (campusCandidates.Count > 0)
            {
                query = query.Where(p => p.Campus == "All" || campusCandidates.Contains(p.Campus.Trim().ToLower()));
            }
            else
            {
                query = query.Where(p => p.Campus == campus || p.Campus == "All");
            }
        }

        if (!string.IsNullOrWhiteSpace(major) && !major.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Major == major || p.Major == "All");
        }

        if (!string.IsNullOrWhiteSpace(postType))
        {
            query = query.Where(p => p.PostType == postType);
        }

        if (isAnswered.HasValue)
        {
            query = query.Where(p => p.IsAnswered == isAnswered.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(s) || 
                                     p.Content.ToLower().Contains(s) || 
                                     p.Tags.Any(t => t.TagName.ToLower().Contains(s)));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        query = sort.ToLower() switch
        {
            "popular" => query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.LikeCount + p.CommentCount).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.IsPinned).ThenByDescending(p => p.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetByAuthorAsync(Guid authorId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Posts
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.Tags)
            .Where(p => p.AuthorId == authorId && !p.IsHidden)
            .OrderByDescending(p => p.CreatedAt);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default)
    {
        await _dbContext.Posts.AddAsync(post, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return post;
    }

    public async Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
    {
        if (_dbContext.Entry(post).State == EntityState.Detached)
        {
            _dbContext.Posts.Update(post);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
    {
        _dbContext.Posts.Remove(post);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementViewCountAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _dbContext.Posts.FindAsync([postId], cancellationToken);
        if (post != null)
        {
            post.ViewCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
