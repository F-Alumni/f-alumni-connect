using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AiService.Repositories.Entities;
using AiService.Repositories.Interfaces;
using AiService.Services.DTOs.Requests;
using AiService.Services.DTOs.Responses;
using AiService.Services.Exceptions;
using AiService.Services.Interfaces;

namespace AiService.Services.Implementations;

public class AiChatService : IAiChatService
{
    public const int MaxAnswerCharacters = 255;

    private const string NoMaterialFallbackMessage =
        "Hiện chưa có thông tin trong tài liệu. Sinh viên vui lòng gọi hoặc email cho phòng DVSV, hoặc truy cập daihoc.fpt.edu.vn để tìm kiếm thông tin.";

    private readonly IRagRepository _ragRepository;
    private readonly IRagSearchService _ragSearchService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiChatService> _logger;
    private readonly string? _geminiApiKey;
    private readonly string _geminiModel;

    public AiChatService(
        IRagRepository ragRepository,
        IRagSearchService ragSearchService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiChatService> logger)
    {
        _ragRepository = ragRepository;
        _ragSearchService = ragSearchService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        var configuredGeminiKey = configuration["Gemini:ApiKey"];
        _geminiApiKey = !string.IsNullOrWhiteSpace(configuredGeminiKey)
            ? configuredGeminiKey
            : Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        _geminiModel = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
    }

    public async Task<AiChatResponse> AskAsync(AiChatRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        // 1. Get or create conversation
        AiConversation? conversation = null;
        if (request.ConversationId.HasValue)
        {
            conversation = await _ragRepository.GetConversationAsync(request.ConversationId.Value, userId, cancellationToken);
        }

        if (conversation == null)
        {
            var title = request.Question.Length > 50 ? request.Question[..50] + "..." : request.Question;
            conversation = await _ragRepository.CreateConversationAsync(new AiConversation
            {
                UserId = userId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            }, cancellationToken);
        }

        // 2. Retrieve relevant context chunks via RAG engine
        var relevantChunks = await _ragSearchService.SearchRelevantChunksAsync(
            request.Question,
            request.Major,
            request.Campus,
            topK: 4,
            cancellationToken);

        // 3. Build citations list
        var citations = relevantChunks.Select(rc => new CitationResponse
        {
            SourceTitle = rc.Chunk.Document?.Title ?? "Tài liệu FPTU",
            Section = rc.Chunk.Section,
            ProgramCode = rc.Chunk.Document?.ProgramCode,
            Snippet = GetCleanSnippet(rc.Chunk.Content, 250)
        }).ToList();

        // 4. Generate answer: Gemini if configured, otherwise high-accuracy local synthesis
        string answer;
        if (!string.IsNullOrWhiteSpace(_geminiApiKey) && relevantChunks.Count > 0)
        {
            answer = await GenerateWithGeminiAsync(request.Question, relevantChunks, conversation.Messages, cancellationToken);
        }
        else
        {
            answer = GenerateLocalGroundedAnswer(request.Question, relevantChunks);
        }

        // 5. Persist messages in conversation history
        var userMsg = new AiMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = request.Question,
            CreatedAt = DateTime.UtcNow
        };
        await _ragRepository.AddMessageAsync(userMsg, cancellationToken);

        var assistantMsg = new AiMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = answer,
            CitationsJson = JsonSerializer.Serialize(citations),
            CreatedAt = DateTime.UtcNow
        };
        await _ragRepository.AddMessageAsync(assistantMsg, cancellationToken);

        return new AiChatResponse
        {
            ConversationId = conversation.Id,
            Answer = answer,
            Citations = citations,
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<AiConversationResponse> GetConversationHistoryAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var conv = await _ragRepository.GetConversationAsync(conversationId, userId, cancellationToken);
        if (conv == null)
        {
            throw new NotFoundException("Conversation", conversationId);
        }

        return new AiConversationResponse
        {
            Id = conv.Id,
            Title = conv.Title,
            CreatedAt = conv.CreatedAt,
            LastMessageAt = conv.LastMessageAt,
            Messages = conv.Messages.Select(m => new AiMessageResponse
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                Citations = DeserializeCitations(m.CitationsJson),
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<AiConversationResponse>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var convs = await _ragRepository.GetConversationsByUserAsync(userId, cancellationToken);
        return convs.Select(c => new AiConversationResponse
        {
            Id = c.Id,
            Title = c.Title,
            CreatedAt = c.CreatedAt,
            LastMessageAt = c.LastMessageAt,
            Messages = new List<AiMessageResponse>()
        }).ToList();
    }

    private async Task<string> GenerateWithGeminiAsync(
        string question,
        IReadOnlyList<ScoredChunk> contextChunks,
        IEnumerable<AiMessage> history,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_geminiModel}:generateContent?key={_geminiApiKey}";

            var sbContext = new StringBuilder();
            sbContext.AppendLine("NGỮ CẢNH TƯ LIỆU THỰC TẾ (TÀI LIỆU FPTU & BÀI ĐĂNG FORUM):");
            int i = 1;
            foreach (var sc in contextChunks)
            {
                sbContext.AppendLine($"[Tư liệu {i++} - Nguồn: {sc.Chunk.Document?.Title} | Mục: {sc.Chunk.Section}]");
                sbContext.AppendLine(sc.Chunk.Content);
                sbContext.AppendLine();
            }

            var systemInstruction =
                "Bạn là FPT Alumni Assistant. Quy tắc bắt buộc: " +
                $"1. Trả lời CỰC KỲ NGẮN GỌN, ĐÚNG TRỌNG TÂM, TỐI ĐA {MaxAnswerCharacters} KÝ TỰ (tính cả khoảng trắng và dấu câu) cho mỗi lần trả lời. " +
                "2. CHỈ sử dụng thông tin trong NGỮ CẢNH TƯ LIỆU bên dưới, tuyệt đối KHÔNG bịa đặt hay suy diễn. " +
                "3. Nêu thẳng số liệu/quy định cốt lõi, không chào hỏi rườm rà hay liệt kê nguồn dài dòng trong câu trả lời. " +
                $"4. Nếu tư liệu không có câu trả lời cho câu hỏi, chỉ trả lời đúng: \"{NoMaterialFallbackMessage}\"";

            var promptText = $"{systemInstruction}\n\n{sbContext}\n\nCÂU HỎI CỦA NGƯỜI DÙNG: {question}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = promptText }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 200
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseJson);
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return EnforceMaxLength(text, MaxAnswerCharacters);
                    }
                }
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini API returned error: {StatusCode} - {Body}. Falling back to local synthesizer.", response.StatusCode, err);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini call failed. Falling back to local synthesizer.");
        }

        return GenerateLocalGroundedAnswer(question, contextChunks);
    }

    private static string GenerateLocalGroundedAnswer(string question, IReadOnlyList<ScoredChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return NoMaterialFallbackMessage;
        }

        var primaryChunk = chunks
            .FirstOrDefault(c => !c.Chunk.Section.Contains("RAG KEYWORDS", StringComparison.OrdinalIgnoreCase))?.Chunk
            ?? chunks[0].Chunk;
        var cleanContent = CleanMarkdownForCompactAnswer(primaryChunk.Content);
        if (string.IsNullOrWhiteSpace(cleanContent))
        {
            return NoMaterialFallbackMessage;
        }

        return EnforceMaxLength(cleanContent, MaxAnswerCharacters);
    }

    internal static string CleanMarkdownForCompactAnswer(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Strip common markdown headings/bold markers/section dividers so compact answers stay readable
        var cleaned = text
            .Replace("\r\n", "\n")
            .Replace("**", string.Empty)
            .Replace("##", string.Empty)
            .Replace("###", string.Empty);

        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[=-]{3,}", " ");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\n+", " ");
        return System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    internal static string EnforceMaxLength(string text, int maxLength = MaxAnswerCharacters)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return NoMaterialFallbackMessage;
        }

        var normalized = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s+", " ");
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        var limit = Math.Max(1, maxLength - 3);
        var candidate = normalized[..limit];
        var lastSpace = candidate.LastIndexOf(' ');
        if (lastSpace > limit / 2)
        {
            candidate = candidate[..lastSpace];
        }

        return candidate.TrimEnd(' ', ',', ';', ':') + "...";
    }

    private static string GetCleanSnippet(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var clean = text.Replace("\n", " ").Replace("\r", " ").Trim();
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ");
        return clean.Length > maxLength ? clean[..maxLength] + "..." : clean;
    }

    private static List<CitationResponse> DeserializeCitations(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<CitationResponse>>(json) ?? new List<CitationResponse>();
        }
        catch
        {
            return new List<CitationResponse>();
        }
    }
}
