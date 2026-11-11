using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using AiService.Repositories.Entities;
using AiService.Repositories.Interfaces;
using AiService.Services.DTOs.Requests;
using AiService.Services.Implementations;
using AiService.Services.Interfaces;

namespace AiService.Tests.Services;

public class AiServiceTests
{
    private class FakeRagRepository : IRagRepository
    {
        public readonly List<RagDocument> Documents = new();
        public readonly List<RagChunk> Chunks = new();
        public readonly List<AiConversation> Conversations = new();
        public readonly List<AiMessage> Messages = new();
        public readonly List<MentorProfile> Mentors = new();

        public Task AddChunksAsync(IEnumerable<RagChunk> chunks, CancellationToken cancellationToken = default)
        {
            Chunks.AddRange(chunks);
            return Task.CompletedTask;
        }

        public Task<RagDocument> AddDocumentAsync(RagDocument doc, CancellationToken cancellationToken = default)
        {
            Documents.Add(doc);
            return Task.FromResult(doc);
        }

        public Task AddMentorAsync(MentorProfile mentor, CancellationToken cancellationToken = default)
        {
            Mentors.Add(mentor);
            return Task.CompletedTask;
        }

        public Task AddMessageAsync(AiMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            var conv = Conversations.FirstOrDefault(c => c.Id == message.ConversationId);
            if (conv != null)
            {
                conv.Messages.Add(message);
                conv.LastMessageAt = message.CreatedAt;
            }
            return Task.CompletedTask;
        }

        public Task<AiConversation> CreateConversationAsync(AiConversation conversation, CancellationToken cancellationToken = default)
        {
            Conversations.Add(conversation);
            return Task.FromResult(conversation);
        }

        public Task DeleteDocumentByPathAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            var doc = Documents.FirstOrDefault(d => d.SourcePath == sourcePath);
            if (doc != null)
            {
                Documents.Remove(doc);
                Chunks.RemoveAll(c => c.DocumentId == doc.Id);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RagChunk>> GetAllChunksAsync(string? programCode = null, string? campus = null, CancellationToken cancellationToken = default)
        {
            foreach (var c in Chunks)
            {
                if (c.Document == null)
                {
                    c.Document = Documents.FirstOrDefault(d => d.Id == c.DocumentId);
                }
            }
            return Task.FromResult<IReadOnlyList<RagChunk>>(Chunks);
        }

        public Task<int> GetChunkCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Chunks.Count);
        }

        public Task<AiConversation?> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Conversations.FirstOrDefault(c => c.Id == conversationId && c.UserId == userId));
        }

        public Task<IReadOnlyList<AiConversation>> GetConversationsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AiConversation>>(Conversations.Where(c => c.UserId == userId).ToList());
        }

        public Task<RagDocument?> GetDocumentByPathAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Documents.FirstOrDefault(d => d.SourcePath == sourcePath));
        }

        public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Documents.Count);
        }

        public Task<IReadOnlyList<MentorProfile>> GetMentorsAsync(string? major = null, CancellationToken cancellationToken = default)
        {
            var q = Mentors.AsEnumerable();
            if (!string.IsNullOrEmpty(major) && major != "All") q = q.Where(m => m.Major == major);
            return Task.FromResult<IReadOnlyList<MentorProfile>>(q.ToList());
        }
    }

    [Fact]
    public async Task RagSearchService_Matches_Tuition_Chunk_Accurately()
    {
        var repo = new FakeRagRepository();
        var doc = new RagDocument
        {
            Title = "Học phí Đại học FPT",
            SourcePath = "Hoc_phi.docx",
            DocumentType = "Tuition",
            Campus = "All"
        };
        await repo.AddDocumentAsync(doc);

        var chunk = new RagChunk
        {
            DocumentId = doc.Id,
            Document = doc,
            ChunkIdentifier = "chunk_tuition_hola",
            Section = "Học phí chuyên ngành tại Hola và Xavalo",
            Content = "Học phí chương trình đại học chính quy tại Campus Hola là 28,700,000 VNĐ một học kỳ chuyên ngành.",
            Keywords = "học phí hola xavalo học kỳ"
        };
        await repo.AddChunksAsync(new[] { chunk });

        var searchService = new RagSearchService(repo, NullLogger<RagSearchService>.Instance);
        var results = await searchService.SearchRelevantChunksAsync("Cho mình hỏi học phí tại Hola là bao nhiêu?");

        Assert.NotEmpty(results);
        Assert.Equal("chunk_tuition_hola", results[0].Chunk.ChunkIdentifier);
    }

    [Fact]
    public async Task AiChatService_MultiTurn_Conversation_And_Citations_Success()
    {
        var repo = new FakeRagRepository();
        var doc = new RagDocument
        {
            Title = "Khung chương trình Kỹ thuật phần mềm (BIT_SE_K19B)",
            SourcePath = "BIT_SE_K19B.docx",
            DocumentType = "Curriculum",
            ProgramCode = "BIT_SE",
            Campus = "All"
        };
        await repo.AddDocumentAsync(doc);

        var chunk = new RagChunk
        {
            DocumentId = doc.Id,
            Document = doc,
            ChunkIdentifier = "chunk_se_objectives",
            Section = "1. Mục tiêu đào tạo Kỹ thuật phần mềm",
            Content = "Đào tạo cử nhân ngành CNTT chuyên ngành Kỹ thuật phần mềm (KTPM) với tổng số 145 tín chỉ chuyên môn.",
            Keywords = "BIT_SE KTPM 145 tín chỉ"
        };
        await repo.AddChunksAsync(new[] { chunk });

        var searchService = new RagSearchService(repo, NullLogger<RagSearchService>.Instance);
        var config = new ConfigurationBuilder().Build();
        var chatService = new AiChatService(
            repo,
            searchService,
            new System.Net.Http.HttpClient().AsFactory(),
            config,
            NullLogger<AiChatService>.Instance);

        var userId = Guid.NewGuid();

        // Turn 1
        var res1 = await chatService.AskAsync(new AiChatRequest
        {
            Question = "Ngành Kỹ thuật phần mềm BIT_SE cần hoàn thành bao nhiêu tín chỉ?",
            Major = "BIT_SE"
        }, userId);

        Assert.NotNull(res1);
        Assert.NotEqual(Guid.Empty, res1.ConversationId);
        Assert.NotEmpty(res1.Citations);
        Assert.Contains("145 tín chỉ", res1.Answer);

        // Turn 2: Follow up question in the same conversation (AI-04)
        var res2 = await chatService.AskAsync(new AiChatRequest
        {
            Question = "Mục tiêu cụ thể của ngành này là gì?",
            ConversationId = res1.ConversationId,
            Major = "BIT_SE"
        }, userId);

        Assert.Equal(res1.ConversationId, res2.ConversationId);
        var history = await chatService.GetConversationHistoryAsync(res1.ConversationId, userId);
        Assert.Equal(4, history.Messages.Count); // 2 user msgs + 2 assistant msgs
    }

    [Fact]
    public async Task MentorRecommendation_Matches_By_Major_And_Skills()
    {
        var repo = new FakeRagRepository();
        await repo.AddMentorAsync(new MentorProfile
        {
            FullName = "Nguyễn Hữu Trí",
            Email = "tri@alumni.fpt.edu.vn",
            Major = "SE",
            CurrentCompany = "FPT Software",
            Skills = "C#, .NET 10, Microservices, Kafka"
        });

        await repo.AddMentorAsync(new MentorProfile
        {
            FullName = "Lê Hoàng Yến",
            Email = "yen@alumni.fpt.edu.vn",
            Major = "AI",
            CurrentCompany = "VinAI",
            Skills = "Python, PyTorch, LLM, RAG"
        });

        var service = new MentorRecommendationService(repo, NullLogger<MentorRecommendationService>.Instance);
        var recommendations = await service.RecommendMentorsAsync(new RecommendMentorRequest
        {
            Major = "SE",
            TargetSkills = new List<string> { ".NET", "C#" }
        });

        Assert.NotEmpty(recommendations);
        Assert.Equal("Nguyễn Hữu Trí", recommendations[0].FullName);
        Assert.True(recommendations[0].MatchScore > recommendations[1].MatchScore);
    }

    [Fact]
    public async Task RagIngestion_RealDirectory_Ingests_And_Queries_Correctly()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data", "rag"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "rag"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "data", "rag"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "data", "rag")
        };

        var ragDir = candidates.FirstOrDefault(d => Directory.Exists(d) && Directory.GetFiles(d, "*.docx", SearchOption.AllDirectories).Length > 0);
        Assert.True(!string.IsNullOrEmpty(ragDir) && Directory.Exists(ragDir), "Thư mục tài liệu RAG data/rag phải tồn tại và chứa file .docx");

        var repo = new FakeRagRepository();
        var ingestionService = new RagIngestionService(repo, NullLogger<RagIngestionService>.Instance);
        var result = await ingestionService.IngestDocumentsFromDirectoryAsync(ragDir, forceReindex: true);

        Assert.True(result.TotalFilesProcessed > 0);
        Assert.True(result.TotalChunksCreated > 0);

        var searchService = new RagSearchService(repo, NullLogger<RagSearchService>.Instance);
        
        // Test query about tuition at Hola
        var tuitionResults = await searchService.SearchRelevantChunksAsync("Học phí chuyên ngành tại Hola là bao nhiêu?");
        Assert.NotEmpty(tuitionResults);
        Assert.Contains("31,600,000", tuitionResults[0].Chunk.Content);

        // Test query about BIT_SE curriculum
        var seResults = await searchService.SearchRelevantChunksAsync("Kỹ thuật phần mềm BIT_SE đào tạo những gì?");
        Assert.NotEmpty(seResults);

        // Test query about first year subjects Vovinam
        var vovinamResults = await searchService.SearchRelevantChunksAsync("Môn Vovinam có bắt buộc không?");
        Assert.NotEmpty(vovinamResults);

        // Test out-of-scope query across ALL 12 real .docx files should return empty so DVSV/website fallback triggers
        var unrelatedResults = await searchService.SearchRelevantChunksAsync("Giá vé máy bay đi Nhật tháng sau là bao nhiêu?");
        Assert.Empty(unrelatedResults);
    }

    [Fact]
    public async Task AiChatService_Enforces_Max_255_Characters_When_Chunk_Is_Long()
    {
        var repo = new FakeRagRepository();
        var doc = new RagDocument
        {
            Title = "Quy chế đào tạo",
            SourcePath = "reg.docx",
            DocumentType = "Regulations",
            Campus = "All"
        };
        await repo.AddDocumentAsync(doc);

        var longText = "## Quy định thực tập OJT\n" + string.Join(" ", Enumerable.Repeat("Sinh viên cần hoàn thành đầy đủ học phần nền tảng trước kỳ OJT tại doanh nghiệp đối tác.", 6));
        await repo.AddChunksAsync(new[]
        {
            new RagChunk
            {
                DocumentId = doc.Id,
                Document = doc,
                ChunkIdentifier = "ojt_long",
                Section = "OJT",
                Content = longText,
                Keywords = "ojt thực tập doanh nghiệp"
            }
        });

        var searchService = new RagSearchService(repo, NullLogger<RagSearchService>.Instance);
        var chatService = new AiChatService(
            repo,
            searchService,
            new System.Net.Http.HttpClient().AsFactory(),
            new ConfigurationBuilder().Build(),
            NullLogger<AiChatService>.Instance);

        var response = await chatService.AskAsync(new AiChatRequest
        {
            Question = "Quy định thực tập OJT là gì?"
        }, Guid.NewGuid());

        Assert.InRange(response.Answer.Length, 1, AiChatService.MaxAnswerCharacters);
        Assert.EndsWith("...", response.Answer);
    }

    [Fact]
    public async Task AiChatService_When_No_Material_Match_Instructs_DVSV_And_Website()
    {
        var repo = new FakeRagRepository();
        var doc = new RagDocument
        {
            Title = "Các môn năm nhất",
            SourcePath = "first-year.docx",
            DocumentType = "FirstYearSubjects",
            Campus = "All"
        };
        await repo.AddDocumentAsync(doc);
        await repo.AddChunksAsync(new[]
        {
            new RagChunk
            {
                DocumentId = doc.Id,
                Document = doc,
                ChunkIdentifier = "vovinam_01",
                Section = "Vovinam",
                Content = "Môn học Vovinam là môn thể chất bắt buộc, không được miễn trừ nếu không có lý do y tế.",
                Keywords = "vovinam thể chất"
            }
        });

        var searchService = new RagSearchService(repo, NullLogger<RagSearchService>.Instance);
        var chatService = new AiChatService(
            repo,
            searchService,
            new System.Net.Http.HttpClient().AsFactory(),
            new ConfigurationBuilder().Build(),
            NullLogger<AiChatService>.Instance);

        var response = await chatService.AskAsync(new AiChatRequest
        {
            Question = "Hôm nay trời có mưa không?"
        }, Guid.NewGuid());

        Assert.Empty(response.Citations);
        Assert.InRange(response.Answer.Length, 1, AiChatService.MaxAnswerCharacters);
        Assert.Contains("DVSV", response.Answer);
        Assert.Contains("daihoc.fpt.edu.vn", response.Answer);
    }

    [Fact]
    public async Task AiChatService_GeminiPrompt_Instructs_255_Char_Limit_And_DVSV_Fallback()
    {
        var repo = new FakeRagRepository();
        var doc = new RagDocument
        {
            Title = "Học phí FPTU",
            SourcePath = "tuition.docx",
            DocumentType = "Tuition",
            Campus = "All"
        };
        await repo.AddDocumentAsync(doc);
        await repo.AddChunksAsync(new[]
        {
            new RagChunk
            {
                DocumentId = doc.Id,
                Document = doc,
                ChunkIdentifier = "tuition_1",
                Section = "Học phí",
                Content = "Học phí chuyên ngành là 28,700,000 VNĐ mỗi kỳ.",
                Keywords = "học phí"
            }
        });

        var longGeminiReply = string.Join(" ", Enumerable.Repeat("Thông tin học phí kỳ chuyên ngành tại FPTU là 28,700,000 VNĐ mỗi kỳ áp dụng cho sinh viên chính quy.", 5));
        string? capturedPrompt = null;

        var handler = new StubHttpMessageHandler(reqBody =>
        {
            capturedPrompt = reqBody;
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                candidates = new[]
                {
                    new
                    {
                        content = new
                        {
                            parts = new[] { new { text = longGeminiReply } }
                        }
                    }
                }
            });
            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(payload, System.Text.Encoding.UTF8, "application/json")
            };
        });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "test-fake-key",
                ["Gemini:Model"] = "gemini-1.5-flash"
            })
            .Build();

        var searchService = new RagSearchService(repo, NullLogger<RagSearchService>.Instance);
        var chatService = new AiChatService(
            repo,
            searchService,
            new System.Net.Http.HttpClient(handler).AsFactory(),
            config,
            NullLogger<AiChatService>.Instance);

        var response = await chatService.AskAsync(new AiChatRequest
        {
            Question = "Học phí bao nhiêu?"
        }, Guid.NewGuid());

        Assert.NotNull(capturedPrompt);
        using var parsedReq = System.Text.Json.JsonDocument.Parse(capturedPrompt);
        var sentPromptText = parsedReq.RootElement
            .GetProperty("contents")[0]
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        Assert.NotNull(sentPromptText);
        Assert.Contains("255 KÝ TỰ", sentPromptText);
        Assert.Contains("DVSV", sentPromptText);
        Assert.Contains("daihoc.fpt.edu.vn", sentPromptText);
        Assert.InRange(response.Answer.Length, 1, AiChatService.MaxAnswerCharacters);
    }

    private sealed class StubHttpMessageHandler : System.Net.Http.HttpMessageHandler
    {
        private readonly Func<string?, System.Net.Http.HttpResponseMessage> _responder;
        public StubHttpMessageHandler(Func<string?, System.Net.Http.HttpResponseMessage> responder) => _responder = responder;

        protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
            return _responder(body);
        }
    }
}

internal static class HttpClientTestExtensions
{
    private class DummyFactory : System.Net.Http.IHttpClientFactory
    {
        private readonly System.Net.Http.HttpClient _client;
        public DummyFactory(System.Net.Http.HttpClient client) => _client = client;
        public System.Net.Http.HttpClient CreateClient(string name) => _client;
    }

    public static System.Net.Http.IHttpClientFactory AsFactory(this System.Net.Http.HttpClient client)
    {
        return new DummyFactory(client);
    }
}
