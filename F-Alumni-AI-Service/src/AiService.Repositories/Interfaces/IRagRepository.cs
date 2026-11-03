using AiService.Repositories.Entities;

namespace AiService.Repositories.Interfaces;

public interface IRagRepository
{
    Task<RagDocument?> GetDocumentByPathAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<RagDocument> AddDocumentAsync(RagDocument doc, CancellationToken cancellationToken = default);
    Task AddChunksAsync(IEnumerable<RagChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RagChunk>> GetAllChunksAsync(string? programCode = null, string? campus = null, CancellationToken cancellationToken = default);
    Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);
    Task<int> GetChunkCountAsync(CancellationToken cancellationToken = default);
    Task DeleteDocumentByPathAsync(string sourcePath, CancellationToken cancellationToken = default);

    // Conversations
    Task<AiConversation?> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
    Task<AiConversation> CreateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default);
    Task AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiConversation>> GetConversationsByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    // Mentors
    Task<IReadOnlyList<MentorProfile>> GetMentorsAsync(string? major = null, CancellationToken cancellationToken = default);
    Task AddMentorAsync(MentorProfile mentor, CancellationToken cancellationToken = default);
}
