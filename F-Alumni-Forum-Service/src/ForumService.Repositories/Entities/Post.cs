namespace ForumService.Repositories.Entities;

public class PostImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int Order { get; set; }

    public Post? Post { get; set; }
}

public class PostTag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public string TagName { get; set; } = string.Empty;

    public Post? Post { get; set; }
}

public class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorAvatarUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string PostType { get; set; } = "Sharing"; // Question, Sharing, Recruitment
    public string Major { get; set; } = "All"; // SE, AI, IA, GD, IB, MC, MKT, All
    public string Campus { get; set; } = "All"; // FU-HCM, FU-HL, FU-DN, FU-CT, FU-QN, All
    public bool IsAnswered { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsPinned { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PostImage> Images { get; set; } = new List<PostImage>();
    public ICollection<PostTag> Tags { get; set; } = new List<PostTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Reaction> Reactions { get; set; } = new List<Reaction>();
    public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}
