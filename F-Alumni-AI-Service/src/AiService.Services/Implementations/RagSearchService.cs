using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AiService.Repositories.Entities;
using AiService.Repositories.Interfaces;
using AiService.Services.Interfaces;

namespace AiService.Services.Implementations;

public class RagSearchService : IRagSearchService
{
    private readonly IRagRepository _ragRepository;
    private readonly ILogger<RagSearchService> _logger;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "là", "của", "và", "các", "những", "thì", "mà", "ở", "tại", "cho", "với", "được",
        "có", "gì", "nào", "sao", "thế", "như", "thế_nào", "hỏi", "về", "trong", "cho_mình_hỏi",
        "xin_hỏi", "ai", "bao_nhiêu", "mấy", "điều_kiện", "làm_sao", "a", "an", "the", "in", "on", "at"
    };

    public RagSearchService(IRagRepository ragRepository, ILogger<RagSearchService> logger)
    {
        _ragRepository = ragRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScoredChunk>> SearchRelevantChunksAsync(
        string query,
        string? programCode = null,
        string? campus = null,
        int topK = 4,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ScoredChunk>();
        }

        var normalizedQuery = query.Trim().ToLower();
        var allChunks = await _ragRepository.GetAllChunksAsync(programCode, campus, cancellationToken);

        if (allChunks.Count == 0)
        {
            return Array.Empty<ScoredChunk>();
        }

        var queryTerms = Regex.Split(normalizedQuery, @"\W+")
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var scoredList = new List<ScoredChunk>();

        foreach (var chunk in allChunks)
        {
            double score = 0;
            var docTitle = (chunk.Document?.Title ?? string.Empty).ToLower();
            var docType = (chunk.Document?.DocumentType ?? string.Empty).ToLower();
            var section = chunk.Section.ToLower();
            var content = chunk.Content.ToLower();
            var keywords = chunk.Keywords.ToLower();

            // 1. Exact phrase matching
            if (content.Contains(normalizedQuery))
            {
                score += 50.0;
            }

            // 2. Intent boosting
            if (normalizedQuery.Contains("học phí") || normalizedQuery.Contains("tiền") || normalizedQuery.Contains("tuition"))
            {
                if (docType == "tuition") score += 40.0;
            }

            if (normalizedQuery.Contains("vovinam") || normalizedQuery.Contains("năm nhất") || normalizedQuery.Contains("tiếng anh"))
            {
                if (docType == "firstyearsubjects" || content.Contains("vovinam")) score += 40.0;
            }

            if (normalizedQuery.Contains("hola") || normalizedQuery.Contains("hòa lạc") || normalizedQuery.Contains("xavalo") || normalizedQuery.Contains("xe buýt") || normalizedQuery.Contains("ký túc"))
            {
                if (docType == "slang" || docType == "facilities" || content.Contains("hola") || content.Contains("xavalo")) score += 35.0;
            }

            if (normalizedQuery.Contains("kinh nghiệm") || normalizedQuery.Contains("chia sẻ") || normalizedQuery.Contains("forum") || normalizedQuery.Contains("bài viết"))
            {
                if (docType == "forumpost") score += 30.0;
            }

            // Program code matching
            if (!string.IsNullOrEmpty(chunk.Document?.ProgramCode))
            {
                var pCode = chunk.Document.ProgramCode.ToLower();
                if (normalizedQuery.Contains(pCode) || 
                   (pCode.Contains("se") && (normalizedQuery.Contains("phần mềm") || normalizedQuery.Contains("software"))) ||
                   (pCode.Contains("ai") && normalizedQuery.Contains("trí tuệ nhân tạo")) ||
                   (pCode.Contains("ia") && (normalizedQuery.Contains("an toàn") || normalizedQuery.Contains("bảo mật"))))
                {
                    score += 30.0;
                }
            }

            // 3. Keyword / Term matching
            foreach (var term in queryTerms)
            {
                if (docTitle.Contains(term)) score += 12.0;
                if (section.Contains(term)) score += 8.0;
                if (keywords.Contains(term)) score += 6.0;

                // Content term matches (capped to avoid long text skew)
                int matches = Regex.Matches(content, Regex.Escape(term)).Count;
                if (matches > 0)
                {
                    score += Math.Min(matches, 5) * 3.0;
                }
            }

            if (score > 5.0)
            {
                scoredList.Add(new ScoredChunk
                {
                    Chunk = chunk,
                    Score = score
                });
            }
        }

        var results = scoredList
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .ToList();

        _logger.LogInformation("Tìm kiếm RAG '{Query}': trả về {Count} đoạn tri thức liên quan", query, results.Count);
        return results;
    }
}
