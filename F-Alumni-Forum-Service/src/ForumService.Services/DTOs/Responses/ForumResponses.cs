namespace ForumService.Services.DTOs.Responses;

public class PostResponse
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorAvatarUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PostType { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string Campus { get; set; } = string.Empty;
    public bool IsAnswered { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<string> Images { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<CommentResponse> Comments { get; set; } = new();

    public bool HasLiked { get; set; }
    public bool IsBookmarked { get; set; }
}

public class CommentResponse
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorAvatarUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
    public bool IsAcceptedAnswer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<CommentResponse> Replies { get; set; } = new();
}

public class EventResponse
{
    public Guid Id { get; set; }
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string CreatorAvatarUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Campus { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? MaxAttendees { get; set; }
    public int GoingCount { get; set; }
    public int InterestedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? MyStatus { get; set; }
}

public class EventAttendeeResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserAvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class CategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
