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
        
        // Test query about tuition
        var tuitionResults = await searchService.SearchRelevantChunksAsync("Học phí kỳ chuyên ngành tại Hola bao nhiêu?");
        Assert.NotEmpty(tuitionResults);

        // Test query about BIT_SE curriculum
        var seResults = await searchService.SearchRelevantChunksAsync("Kỹ thuật phần mềm BIT_SE đào tạo những gì?");
        Assert.NotEmpty(seResults);

        // Test query about first year subjects Vovinam
        var vovinamResults = await searchService.SearchRelevantChunksAsync("Môn Vovinam có bắt buộc không?");
        Assert.NotEmpty(vovinamResults);
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
