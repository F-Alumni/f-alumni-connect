using Microsoft.EntityFrameworkCore;
using AiService.Repositories.Context;
using AiService.Repositories.Entities;
using AiService.Repositories.Interfaces;

namespace AiService.Repositories.Implementations;

public class RagRepository : IRagRepository
{
    private readonly AiServiceDbContext _dbContext;

    public RagRepository(AiServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RagDocument?> GetDocumentByPathAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        return await _dbContext.RagDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.SourcePath == sourcePath, cancellationToken);
    }

    public async Task<RagDocument> AddDocumentAsync(RagDocument doc, CancellationToken cancellationToken = default)
    {
        await _dbContext.RagDocuments.AddAsync(doc, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return doc;
    }

    public async Task AddChunksAsync(IEnumerable<RagChunk> chunks, CancellationToken cancellationToken = default)
    {
        await _dbContext.RagChunks.AddRangeAsync(chunks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RagChunk>> GetAllChunksAsync(string? programCode = null, string? campus = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RagChunks
            .AsNoTracking()
            .Include(c => c.Document)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(programCode) && !programCode.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.Document!.ProgramCode == programCode || c.Document.ProgramCode == null);
        }

        if (!string.IsNullOrWhiteSpace(campus) && !campus.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.Document!.Campus == campus || c.Document.Campus == "All");
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RagDocuments.CountAsync(cancellationToken);
    }

    public async Task<int> GetChunkCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.RagChunks.CountAsync(cancellationToken);
    }

    public async Task DeleteDocumentByPathAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.RagDocuments
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.SourcePath == sourcePath, cancellationToken);

        if (existing != null)
        {
            _dbContext.RagDocuments.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AiConversation?> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiConversations
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken);
    }

    public async Task<AiConversation> CreateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default)
    {
        await _dbContext.AiConversations.AddAsync(conversation, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public async Task AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default)
    {
        await _dbContext.AiMessages.AddAsync(message, cancellationToken);
        var conv = await _dbContext.AiConversations.FindAsync([message.ConversationId], cancellationToken);
        if (conv != null)
        {
            conv.LastMessageAt = DateTime.UtcNow;
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiConversation>> GetConversationsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AiConversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MentorProfile>> GetMentorsAsync(string? major = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.MentorProfiles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(major) && !major.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(m => m.Major == major);
        }
        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddMentorAsync(MentorProfile mentor, CancellationToken cancellationToken = default)
    {
        await _dbContext.MentorProfiles.AddAsync(mentor, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
