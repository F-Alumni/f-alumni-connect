using Contracts.Events;
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
    private readonly IKafkaProducerService? _kafkaProducer;

    public CommentService(
        ICommentRepository commentRepository,
        IPostRepository postRepository,
        ILogger<CommentService> logger,
        IKafkaProducerService? kafkaProducer = null)
    {
        _commentRepository = commentRepository;
        _postRepository = postRepository;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<CommentResponse> AddCommentAsync(Guid postId, CreateCommentRequest request, Guid authorId, string authorName, string authorAvatarUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Nội dung bình luận không được để trống.");
        }

        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", postId);
        }

        Comment? parent = null;
        if (request.ParentCommentId.HasValue)
        {
            parent = await _commentRepository.GetByIdAsync(request.ParentCommentId.Value, cancellationToken);
            if (parent == null || parent.PostId != postId)
            {
                throw new NotFoundException("ParentComment", request.ParentCommentId.Value);
            }
        }

        var trimmedContent = request.Content.Trim();
        var comment = new Comment
        {
            PostId = postId,
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorAvatarUrl = authorAvatarUrl,
            Content = trimmedContent,
            ParentCommentId = request.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _commentRepository.AddAsync(comment, cancellationToken);
        _logger.LogInformation("Added comment {CommentId} to post {PostId} by {AuthorId}", created.Id, postId, authorId);

        if (_kafkaProducer != null && (post.AuthorId != authorId || (parent != null && parent.AuthorId != authorId)))
        {
            var preview = trimmedContent.Length > 120 ? trimmedContent[..120] + "..." : trimmedContent;
            await _kafkaProducer.PublishCommentCreatedAsync(new CommentCreatedEventV1
            {
                CommentId = created.Id,
                PostId = post.Id,
                PostTitle = post.Title,
                PostAuthorId = post.AuthorId,
                ParentCommentId = parent?.Id,
                ParentCommentAuthorId = parent?.AuthorId,
                ActorUserId = authorId,
                ActorName = string.IsNullOrWhiteSpace(authorName) ? "Một người dùng" : authorName.Trim(),
                ContentPreview = preview,
                CreatedAt = created.CreatedAt
            }, CancellationToken.None);
        }

        return MapToResponse(created);
    }

    public async Task<IReadOnlyList<CommentResponse>> GetCommentsByPostIdAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var comments = await _commentRepository.GetByPostIdAsync(postId, cancellationToken);
        return comments.Select(MapToResponse).ToList();
    }

    public async Task<CommentResponse> UpdateCommentAsync(Guid id, UpdateCommentRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Nội dung bình luận không được để trống.");
        }

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
