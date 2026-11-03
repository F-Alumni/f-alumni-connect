using System.Text.Json.Serialization;

namespace Contracts.Events;

/// <summary>
/// Domain event payload emitted when a new post is created in Forum Service.
/// Topic: post.created.v1
/// Partition Key: PostId
/// </summary>
public class PostCreatedEventV1
{
    public const string EventTypeName = "post.created.v1";
    public const string CurrentVersion = "1.0";
    public const string DefaultTopic = "post.created.v1";

    [JsonPropertyName("postId")]
    public Guid PostId { get; set; }

    [JsonPropertyName("authorId")]
    public Guid AuthorId { get; set; }

    [JsonPropertyName("authorName")]
    public string AuthorName { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("postType")]
    public string PostType { get; set; } = "Sharing";

    [JsonPropertyName("major")]
    public string Major { get; set; } = "All";

    [JsonPropertyName("campus")]
    public string Campus { get; set; } = "All";

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PostCreatedEventV1()
    {
    }

    public PostCreatedEventV1(Guid postId, Guid authorId, string authorName, string title, string content, string postType, string major, string campus)
    {
        PostId = postId;
        AuthorId = authorId;
        AuthorName = authorName;
        Title = title;
        Content = content;
        PostType = postType;
        Major = major;
        Campus = campus;
        CreatedAt = DateTime.UtcNow;
    }
}
