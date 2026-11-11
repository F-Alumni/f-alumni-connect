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
        "xin_hỏi", "ai", "bao_nhiêu", "mấy", "điều_kiện", "làm_sao", "không", "ko", "bao", "nhiêu",
        "mình", "bạn", "em", "anh", "chị", "xin", "muốn", "biết", "cần", "làm", "này", "đó",
        "nên", "nếu", "khi", "từ", "đến", "hay", "hoặc", "nhé", "ạ", "nhỉ", "ơi",
        "a", "an", "the", "in", "on", "at", "is", "are", "what", "how", "when", "where"
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
            bool hasExactPhrase = false;
            bool hasIntentBoost = false;
            var docTitle = (chunk.Document?.Title ?? string.Empty).ToLower();
            var docType = (chunk.Document?.DocumentType ?? string.Empty).ToLower();
            var section = chunk.Section.ToLower();
            var content = chunk.Content.ToLower();
            var keywords = chunk.Keywords.ToLower();

            // 1. Exact phrase matching
            if (content.Contains(normalizedQuery))
            {
                score += 50.0;
                hasExactPhrase = true;
            }

            // 2. Intent boosting & bilingual semantic hints
            if (normalizedQuery.Contains("học phí") || normalizedQuery.Contains("tiền") || normalizedQuery.Contains("tuition"))
            {
                if (docType == "tuition")
                {
                    score += 40.0;
                    hasIntentBoost = true;
                    if (content.Contains("vnd/semester") || content.Contains("first 3 semesters"))
                    {
                        score += 18.0;
                    }
                    if (normalizedQuery.Contains("chuyên ngành") && content.Contains("specialized"))
                    {
                        score += 12.0;
                    }
                }
            }

            if (normalizedQuery.Contains("vovinam") || normalizedQuery.Contains("năm nhất") || normalizedQuery.Contains("tiếng anh"))
            {
                if (docType == "firstyearsubjects" || content.Contains("vovinam"))
                {
                    score += 40.0;
                    hasIntentBoost = true;
                }
            }

            if (normalizedQuery.Contains("hola") || normalizedQuery.Contains("hòa lạc") || normalizedQuery.Contains("hà nội") ||
                normalizedQuery.Contains("xavalo") || normalizedQuery.Contains("hồ chí minh") || normalizedQuery.Contains("hcm") ||
                normalizedQuery.Contains("fuda") || normalizedQuery.Contains("đà nẵng") ||
                normalizedQuery.Contains("hovilo") || normalizedQuery.Contains("cần thơ") ||
                normalizedQuery.Contains("xe buýt") || normalizedQuery.Contains("ký túc"))
            {
                if (docType == "slang" || docType == "facilities" || content.Contains("hola") || content.Contains("xavalo") || content.Contains("fuda") || content.Contains("hovilo"))
                {
                    score += 35.0;
                    hasIntentBoost = true;
                }

                if ((normalizedQuery.Contains("hola") || normalizedQuery.Contains("hòa lạc") || normalizedQuery.Contains("hà nội")) && content.Contains("hola")) score += 20.0;
                if ((normalizedQuery.Contains("xavalo") || normalizedQuery.Contains("hồ chí minh") || normalizedQuery.Contains("hcm")) && content.Contains("xavalo")) score += 20.0;
                if ((normalizedQuery.Contains("fuda") || normalizedQuery.Contains("đà nẵng")) && content.Contains("fuda")) score += 20.0;
                if ((normalizedQuery.Contains("hovilo") || normalizedQuery.Contains("cần thơ")) && content.Contains("hovilo")) score += 20.0;
            }

            if (normalizedQuery.Contains("kinh nghiệm") || normalizedQuery.Contains("chia sẻ") || normalizedQuery.Contains("forum") || normalizedQuery.Contains("bài viết"))
            {
                if (docType == "forumpost")
                {
                    score += 30.0;
                    hasIntentBoost = true;
                }
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
                    hasIntentBoost = true;
                }
            }

            // 3. Whole-word Keyword / Term matching
            int matchedTerms = 0;
            bool hasMetadataMatch = false;
            foreach (var term in queryTerms)
            {
                var wordPattern = $@"(?<!\w){Regex.Escape(term)}(?!\w)";
                bool termMatched = false;

                if (Regex.IsMatch(docTitle, wordPattern)) { score += 12.0; termMatched = true; hasMetadataMatch = true; }
                if (Regex.IsMatch(section, wordPattern)) { score += 8.0; termMatched = true; hasMetadataMatch = true; }
                if (Regex.IsMatch(keywords, wordPattern)) { score += 6.0; termMatched = true; hasMetadataMatch = true; }

                // Content term matches (capped to avoid long text skew)
                int matches = Regex.Matches(content, wordPattern).Count;
                if (matches > 0)
                {
                    score += Math.Min(matches, 5) * 3.0;
                    termMatched = true;
                }

                if (termMatched) matchedTerms++;
            }

            // Deprioritize raw keyword-index blocks so descriptive paragraphs rank ahead in top-K citations
            if (section.StartsWith("rag keywords") || content.StartsWith("rag keywords"))
            {
                score -= 25.0;
            }

            // Reject weak incidental overlaps on out-of-scope multi-word queries
            if (!hasExactPhrase && !hasIntentBoost)
            {
                double coverage = queryTerms.Count > 0 ? (double)matchedTerms / queryTerms.Count : 0;
                if (queryTerms.Count >= 4)
                {
                    var passesMetadataRule = hasMetadataMatch && coverage >= 0.4 && score >= 25.0;
                    if (coverage < 0.6 && !passesMetadataRule)
                    {
                        continue;
                    }
                }
                else if (queryTerms.Count == 3 && (matchedTerms < 2 || coverage < 0.4))
                {
                    continue;
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
