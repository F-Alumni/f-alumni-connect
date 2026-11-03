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
        _geminiApiKey = configuration["Gemini:ApiKey"]
            ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
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

            var systemInstruction = "Bạn là FPT Alumni Assistant chuyên nghiệp. Quy tắc bắt buộc: " +
                "1. Trả lời CHÍNH XÁC, ĐI THẲNG VÀO TRỌNG TÂM, KHÔNG LAN MAN, KHÔNG BỊA ĐẶT. " +
                "2. CHỈ sử dụng thông tin trong NGỮ CẢNH TƯ LIỆU được cung cấp bên dưới. " +
                "3. Nêu rõ các số liệu cụ thể (học phí, tín chỉ, mã môn, quy định) nếu có. " +
                "4. Cuối câu trả lời, hãy chỉ rõ nguồn tham khảo đã trích dẫn.";

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
                    maxOutputTokens = 800
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
                        return text.Trim();
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
            return "Chào bạn, hiện tại cơ sở dữ liệu FPT Alumni Connect chưa có tài liệu hoặc bài viết nào trực tiếp đề cập đến nội dung bạn hỏi. Bạn có thể đăng câu hỏi lên Diễn đàn (Forum) để các bạn sinh viên và cựu sinh viên khác giải đáp nhé!";
        }

        var sb = new StringBuilder();
        var primaryChunk = chunks[0].Chunk;

        sb.AppendLine($"Dựa trên tài liệu quy chuẩn của Đại học FPT (**{primaryChunk.Document?.Title ?? "FPTU Documents"}** - *{primaryChunk.Section}*), thông tin cụ thể như sau:\n");

        var cleanContent = primaryChunk.Content.Trim();
        if (cleanContent.Length > 700)
        {
            cleanContent = cleanContent[..700] + "...";
        }

        sb.AppendLine(cleanContent);
        sb.AppendLine();

        if (chunks.Count > 1)
        {
            sb.AppendLine("**Các thông tin bổ trợ liên quan:**");
            for (int i = 1; i < Math.Min(chunks.Count, 3); i++)
            {
                var extra = chunks[i].Chunk;
                var shortSnippet = GetCleanSnippet(extra.Content, 180);
                sb.AppendLine($"- **{extra.Document?.Title}** (*{extra.Section}*): {shortSnippet}");
            }
        }

        return sb.ToString().Trim();
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
