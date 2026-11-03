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
        return await _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .Include(c => c.Replies)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
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
        var post = await _dbContext.Posts.FindAsync([comment.PostId], cancellationToken);
        if (post != null && post.CommentCount > 0)
        {
            post.CommentCount--;
        }
        _dbContext.Comments.Remove(comment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
