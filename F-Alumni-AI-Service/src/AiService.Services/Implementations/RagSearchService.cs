using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using AiService.Repositories.Entities;
using AiService.Repositories.Interfaces;
using AiService.Services.DTOs.Responses;
using AiService.Services.Interfaces;

namespace AiService.Services.Implementations;

public class RagSearchService : IRagSearchService
{
    private readonly IRagRepository _ragRepository;
    private readonly ILogger<RagSearchService> _logger;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "la", "là", "cua", "của", "va", "và", "cac", "các", "nhung", "những", "thi", "thì",
        "ma", "mà", "o", "ở", "tai", "tại", "cho", "voi", "với", "duoc", "được", "co", "có",
        "gi", "gì", "nao", "nào", "sao", "the", "thế", "nhu", "như", "hoi", "hỏi", "ve", "về",
        "trong", "minh", "mình", "xin", "bao", "nhieu", "nhiêu", "may", "mấy", "lam", "làm",
        "nay", "này", "do", "đó", "a", "an", "in", "on", "at", "is", "are", "what", "how",
        "nha", "nhe", "nhé", "vậy", "vay", "ạ", "ơi", "oi", "ban", "bạn", "em", "toi", "tôi",
        "khong", "không", "can", "cần", "phai", "phải", "de", "để", "nhat", "nhất"
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
        var (_, results) = await EvaluateAsync(query, programCode, campus, topK, cancellationToken);
        return results;
    }

    public async Task<RagBoostDiagnosticsResponse> TestBoostAsync(
        string query,
        string? programCode = null,
        string? campus = null,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var (diagnostics, _) = await EvaluateAsync(query, programCode, campus, topK, cancellationToken);
        return diagnostics;
    }

    private async Task<(RagBoostDiagnosticsResponse Diagnostics, IReadOnlyList<ScoredChunk> Results)> EvaluateAsync(
        string query,
        string? programCode,
        string? campus,
        int topK,
        CancellationToken cancellationToken)
    {
        var diagnostics = new RagBoostDiagnosticsResponse
        {
            Query = query ?? string.Empty
        };

        if (string.IsNullOrWhiteSpace(query))
        {
            return (diagnostics, Array.Empty<ScoredChunk>());
        }

        var resolvedTopK = topK <= 0 ? 5 : Math.Min(topK, 50);

        var normalizedQuery = query.Trim().ToLowerInvariant();
        var unaccentedQuery = RemoveDiacritics(normalizedQuery);
        diagnostics.NormalizedQuery = unaccentedQuery;

        var queryTerms = Regex.Split(unaccentedQuery, @"[^\p{L}\p{N}_]+")
            .Where(t => t.Length > 1 && !StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        diagnostics.ExtractedTerms = queryTerms;

        var coreSearchPhrase = ExtractCorePhrase(unaccentedQuery);

        // Detect query intents cleanly without campus-name cross-contamination
        bool isTuitionIntent = ContainsAny(unaccentedQuery, "hoc phi", "tuition", "tien hoc", "dong tien", "nop tien", "hoc bong", "mien giam", "chi phi nhap hoc");
        bool isFirstYearIntent = ContainsAny(unaccentedQuery,
            "vovinam", "vo 1", "vo 2", "vo 3", "vov114", "vov124", "vov134",
            "nam nhat", "tieng anh", "quan su", "giao duc quoc phong", "qsu", "quan khu 7",
            "ren luyen", "the duc", "nhac cu", "dan bau", "dan nguyet", "dan ty ba",
            "dan nhi", "dan tranh", "sao truc", "dinh huong", "luk", "transition", "placement test");
        bool isFacilitiesIntent = ContainsAny(unaccentedQuery,
            "co so vat chat", "ky tuc", "ktx", "dom", "thu vien", "can tin", "canteen",
            "san bong", "xe buyt", "xe bus", "bai do xe", "gui xe", "phong gym", "khuon vien", "phong o");
        bool isRegulationsIntent = ContainsAny(unaccentedQuery,
            "bao luu", "tam nghi", "hoan tien do", "chuyen nganh", "chuyen co so", "chuyen combo",
            "fap", "thi tham dinh", "coursera", "diem danh", "thoi hoc", "canh bao hoc vu",
            "quy dinh", "qui dinh", "hoc vu", "thu tuc", "hoc lai", "thi lai", "bang diem", "hoc bong stem")
            || Regex.IsMatch(unaccentedQuery, @"\b(bl|hl)\b");
        bool isSlangIntent = (ContainsAny(unaccentedQuery, "ten goi", "biet danh", "slang", "la gi")
            || (unaccentedQuery.Contains("tai sao") && unaccentedQuery.Contains("goi"))
            || unaccentedQuery.Contains("y nghia ten"))
            && ContainsAny(unaccentedQuery, "hola", "xavalo", "hovilo", "fuda", "coc");
        bool isForumIntent = ContainsAny(unaccentedQuery, "kinh nghiem", "chia se", "review", "forum", "bai viet", "cuu sinh vien", "ojt", "thuc tap");
        bool hasExplicitCurriculumKeywords = ContainsAny(unaccentedQuery,
            "tin chi", "khung chuong trinh", "chuong trinh dao tao", "muc tieu", "chuan dau ra",
            "mon hoc", "chuyen nganh", "plo", "ky hoc", "hoc gi", "do an", "capstone", "dao tao");
        bool hasMajorMention = ContainsAny(unaccentedQuery,
            "phan mem", "tri tue nhan tao", "an toan thong tin", "do hoa", "my thuat so",
            "kinh doanh quoc te", "truyen thong", "marketing")
            || Regex.IsMatch(unaccentedQuery, @"\b(bit_se|bit_ai|bit_ia|bit_gd|bba_ib|bba_mc|bba_mkt|se|ia|gd|ib|mc|mkt|ktpm|attt)\b")
            || Regex.IsMatch(query, @"\bAI\b");
        bool hasOtherPrimaryIntent = isTuitionIntent || isFirstYearIntent || isFacilitiesIntent || isRegulationsIntent || isSlangIntent;
        bool isCurriculumIntent = hasExplicitCurriculumKeywords || (hasMajorMention && !hasOtherPrimaryIntent);

        var queryCampus = DetectCampusFromQuery(unaccentedQuery, null);
        var filterCampus = DetectCampusFromQuery(string.Empty, campus);
        var detectedCampus = queryCampus ?? filterCampus;

        var detectedProgram = InferProgramCode(unaccentedQuery, query, programCode);

        if (isTuitionIntent) diagnostics.DetectedIntents.Add("Tuition (+75)");
        if (isFirstYearIntent) diagnostics.DetectedIntents.Add("FirstYearSubjects (+75)");
        if (isFacilitiesIntent) diagnostics.DetectedIntents.Add("Facilities (+70)");
        if (isRegulationsIntent) diagnostics.DetectedIntents.Add("Regulations (+70)");
        if (isSlangIntent) diagnostics.DetectedIntents.Add("Slang/Alias (+35)");
        if (isForumIntent) diagnostics.DetectedIntents.Add("ForumPost (+35)");
        if (isCurriculumIntent) diagnostics.DetectedIntents.Add("Curriculum (+65)");
        if (!string.IsNullOrEmpty(queryCampus)) diagnostics.DetectedIntents.Add($"Campus:{queryCampus} (+30)");
        if (!string.IsNullOrEmpty(detectedProgram)) diagnostics.DetectedIntents.Add($"Program:{detectedProgram} (+40)");

        var allChunks = await _ragRepository.GetAllChunksAsync(null, null, cancellationToken);
        diagnostics.TotalCandidateChunks = allChunks.Count;

        if (allChunks.Count == 0)
        {
            return (diagnostics, Array.Empty<ScoredChunk>());
        }

        var scoredList = new List<ScoredChunk>();

        foreach (var chunk in allChunks)
        {
            double score = 0;
            var matchedIntents = new List<string>();
            bool hasExactPhrase = false;
            bool hasIntentBoost = false;

            var docTitle = (chunk.Document?.Title ?? string.Empty).ToLowerInvariant();
            var docType = (chunk.Document?.DocumentType ?? string.Empty).ToLowerInvariant();
            var docProgram = (chunk.Document?.ProgramCode ?? string.Empty).ToLowerInvariant();
            var section = chunk.Section.ToLowerInvariant();
            var content = chunk.Content.ToLowerInvariant();
            var keywords = chunk.Keywords.ToLowerInvariant();

            var docTitleFlat = RemoveDiacritics(docTitle);
            var sectionFlat = RemoveDiacritics(section);
            var contentFlat = RemoveDiacritics(content);
            var keywordsFlat = RemoveDiacritics(keywords);

            // 1. Exact or core phrase matching
            if (content.Contains(normalizedQuery) || contentFlat.Contains(unaccentedQuery) ||
                (coreSearchPhrase.Length >= 8 && (contentFlat.Contains(coreSearchPhrase) || sectionFlat.Contains(coreSearchPhrase))))
            {
                score += 50.0;
                hasExactPhrase = true;
                matchedIntents.Add("ExactPhrase (+50)");
            }

            // 2. Intent boosting
            if (isTuitionIntent && docType == "tuition")
            {
                score += 75.0;
                hasIntentBoost = true;
                matchedIntents.Add("TuitionBoost (+75)");
            }

            if (isFirstYearIntent && (docType == "firstyearsubjects" || ContainsAny(contentFlat, "vovinam", "nhac cu", "quoc phong", "qsu")))
            {
                score += 75.0;
                hasIntentBoost = true;
                matchedIntents.Add("FirstYearBoost (+75)");
            }

            if (isFacilitiesIntent && (docType == "facilities" || ContainsAny(sectionFlat, "dormitory", "ktx", "library", "canteen", "facilities")))
            {
                score += 70.0;
                hasIntentBoost = true;
                matchedIntents.Add("FacilitiesBoost (+70)");
            }

            if (isRegulationsIntent && (docType == "regulations" || ContainsAny(sectionFlat, "regulation", "suspension", "transfer", "fap", "coursera")))
            {
                score += 70.0;
                hasIntentBoost = true;
                matchedIntents.Add("RegulationsBoost (+70)");
            }

            if (isSlangIntent && (sectionFlat.Contains("biet danh") || contentFlat.Contains("ten goi khac")))
            {
                score += 35.0;
                hasIntentBoost = true;
                matchedIntents.Add("SlangBoost (+35)");
            }

            if (isForumIntent && docType == "forumpost")
            {
                score += 35.0;
                hasIntentBoost = true;
                matchedIntents.Add("ForumBoost (+35)");
            }

            if (isCurriculumIntent && docType == "curriculum")
            {
                score += 65.0;
                hasIntentBoost = true;
                matchedIntents.Add("CurriculumBoost (+65)");
            }

            // Program code boosting (for curriculum intent or forum posts, avoids hijacking tuition/facilities queries)
            if (!string.IsNullOrEmpty(detectedProgram) && !string.IsNullOrEmpty(docProgram) &&
                (isCurriculumIntent || docType == "forumpost"))
            {
                var normalizedDocProgram = NormalizeProgramCode(docProgram);
                if (string.Equals(normalizedDocProgram, detectedProgram, StringComparison.OrdinalIgnoreCase))
                {
                    score += 40.0;
                    hasIntentBoost = true;
                    matchedIntents.Add($"ProgramMatch:{detectedProgram} (+40)");
                }
            }

            // 3. Keyword / Term matching (diacritics-folded with word boundary safety for short tokens)
            int matchedTerms = 0;
            bool hasMetadataMatch = false;
            foreach (var term in queryTerms)
            {
                bool useWordBoundary = term.Length <= 3;
                bool termMatched = false;

                if (ContainsTerm(docTitleFlat, term, useWordBoundary)) { score += 8.0; termMatched = true; hasMetadataMatch = true; }
                if (ContainsTerm(sectionFlat, term, useWordBoundary)) { score += 6.0; termMatched = true; hasMetadataMatch = true; }
                if (ContainsTerm(keywordsFlat, term, useWordBoundary)) { score += 4.0; termMatched = true; hasMetadataMatch = true; }

                var pattern = useWordBoundary ? $@"\b{Regex.Escape(term)}\b" : Regex.Escape(term);
                int matches = Regex.Matches(contentFlat, pattern).Count;
                if (matches > 0)
                {
                    score += Math.Min(matches, 3) * 2.0;
                    termMatched = true;
                }

                if (termMatched) matchedTerms++;
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

            // 4. Campus section/content matching boost (applied only to chunks with existing relevance)
            if (score > 0 && !string.IsNullOrEmpty(detectedCampus) && ChunkMatchesCampus(detectedCampus, sectionFlat, contentFlat))
            {
                score += 30.0;
                matchedIntents.Add($"CampusMatch:{detectedCampus} (+30)");
            }

            // 5. Deprioritize pure keyword-tag chunks so substantive explanatory sections rank higher
            if (sectionFlat.Contains("rag keywords") || sectionFlat.EndsWith("keywords"))
            {
                score -= 25.0;
            }

            var minScore = (hasExactPhrase || hasIntentBoost) ? 5.0 : 15.0;
            if (score >= minScore)
            {
                scoredList.Add(new ScoredChunk
                {
                    Chunk = chunk,
                    Score = score,
                    MatchedIntent = matchedIntents.Count > 0 ? string.Join(", ", matchedIntents) : "LexicalMatch"
                });
            }
        }

        var results = scoredList
            .OrderByDescending(s => s.Score)
            .Take(resolvedTopK)
            .ToList();

        diagnostics.TopResults = results.Select(r => new ScoredChunkDetail
        {
            Score = r.Score,
            DocumentTitle = r.Chunk.Document?.Title ?? string.Empty,
            DocumentType = r.Chunk.Document?.DocumentType ?? string.Empty,
            ProgramCode = r.Chunk.Document?.ProgramCode,
            Campus = r.Chunk.Document?.Campus ?? "All",
            Section = r.Chunk.Section,
            MatchedIntent = r.MatchedIntent,
            Snippet = r.Chunk.Content.Length > 260 ? r.Chunk.Content[..260] + "..." : r.Chunk.Content
        }).ToList();

        _logger.LogInformation("Tìm kiếm RAG '{Query}': trả về {Count} đoạn tri thức liên quan (Top score: {TopScore})",
            query, results.Count, results.FirstOrDefault()?.Score ?? 0);

        return (diagnostics, results);
    }

    private static string ExtractCorePhrase(string unaccentedQuery)
    {
        var cleaned = Regex.Replace(unaccentedQuery, @"[^\p{L}\p{N}\s_]", " ");
        cleaned = Regex.Replace(cleaned, @"\b(xin|cho minh hoi|cho em hoi|cho toi hoi|cho hoi|la bao nhieu|bao nhieu|nhu the nao|the nao|la gi|co bat buoc khong|bat buoc khong|co duoc khong|o dau|khi nao|nhung gi|giup em|giup minh|ad oi|ban oi|vay|nha|nhe)\b", " ");
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private static string? DetectCampusFromQuery(string unaccentedQuery, string? explicitCampus)
    {
        if (ContainsAny(unaccentedQuery, "hola", "hoa lac", "ha noi", "fu-hl", "fuhl")) return "FU-HL";
        if (ContainsAny(unaccentedQuery, "xavalo", "ho chi minh", "sai gon", "fu-hcm", "fuhcm") || Regex.IsMatch(unaccentedQuery, @"\bhcm\b")) return "FU-HCM";
        if (ContainsAny(unaccentedQuery, "fuda", "da nang", "fu-dn", "fudn")) return "FU-DN";
        if (ContainsAny(unaccentedQuery, "hovilo", "can tho", "fu-ct", "fuct")) return "FU-CT";
        if (ContainsAny(unaccentedQuery, "quy nhon", "fu-qn", "fuqn")) return "FU-QN";

        if (!string.IsNullOrWhiteSpace(explicitCampus) && !explicitCampus.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var c = RemoveDiacritics(explicitCampus.Trim().ToLowerInvariant());
            if (c.Contains("hl") || c.Contains("hola") || c.Contains("hoa lac") || c.Contains("ha noi")) return "FU-HL";
            if (c.Contains("hcm") || c.Contains("xavalo") || c.Contains("ho chi minh") || c.Contains("sai gon")) return "FU-HCM";
            if (c.Contains("dn") || c.Contains("fuda") || c.Contains("da nang")) return "FU-DN";
            if (c.Contains("ct") || c.Contains("hovilo") || c.Contains("can tho")) return "FU-CT";
            if (c.Contains("qn") || c.Contains("quy nhon")) return "FU-QN";
        }

        return null;
    }

    private static string? InferProgramCode(string unaccentedQuery, string rawQuery, string? explicitProgram)
    {
        if (!string.IsNullOrWhiteSpace(explicitProgram) && !explicitProgram.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            return NormalizeProgramCode(explicitProgram);
        }

        if (Regex.IsMatch(unaccentedQuery, @"\b(bit_se|se|ktpm)\b") || ContainsAny(unaccentedQuery, "phan mem", "software", "ky thuat phan mem")) return "BIT_SE";
        if (Regex.IsMatch(unaccentedQuery, @"\b(bit_ai)\b") || Regex.IsMatch(rawQuery, @"\bAI\b") || ContainsAny(unaccentedQuery, "tri tue nhan tao", "artificial intelligence", "nganh ai", "chuyen nganh ai")) return "BIT_AI";
        if (Regex.IsMatch(unaccentedQuery, @"\b(bit_ia|ia|attt)\b") || ContainsAny(unaccentedQuery, "an toan thong tin", "bao mat", "information assurance")) return "BIT_IA";
        if (Regex.IsMatch(unaccentedQuery, @"\b(bit_gd|gd)\b") || ContainsAny(unaccentedQuery, "do hoa", "my thuat so", "thiet ke do hoa", "graphic design")) return "BIT_GD";
        if (Regex.IsMatch(unaccentedQuery, @"\b(bba_ib|ib)\b") || ContainsAny(unaccentedQuery, "kinh doanh quoc te", "kdqt", "international business")) return "BBA_IB";
        if (Regex.IsMatch(unaccentedQuery, @"\b(bba_mc|mc)\b") || ContainsAny(unaccentedQuery, "truyen thong da phuong tien", "truyen thong", "multimedia")) return "BBA_MC";
        if (Regex.IsMatch(unaccentedQuery, @"\b(bba_mkt|mkt)\b") || ContainsAny(unaccentedQuery, "marketing", "digital marketing", "marketing so")) return "BBA_MKT";

        return null;
    }

    private static string NormalizeProgramCode(string programCode)
    {
        var p = programCode.Trim().ToUpperInvariant();
        if (p == "SE" || p.StartsWith("BIT_SE") || p == "KTPM") return "BIT_SE";
        if (p == "AI" || p.StartsWith("BIT_AI")) return "BIT_AI";
        if (p == "IA" || p.StartsWith("BIT_IA") || p == "ATTT") return "BIT_IA";
        if (p == "GD" || p.StartsWith("BIT_GD")) return "BIT_GD";
        if (p == "IB" || p.StartsWith("BBA_IB") || p == "KDQT") return "BBA_IB";
        if (p == "MC" || p.StartsWith("BBA_MC")) return "BBA_MC";
        if (p == "MKT" || p.StartsWith("BBA_MKT")) return "BBA_MKT";
        return p;
    }

    private static bool ChunkMatchesCampus(string campusCode, string sectionFlat, string contentFlat)
    {
        return campusCode switch
        {
            "FU-HL" => ContainsAny(sectionFlat, "hola", "hoa lac", "ha noi") || ContainsAny(contentFlat, "hola", "hoa lac", "ha noi"),
            "FU-HCM" => ContainsAny(sectionFlat, "xavalo", "hcm", "ho chi minh", "sai gon", "tp.hcm") || ContainsAny(contentFlat, "xavalo", "fu-hcm", "tp.hcm", "tphcm", "ho chi minh", "sai gon", "quan 9"),
            "FU-DN" => ContainsAny(sectionFlat, "fuda", "da nang") || ContainsAny(contentFlat, "fuda", "da nang"),
            "FU-CT" => ContainsAny(sectionFlat, "hovilo", "can tho") || ContainsAny(contentFlat, "hovilo", "can tho"),
            "FU-QN" => ContainsAny(sectionFlat, "quy nhon") || ContainsAny(contentFlat, "quy nhon"),
            _ => false
        };
    }

    private static bool ContainsTerm(string text, string term, bool useWordBoundary)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term)) return false;
        return useWordBoundary
            ? Regex.IsMatch(text, $@"\b{Regex.Escape(term)}\b")
            : text.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var n in needles)
        {
            if (text.Contains(n, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    internal static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch switch
                {
                    'đ' => 'd',
                    'Đ' => 'D',
                    _ => ch
                });
            }
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
