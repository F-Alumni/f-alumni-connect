using AiService.Repositories.Entities;
using AiService.Services.DTOs.Responses;

namespace AiService.Services.Interfaces;

public class ScoredChunk
{
    public RagChunk Chunk { get; set; } = default!;
    public double Score { get; set; }
    public string? MatchedIntent { get; set; }
}

public interface IRagSearchService
{
    Task<IReadOnlyList<ScoredChunk>> SearchRelevantChunksAsync(
        string query,
        string? programCode = null,
        string? campus = null,
        int topK = 4,
        CancellationToken cancellationToken = default);

    Task<RagBoostDiagnosticsResponse> TestBoostAsync(
        string query,
        string? programCode = null,
        string? campus = null,
        int topK = 5,
        CancellationToken cancellationToken = default);
}
