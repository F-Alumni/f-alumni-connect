using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Contracts.Events;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Repositories.Context;
using NotificationService.Repositories.Implementations;
using NotificationService.Services.DTOs;
using NotificationService.Services.Implementations;
using NotificationService.Services.Interfaces;

namespace NotificationService.Tests;

public class NotificationServiceTests
{
    private const string JwtKey = "falumni-production-shared-jwt-secret-key-2026-min-32-bytes";
    private const string JwtIssuer = "https://identity.falumni.local";
    private const string JwtAudience = "falumni-services";

    private class FakeRealtimePusher : INotificationRealtimePusher
    {
        public readonly List<(Guid RecipientId, NotificationResponse Notification)> Pushed = new();

        public Task PushToUserAsync(Guid recipientUserId, NotificationResponse notification, CancellationToken cancellationToken = default)
        {
            Pushed.Add((recipientUserId, notification));
            return Task.CompletedTask;
        }
    }

    private static NotificationServiceDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<NotificationServiceDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new NotificationServiceDbContext(options);
    }

    [Fact]
    public async Task Processor_Handles_Chat_Like_Comment_And_Deduplicates()
    {
        await using var db = CreateDbContext($"NotifProc_{Guid.NewGuid()}");
        var repo = new NotificationRepository(db);
        var pusher = new FakeRealtimePusher();
        var processor = new NotificationKafkaProcessor(repo, pusher, NullLogger<NotificationKafkaProcessor>.Instance);

        var authorId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var parentAuthorId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        // 1. Chat message event -> creates notification for recipient, ignores sender
        var chatJson = JsonSerializer.Serialize(new EventEnvelope<ChatMessageSentEventData>
        {
            EventId = Guid.NewGuid(),
            EventType = "chat.message_sent.v1",
            Producer = "f-alumni-chat-service",
            Data = new ChatMessageSentEventData
            {
                MessageId = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                SenderUserId = actorId,
                SenderDisplayName = "Nguyen Van B",
                ContentPreview = "Chào bạn, mình hỏi chút về đồ án!",
                RecipientUserIds = new List<Guid> { authorId, actorId }
            }
        });

        Assert.Equal(1, await processor.ProcessRawEventAsync("chat.message_sent.v1", chatJson));
        // Duplicate event ID is ignored idempotently
        Assert.Equal(0, await processor.ProcessRawEventAsync("chat.message_sent.v1", chatJson));

        // 2. Post liked by another user -> notifies post author
        var likeJson = JsonSerializer.Serialize(new EventEnvelope<PostReactedEventData>
        {
            EventId = Guid.NewGuid(),
            EventType = "forum.post_reacted.v1",
            Producer = "f-alumni-forum-service",
            Data = new PostReactedEventData
            {
                PostId = postId,
                PostTitle = "Chia sẻ kinh nghiệm phỏng vấn",
                PostAuthorId = authorId,
                ActorUserId = actorId,
                ActorName = "Nguyen Van B"
            }
        });
        Assert.Equal(1, await processor.ProcessRawEventAsync("forum.post_reacted.v1", likeJson));

        // Self-like -> 0 notifications
        var selfLikeJson = JsonSerializer.Serialize(new EventEnvelope<PostReactedEventData>
        {
            EventId = Guid.NewGuid(),
            EventType = "forum.post_reacted.v1",
            Producer = "f-alumni-forum-service",
            Data = new PostReactedEventData
            {
                PostId = postId,
                PostTitle = "Self post",
                PostAuthorId = authorId,
                ActorUserId = authorId,
                ActorName = "Author"
            }
        });
        Assert.Equal(0, await processor.ProcessRawEventAsync("forum.post_reacted.v1", selfLikeJson));

        // 3. Comment reply notifies both parent commenter & post author cleanly
        var commentJson = JsonSerializer.Serialize(new EventEnvelope<CommentCreatedEventData>
        {
            EventId = Guid.NewGuid(),
            EventType = "forum.comment_created.v1",
            Producer = "f-alumni-forum-service",
            Data = new CommentCreatedEventData
            {
                CommentId = Guid.NewGuid(),
                PostId = postId,
                PostTitle = "Chia sẻ kinh nghiệm phỏng vấn",
                PostAuthorId = authorId,
                ParentCommentId = Guid.NewGuid(),
                ParentCommentAuthorId = parentAuthorId,
                ActorUserId = actorId,
                ActorName = "Nguyen Van B",
                ContentPreview = "Cảm ơn anh rất nhiều ạ!"
            }
        });
        Assert.Equal(2, await processor.ProcessRawEventAsync("forum.comment_created.v1", commentJson));
        Assert.Equal(3, await repo.GetUnreadCountAsync(authorId));
        Assert.Equal(1, await repo.GetUnreadCountAsync(parentAuthorId));
        Assert.Equal(4, pusher.Pushed.Count);

        // Malformed JSON does not crash
        Assert.Equal(0, await processor.ProcessRawEventAsync("chat.message_sent.v1", "{bad-json"));
    }

    public class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = $"NotifApi_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DatabaseProvider"] = "InMemory",
                    ["InMemoryDatabaseName"] = _dbName,
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience,
                    ["Kafka:BootstrapServers"] = ""
                });
            });
        }
    }

    private static string CreateJwt(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("sub", userId.ToString()),
                new Claim(ClaimTypes.Role, "USER")
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Endpoints_RequireAuth_And_Support_Read_Lifecycle_With_Zero_5xx()
    {
        await using var factory = new ApiFactory();
        var client = factory.CreateClient();

        // Unauthenticated -> 401
        var unauthRes = await client.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthRes.StatusCode);

        var userId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var processor = scope.ServiceProvider.GetRequiredService<INotificationEventProcessor>();
            var json = JsonSerializer.Serialize(new EventEnvelope<PostReactedEventData>
            {
                EventId = Guid.NewGuid(),
                EventType = "forum.post_reacted.v1",
                Producer = "f-alumni-forum-service",
                Data = new PostReactedEventData
                {
                    PostId = Guid.NewGuid(),
                    PostTitle = "Bài viết test",
                    PostAuthorId = userId,
                    ActorUserId = otherId,
                    ActorName = "Tester B"
                }
            });
            await processor.ProcessRawEventAsync("forum.post_reacted.v1", json);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt(userId));

        var feedRes = await client.GetAsync("/api/v1/notifications?page=-1&pageSize=999");
        Assert.Equal(HttpStatusCode.OK, feedRes.StatusCode);
        var feed = await feedRes.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        Assert.NotNull(feed);
        Assert.Single(feed!.Items);
        Assert.Equal(1, feed.UnreadCount);

        var notifId = feed.Items[0].Id;

        var markRes = await client.PatchAsync($"/api/v1/notifications/{notifId}/read", null);
        Assert.Equal(HttpStatusCode.OK, markRes.StatusCode);

        var unreadRes = await client.GetAsync("/api/v1/notifications/unread-count");
        Assert.Equal(HttpStatusCode.OK, unreadRes.StatusCode);
        var unreadJson = await unreadRes.Content.ReadAsStringAsync();
        Assert.Contains("\"unreadCount\":0", unreadJson);

        // Non-existent ID -> 404 (not 5xx)
        var missingRes = await client.PatchAsync($"/api/v1/notifications/{Guid.NewGuid()}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, missingRes.StatusCode);

        // Cleanup read
        var cleanRes = await client.DeleteAsync("/api/v1/notifications/read");
        Assert.Equal(HttpStatusCode.OK, cleanRes.StatusCode);
    }
}
