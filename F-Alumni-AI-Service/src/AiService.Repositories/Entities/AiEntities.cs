namespace AiService.Repositories.Entities;

public class RagDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "Curriculum"; // Curriculum, Tuition, Facilities, Regulations, Slang, ForumPost
    public string? ProgramCode { get; set; }
    public string Campus { get; set; } = "All";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RagChunk> Chunks { get; set; } = new List<RagChunk>();
}

public class RagChunk
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public string ChunkIdentifier { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;

    public RagDocument? Document { get; set; }
}

public class AiConversation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Title { get; set; } = "Cuộc trò chuyện mới";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<AiMessage> Messages { get; set; } = new List<AiMessage>();
}

public class AiMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = "user"; // user, assistant
    public string Content { get; set; } = string.Empty;
    public string CitationsJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiConversation? Conversation { get; set; }
}

public class MentorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string CurrentCompany { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}
