namespace AiService.Services.DTOs.Responses;

public class CitationResponse
{
    public string SourceTitle { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string? ProgramCode { get; set; }
    public string Snippet { get; set; } = string.Empty;
}

public class AiChatResponse
{
    public Guid ConversationId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public List<CitationResponse> Citations { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiMessageResponse
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<CitationResponse> Citations { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class AiConversationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public List<AiMessageResponse> Messages { get; set; } = new();
}

public class MentorResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string CurrentCompany { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Skills { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public int MatchScore { get; set; }
}

public class RagIngestionResult
{
    public int TotalFilesProcessed { get; set; }
    public int TotalChunksCreated { get; set; }
    public List<string> IngestedFiles { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class RagBoostDiagnosticsResponse
{
    public string Query { get; set; } = string.Empty;
    public string NormalizedQuery { get; set; } = string.Empty;
    public List<string> DetectedIntents { get; set; } = new();
    public List<string> ExtractedTerms { get; set; } = new();
    public int TotalCandidateChunks { get; set; }
    public List<ScoredChunkDetail> TopResults { get; set; } = new();
}

public class ScoredChunkDetail
{
    public double Score { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? ProgramCode { get; set; }
    public string Campus { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string? MatchedIntent { get; set; }
    public string Snippet { get; set; } = string.Empty;
}
