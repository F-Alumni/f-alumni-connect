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

public class InteractionService : IInteractionService
{
    private readonly IInteractionRepository _interactionRepository;
    private readonly IPostRepository _postRepository;
    private readonly ILogger<InteractionService> _logger;
    private readonly IKafkaProducerService? _kafkaProducer;

    public InteractionService(
        IInteractionRepository interactionRepository,
        IPostRepository postRepository,
        ILogger<InteractionService> logger,
        IKafkaProducerService? kafkaProducer = null)
    {
        _interactionRepository = interactionRepository;
        _postRepository = postRepository;
        _logger = logger;
        _kafkaProducer = kafkaProducer;
    }

    public Task<bool> ToggleReactionAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        return ToggleReactionAsync(postId, userId, actorName: null, cancellationToken);
    }

    public async Task<bool> ToggleReactionAsync(Guid postId, Guid userId, string? actorName, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", postId);
        }

        var liked = await _interactionRepository.ToggleReactionAsync(postId, userId, "Like", cancellationToken);
        _logger.LogInformation("User {UserId} toggled reaction on post {PostId}: {Liked}", userId, postId, liked);

        if (liked && post.AuthorId != userId && _kafkaProducer != null)
        {
            await _kafkaProducer.PublishPostReactedAsync(new PostReactedEventV1
            {
                PostId = post.Id,
                PostTitle = post.Title,
                PostAuthorId = post.AuthorId,
                ActorUserId = userId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "Một người dùng" : actorName.Trim(),
                ReactionType = "Like",
                OccurredAt = DateTime.UtcNow
            }, CancellationToken.None);
        }

        return liked;
    }

    public async Task<bool> ToggleBookmarkAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        var post = await _postRepository.GetByIdAsync(postId, cancellationToken);
        if (post == null)
        {
            throw new NotFoundException("Post", postId);
        }

        var saved = await _interactionRepository.ToggleBookmarkAsync(postId, userId, cancellationToken);
        _logger.LogInformation("User {UserId} toggled bookmark on post {PostId}: {Saved}", userId, postId, saved);
        return saved;
    }

    public async Task<PagedResponse<PostResponse>> GetMyBookmarksAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _interactionRepository.GetBookmarkedPostsAsync(userId, page, pageSize, cancellationToken);
        var responses = items.Select(p => new PostResponse
        {
            Id = p.Id,
            AuthorId = p.AuthorId,
            AuthorName = p.AuthorName,
            AuthorAvatarUrl = p.AuthorAvatarUrl,
            Title = p.Title,
            Content = p.Content,
            PostType = p.PostType,
            Major = p.Major,
            Campus = p.Campus,
            IsAnswered = p.IsAnswered,
            ViewCount = p.ViewCount,
            LikeCount = p.LikeCount,
            CommentCount = p.CommentCount,
            IsPinned = p.IsPinned,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Images = p.Images.OrderBy(i => i.Order).Select(i => i.ImageUrl).ToList(),
            Tags = p.Tags.Select(t => t.TagName).ToList(),
            IsBookmarked = true
        }).ToList();

        return new PagedResponse<PostResponse>(responses, totalCount, page, pageSize);
    }

    public async Task CreateReportAsync(CreateReportRequest request, Guid reporterId, string reporterName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Lý do báo cáo không được để trống.", nameof(request));
        }

        var targetType = string.IsNullOrWhiteSpace(request.TargetType) ? "Post" : request.TargetType.Trim();
        var cleanReporterName = string.IsNullOrWhiteSpace(reporterName) ? "Một người dùng" : reporterName.Trim();

        var report = new Report
        {
            TargetType = targetType,
            TargetId = request.TargetId,
            ReporterId = reporterId,
            ReporterName = cleanReporterName,
            Reason = request.Reason.Trim(),
            Details = request.Details?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        var savedReport = await _interactionRepository.AddReportAsync(report, cancellationToken);
        _logger.LogInformation("User {ReporterId} reported {TargetType} {TargetId}", reporterId, targetType, request.TargetId);

        if (_kafkaProducer != null)
        {
            try
            {
                await _kafkaProducer.PublishReportCreatedAsync(new ReportCreatedEventV1
                {
                    ReportId = savedReport.Id,
                    TargetType = savedReport.TargetType,
                    TargetId = savedReport.TargetId,
                    ReporterId = savedReport.ReporterId,
                    ReporterName = savedReport.ReporterName,
                    Reason = savedReport.Reason,
                    Details = savedReport.Details,
                    Status = savedReport.Status,
                    CreatedAt = savedReport.CreatedAt
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not publish ReportCreatedEventV1 for report {ReportId}. Continuing gracefully.", savedReport.Id);
            }
        }
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _interactionRepository.GetAllCategoriesAsync(cancellationToken);
        if (categories.Count == 0)
        {
            // Seed initial standard categories if empty
            var defaultCategories = new[]
            {
                new Category { Name = "Hỏi đáp & Trao đổi", Slug = "hoi-dap", Description = "Khu vực đặt câu hỏi về bài giảng, đồ án và quy chế." },
                new Category { Name = "Chia sẻ kinh nghiệm", Slug = "chia-se", Description = "Kinh nghiệm thực tập OJT, phỏng vấn và phát triển nghề nghiệp." },
                new Category { Name = "Cơ hội việc làm", Slug = "tuyen-dung", Description = "Thông tin tuyển dụng từ mạng lưới cựu sinh viên FPTU." },
                new Category { Name = "Sự kiện & Hội nhóm", Slug = "su-kien", Description = "Gặp gỡ, offline và hoạt động cộng đồng." }
            };

            foreach (var cat in defaultCategories)
            {
                await _interactionRepository.AddCategoryAsync(cat, cancellationToken);
            }
            categories = await _interactionRepository.GetAllCategoriesAsync(cancellationToken);
        }

        return categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            Description = c.Description
        }).ToList();
    }
}
