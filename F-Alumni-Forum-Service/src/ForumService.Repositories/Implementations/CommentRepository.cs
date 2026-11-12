using Microsoft.EntityFrameworkCore;
using ForumService.Repositories.Context;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;

namespace ForumService.Repositories.Implementations;

public class CommentRepository : ICommentRepository
{
    private readonly ForumServiceDbContext _dbContext;

    public CommentRepository(ForumServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Comments
            .Include(c => c.Replies)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Comment>> GetByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var allComments = await _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var lookup = allComments.ToDictionary(c => c.Id);
        foreach (var c in allComments)
        {
            c.Replies = new List<Comment>();
        }

        var topLevel = new List<Comment>();
        foreach (var c in allComments)
        {
            if (c.ParentCommentId.HasValue && lookup.TryGetValue(c.ParentCommentId.Value, out var parent))
            {
                parent.Replies.Add(c);
            }
            else
            {
                topLevel.Add(c);
            }
        }

        return topLevel;
    }

    public async Task<Comment> AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Comments.AddAsync(comment, cancellationToken);
        var post = await _dbContext.Posts.FindAsync([comment.PostId], cancellationToken);
        if (post != null)
        {
            post.CommentCount++;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task UpdateAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _dbContext.Comments.Update(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        var allPostComments = await _dbContext.Comments
            .Where(c => c.PostId == comment.PostId)
            .ToListAsync(cancellationToken);

        var toDelete = new List<Comment>();
        CollectDescendants(comment.Id, allPostComments, toDelete);
        var root = allPostComments.FirstOrDefault(c => c.Id == comment.Id) ?? comment;
        toDelete.Add(root);

        var post = await _dbContext.Posts.FindAsync([comment.PostId], cancellationToken);
        if (post != null)
        {
            post.CommentCount = Math.Max(0, post.CommentCount - toDelete.Count);
            if (toDelete.Any(c => c.IsAcceptedAnswer))
            {
                var deletedIds = toDelete.Select(c => c.Id).ToHashSet();
                if (!allPostComments.Any(c => !deletedIds.Contains(c.Id) && c.IsAcceptedAnswer))
                {
                    post.IsAnswered = false;
                }
            }
        }

        _dbContext.Comments.RemoveRange(toDelete);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void CollectDescendants(Guid parentId, List<Comment> allComments, List<Comment> result)
    {
        var children = allComments.Where(c => c.ParentCommentId == parentId).ToList();
        foreach (var child in children)
        {
            CollectDescendants(child.Id, allComments, result);
            result.Add(child);
        }
    }
}
