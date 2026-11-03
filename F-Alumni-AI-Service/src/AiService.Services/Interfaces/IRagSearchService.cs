using AiService.Repositories.Entities;

namespace AiService.Services.Interfaces;

public class ScoredChunk
{
    public RagChunk Chunk { get; set; } = default!;
    public double Score { get; set; }
}

public interface IRagSearchService
{
    Task<IReadOnlyList<ScoredChunk>> SearchRelevantChunksAsync(
        string query,
        string? programCode = null,
        string? campus = null,
        int topK = 4,
        CancellationToken cancellationToken = default);
}
