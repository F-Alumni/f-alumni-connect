using Microsoft.Extensions.Logging;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Exceptions;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Implementations;

public class CommentService : ICommentService
{
    private readonly ICommentRepository _commentRepository;
    private readonly IPostRepository _postRepository;
    private readonly ILogger<CommentService> _logger;

    public CommentService(
        ICommentRepository commentRepository,
        IPostRepository postRepository,
        ILogger<CommentService> logger)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _logger = logger;
    }

    public async Task<CommentResponse> AddCommentAsync(Guid postId, CreateCommentRequest request, Guid authorId, string authorName, string authorAvatarUrl, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", postId);
        }

        if (request.ParentCommentId.HasValue)
        {
            var parent = await _commentRepository.GetByIdAsync(request.ParentCommentId.Value, cancellationToken);
            if (parent == null || parent.PostId != postId)
            {
                throw new NotFoundException("ParentComment", request.ParentCommentId.Value);
            }
        }

        var comment = new Comment
        {
            PostId = postId,
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorAvatarUrl = authorAvatarUrl,
            Content = request.Content.Trim(),
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.AddAsync(comment, cancellationToken);
        _logger.LogInformation("Added comment {CommentId} to post {PostId} by {AuthorId}", created.Id, postId, authorId);

        return MapToResponse(created);
    }

    public async Task<IReadOnlyList<CommentResponse>> GetCommentsByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var comments = await _commentRepository.GetByPostIdAsync(postId, cancellationToken);
        return comments.Select(MapToResponse).ToList();
    }

    public async Task<CommentResponse> UpdateCommentAsync(Guid id, UpdateCommentRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
        if (comment == null)
        {
            throw new NotFoundException("Comment", id);
        }

        if (comment.AuthorId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền chỉnh sửa bình luận này.");
        }

        comment.Content = request.Content.Trim();
        comment.UpdatedAt = DateTime.UtcNow;

        await _commentRepository.UpdateAsync(comment, cancellationToken);
        return MapToResponse(comment);
    }

    public async Task DeleteCommentAsync(Guid id, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(id, cancellationToken);
        if (comment == null)
        {
            throw new NotFoundException("Comment", id);
        }

        if (comment.AuthorId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền xóa bình luận này.");
        }

        await _commentRepository.DeleteAsync(comment, cancellationToken);
        _logger.LogInformation("Deleted comment {CommentId} by {UserId}", id, currentUserId);
    }

    public async Task<CommentResponse> AcceptAnswerAsync(Guid commentId, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var comment = await _commentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
        {
            throw new NotFoundException("Comment", commentId);
        }

        var post = await _postRepository.GetByIdAsync(comment.PostId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", comment.PostId);
        }

        if (post.AuthorId != currentUserId)
        {
            throw new ForbiddenException("Chỉ người đặt câu hỏi mới có quyền chấp nhận câu trả lời tốt nhất.");
        }

        comment.IsAcceptedAnswer = true;
        post.IsAnswered = true;

        await _commentRepository.UpdateAsync(comment, cancellationToken);
        await _postRepository.UpdateAsync(post, cancellationToken);

        return MapToResponse(comment);
    }

    private static CommentResponse MapToResponse(Comment c)
    {
        return new CommentResponse
        {
            Id = c.Id,
            PostId = c.PostId,
            AuthorId = c.AuthorId,
            AuthorName = c.AuthorName,
            AuthorAvatarUrl = c.AuthorAvatarUrl,
            Content = c.Content,
            ParentCommentId = c.ParentCommentId,
            IsAcceptedAnswer = c.IsAcceptedAnswer,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Replies = c.Replies?.Select(MapToResponse).ToList() ?? new List<CommentResponse>()
        };
    }
}
