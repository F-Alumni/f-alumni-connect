using System.Text.Json;
using Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using AiService.Repositories.Entities;
using AiService.Repositories.Interfaces;
using AiService.Services.Implementations;
using Xunit;

namespace AiService.Tests.Services;

public class KafkaEventConsumerTests
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
            return Task.FromResult<IReadOnlyList<RagChunk>>(Chunks);
        }

        public Task<int> GetChunkCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Chunks.Count);
        public Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(Documents.Count);
        public Task<RagDocument?> GetDocumentByPathAsync(string sourcePath, CancellationToken cancellationToken = default) => Task.FromResult(Documents.FirstOrDefault(d => d.SourcePath == sourcePath));
        public Task<AiConversation?> GetConversationAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(Conversations.FirstOrDefault(c => c.Id == conversationId && c.UserId == userId));
        public Task<IReadOnlyList<AiConversation>> GetConversationsByUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AiConversation>>(Conversations.Where(c => c.UserId == userId).ToList());
        public Task<IReadOnlyList<MentorProfile>> GetMentorsAsync(string? major = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MentorProfile>>(Mentors);
    }

    [Fact]
    public void EventEnvelope_SerializesAndDeserializes_MatchingPostCreatedEventV1Contract()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var eventData = new PostCreatedEventV1(
            postId: postId,
            authorId: authorId,
            authorName: "Nguyen Van A",
            title: "Microservices with .NET 10 & Kafka",
            content: "Sharing hands-on experience building distributed systems...",
            postType: "Sharing",
            major: "SE",
            campus: "FU-HCM"
        );

        var envelope = new EventEnvelope<PostCreatedEventV1>(
            eventType: PostCreatedEventV1.EventTypeName,
            version: PostCreatedEventV1.CurrentVersion,
            producer: "f-alumni-forum-service",
            data: eventData
        );

        // Act
        var json = JsonSerializer.Serialize(envelope);
        var deserialized = JsonSerializer.Deserialize<EventEnvelope<PostCreatedEventV1>>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(PostCreatedEventV1.EventTypeName, deserialized.EventType);
        Assert.Equal(postId, deserialized.Data.PostId);
        Assert.Equal("Nguyen Van A", deserialized.Data.AuthorName);
        Assert.Equal("SE", deserialized.Data.Major);
        Assert.Equal("FU-HCM", deserialized.Data.Campus);
    }

    [Fact]
    public async Task IngestForumPostAsync_IngestsPostAsRagDocumentAndChunk()
    {
        // Arrange
        var repo = new FakeRagRepository();
        var ingestionService = new RagIngestionService(repo, NullLogger<RagIngestionService>.Instance);
        var postId = Guid.NewGuid();

        // Act
        await ingestionService.IngestForumPostAsync(
            postId,
            "Học bổng AI FPT Software 2026",
            "Nội dung chi tiết về chương trình học bổng AI...",
            "Tran Thi B",
            "AI",
            "FU-HL"
        );

        // Assert
        Assert.Single(repo.Documents);
        var doc = repo.Documents.First();
        Assert.Equal($"forum/post/{postId}", doc.SourcePath);
        Assert.Equal("ForumPost", doc.DocumentType);
        Assert.Equal("AI", doc.ProgramCode);

        Assert.Single(repo.Chunks);
        var chunk = repo.Chunks.First();
        Assert.Equal($"post_{postId}", chunk.ChunkIdentifier);
        Assert.Contains("Tran Thi B", chunk.Content);
        Assert.Contains("Học bổng AI", chunk.Keywords);
    }
}
