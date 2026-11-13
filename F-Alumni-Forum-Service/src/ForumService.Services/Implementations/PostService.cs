using Contracts.Events;
using Microsoft.Extensions.Logging;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;
using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Exceptions;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Implementations;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly IInteractionRepository _interactionRepository;
    private readonly IKafkaProducerService _kafkaProducer;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepository,
        IInteractionRepository interactionRepository,
        IKafkaProducerService kafkaProducer,
        ILogger<PostService> logger)
    {
        _postRepository = postRepository;
        _interactionRepository = interactionRepository;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<PostResponse> CreatePostAsync(CreatePostRequest request, Guid authorId, string authorName, string authorAvatarUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Tiêu đề bài viết không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Nội dung bài viết không được để trống.");
        }

        var post = new Post
        {
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorAvatarUrl = authorAvatarUrl,
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            PostType = string.IsNullOrWhiteSpace(request.PostType) ? "Sharing" : request.PostType.Trim(),
            Major = string.IsNullOrWhiteSpace(request.Major) ? "All" : request.Major.Trim(),
            Campus = string.IsNullOrWhiteSpace(request.Campus) ? "All" : request.Campus.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        if (request.ImageUrls != null)
        {
            int order = 0;
            foreach (var url in request.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                post.Images.Add(new PostImage
                {
                    ImageUrl = url.Trim(),
                    Order = order++
                });
            }
        }

        if (request.Tags != null)
        {
            foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
            {
                post.Tags.Add(new PostTag
                {
                    TagName = tag.Trim().ToLower()
                });
            }
        }

        var created = await _postRepository.AddAsync(post, cancellationToken);
        _logger.LogInformation("Created post {PostId} by author {AuthorId}", created.Id, authorId);

        // Publish domain event to Kafka
        var postEvent = new PostCreatedEventV1(
            postId: created.Id,
            authorId: created.AuthorId,
            authorName: created.AuthorName,
            title: created.Title,
            content: created.Content,
            postType: created.PostType,
            major: created.Major,
            campus: created.Campus
        );
        _ = _kafkaProducer.PublishPostCreatedAsync(postEvent, cancellationToken);

        return MapToResponse(created);
    }

    public async Task<PostResponse> GetPostByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", id);
        }

        await _postRepository.IncrementViewCountAsync(id, cancellationToken);
        post.ViewCount++;

        var response = MapToResponse(post);
        if (currentUserId.HasValue)
        {
            response.HasLiked = await _interactionRepository.HasUserReactedAsync(id, currentUserId.Value, cancellationToken);
            response.IsBookmarked = await _interactionRepository.HasUserBookmarkedAsync(id, currentUserId.Value, cancellationToken);
        }
        return response;
    }

    public async Task<PagedResponse<PostResponse>> GetFeedAsync(
        string? campus = null,
        string? major = null,
        string? postType = null,
        string? search = null,
        bool? isAnswered = null,
        string sort = "latest",
        int page = 1,
        int pageSize = 10,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default,
        bool includeHidden = false)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _postRepository.GetFeedAsync(campus, major, postType, search, isAnswered, sort, page, pageSize, cancellationToken, includeHidden);

        var responses = new List<PostResponse>();
        foreach (var item in items)
        {
            var res = MapToResponse(item);
            if (currentUserId.HasValue)
            {
                res.HasLiked = await _interactionRepository.HasUserReactedAsync(item.Id, currentUserId.Value, cancellationToken);
                res.IsBookmarked = await _interactionRepository.HasUserBookmarkedAsync(item.Id, currentUserId.Value, cancellationToken);
            }
            responses.Add(res);
        }

        return new PagedResponse<PostResponse>(responses, totalCount, page, pageSize);
    }

    public async Task<PagedResponse<PostResponse>> GetMyPostsAsync(Guid authorId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _postRepository.GetByAuthorAsync(authorId, page, pageSize, cancellationToken);
        var responses = new List<PostResponse>();
        foreach (var item in items)
        {
            var res = MapToResponse(item);
            res.HasLiked = await _interactionRepository.HasUserReactedAsync(item.Id, authorId, cancellationToken);
            res.IsBookmarked = await _interactionRepository.HasUserBookmarkedAsync(item.Id, authorId, cancellationToken);
            responses.Add(res);
        }
        return new PagedResponse<PostResponse>(responses, totalCount, page, pageSize);
    }

    public async Task<PostResponse> UpdatePostAsync(Guid id, UpdatePostRequest request, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Tiêu đề bài viết không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ArgumentException("Nội dung bài viết không được để trống.");
        }

        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", id);
        }

        if (post.AuthorId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền chỉnh sửa bài viết này.");
        }

        post.Title = request.Title.Trim();
        post.Content = request.Content.Trim();
        post.PostType = request.PostType;
        post.Major = request.Major;
        post.Campus = request.Campus;
        if (request.IsAnswered.HasValue)
        {
            post.IsAnswered = request.IsAnswered.Value;
        }
        post.UpdatedAt = DateTime.UtcNow;

        if (request.ImageUrls != null)
        {
            post.Images.Clear();
            int order = 0;
            foreach (var url in request.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                post.Images.Add(new PostImage { Id = Guid.Empty, PostId = post.Id, ImageUrl = url.Trim(), Order = order++ });
            }
        }

        if (request.Tags != null)
        {
            post.Tags.Clear();
            foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct())
            {
                post.Tags.Add(new PostTag { Id = Guid.Empty, PostId = post.Id, TagName = tag.Trim().ToLower() });
            }
        }

        await _postRepository.UpdateAsync(post, cancellationToken);
        return MapToResponse(post);
    }

    public async Task DeletePostAsync(Guid id, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", id);
        }

        if (post.AuthorId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền xóa bài viết này.");
        }

        await _postRepository.DeleteAsync(post, cancellationToken);
        _logger.LogInformation("Deleted post {PostId} by user {UserId}", id, currentUserId);
    }

    public async Task MarkAnsweredAsync(Guid id, bool isAnswered, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", id);
        }

        if (post.AuthorId != currentUserId)
        {
            throw new ForbiddenException("Chỉ tác giả câu hỏi mới có thể cập nhật trạng thái đã giải đáp.");
        }

        post.IsAnswered = isAnswered;
        post.UpdatedAt = DateTime.UtcNow;
        await _postRepository.UpdateAsync(post, cancellationToken);
    }

    public async Task<PostResponse> SetVisibilityAsync(Guid id, bool isHidden, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdIncludingHiddenAsync(id, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", id);
        }

        post.IsHidden = isHidden;
        post.UpdatedAt = DateTime.UtcNow;
        await _postRepository.UpdateAsync(post, cancellationToken);
        _logger.LogInformation("Set post {PostId} visibility IsHidden={IsHidden}", id, isHidden);
        return MapToResponse(post);
    }

    private static PostResponse MapToResponse(Post post)
    {
        return new PostResponse
        {
            Id = post.Id,
            AuthorId = post.AuthorId,
            AuthorName = post.AuthorName,
            AuthorAvatarUrl = post.AuthorAvatarUrl,
            Title = post.Title,
            Content = post.Content,
            PostType = post.PostType,
            Major = post.Major,
            Campus = post.Campus,
            IsAnswered = post.IsAnswered,
            ViewCount = post.ViewCount,
            LikeCount = post.LikeCount,
            CommentCount = post.CommentCount,
            IsPinned = post.IsPinned,
            IsHidden = post.IsHidden,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            Images = post.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).ToList(),
            Tags = post.Tags.Select(t => t.TagName).ToList(),
            Comments = post.Comments.Where(c => c.ParentCommentId == null).Select(MapCommentToResponse).ToList()
        };
    }

    private static CommentResponse MapCommentToResponse(Comment c)
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
            Replies = c.Replies.Select(MapCommentToResponse).ToList()
        };
    }
}
