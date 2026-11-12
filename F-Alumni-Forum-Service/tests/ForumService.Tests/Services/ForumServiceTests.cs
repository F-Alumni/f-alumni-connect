using Microsoft.Extensions.Logging.Abstractions;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.Exceptions;
using ForumService.Services.Implementations;
using ForumService.Services.Interfaces;

namespace ForumService.Tests.Services;

public class ForumServiceTests
{
    private class FakePostRepository : IPostRepository
    {
        public readonly List<Post> Posts = new();

        public Task<Post> AddAsync(Post post, CancellationToken cancellationToken = default)
        {
            Posts.Add(post);
            return Task.FromResult(post);
        }

        public Task DeleteAsync(Post post, CancellationToken cancellationToken = default)
        {
            Posts.Remove(post);
            return Task.CompletedTask;
        }

        public Task<Post?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));
        }

        public Task<(IReadOnlyList<Post> Items, int TotalCount)> GetByAuthorAsync(Guid authorId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var items = Posts.Where(p => p.AuthorId == authorId).ToList();
            return Task.FromResult< (IReadOnlyList<Post>, int) >((items, items.Count));
        }

        public Task<(IReadOnlyList<Post> Items, int TotalCount)> GetFeedAsync(string? campus = null, string? major = null, string? postType = null, string? search = null, bool? isAnswered = null, string sort = "latest", int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var q = Posts.AsEnumerable();
            if (!string.IsNullOrEmpty(campus) && campus != "All") q = q.Where(p => p.Campus == campus || p.Campus == "All");
            if (!string.IsNullOrEmpty(major) && major != "All") q = q.Where(p => p.Major == major || p.Major == "All");
            if (!string.IsNullOrEmpty(postType)) q = q.Where(p => p.PostType == postType);
            if (isAnswered.HasValue) q = q.Where(p => p.IsAnswered == isAnswered.Value);
            if (!string.IsNullOrEmpty(search)) q = q.Where(p => p.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

            var list = q.ToList();
            return Task.FromResult< (IReadOnlyList<Post>, int) >((list, list.Count));
        }

        public Task IncrementViewCountAsync(Guid postId, CancellationToken cancellationToken = default)
        {
            var p = Posts.FirstOrDefault(x => x.Id == postId);
            if (p != null) p.ViewCount++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Post post, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private class FakeInteractionRepository : IInteractionRepository
    {
        public readonly HashSet<(Guid PostId, Guid UserId)> Likes = new();
        public readonly HashSet<(Guid PostId, Guid UserId)> Bookmarks = new();
        public readonly List<Report> Reports = new();
        public readonly List<Category> Categories = new();

        public Task<Category> AddCategoryAsync(Category category, CancellationToken cancellationToken = default)
        {
            Categories.Add(category);
            return Task.FromResult(category);
        }

        public Task<Report> AddReportAsync(Report report, CancellationToken cancellationToken = default)
        {
            Reports.Add(report);
            return Task.FromResult(report);
        }

        public Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Category>>(Categories);
        }

        public Task<(IReadOnlyList<Post> Items, int TotalCount)> GetBookmarkedPostsAsync(Guid userId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            return Task.FromResult< (IReadOnlyList<Post>, int) >((new List<Post>(), 0));
        }

        public Task<bool> HasUserReactedAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Likes.Contains((postId, userId)));
        }

        public Task<bool> ToggleBookmarkAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
        {
            if (Bookmarks.Contains((postId, userId)))
            {
                Bookmarks.Remove((postId, userId));
                return Task.FromResult(false);
            }
            Bookmarks.Add((postId, userId));
            return Task.FromResult(true);
        }

        public Task<bool> ToggleReactionAsync(Guid postId, Guid userId, string reactionType = "Like", CancellationToken cancellationToken = default)
        {
            if (Likes.Contains((postId, userId)))
            {
                Likes.Remove((postId, userId));
                return Task.FromResult(false);
            }
            Likes.Add((postId, userId));
            return Task.FromResult(true);
        }
    }

    private class FakeKafkaProducer : IKafkaProducerService
    {
        public readonly List<object> Events = new();
        public Task PublishPostCreatedAsync(Contracts.Events.PostCreatedEventV1 eventData, CancellationToken cancellationToken = default)
        {
            Events.Add(eventData);
            return Task.CompletedTask;
        }

        public Task PublishPostReactedAsync(Contracts.Events.PostReactedEventV1 eventData, CancellationToken cancellationToken = default)
        {
            Events.Add(eventData);
            return Task.CompletedTask;
        }

        public Task PublishCommentCreatedAsync(Contracts.Events.CommentCreatedEventV1 eventData, CancellationToken cancellationToken = default)
        {
            Events.Add(eventData);
            return Task.CompletedTask;
        }

        public Task PublishReportCreatedAsync(Contracts.Events.ReportCreatedEventV1 eventData, CancellationToken cancellationToken = default)
        {
            Events.Add(eventData);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PostService_CreatePost_Success_And_EmitsKafkaEvent()
    {
        var postRepo = new FakePostRepository();
        var interactionRepo = new FakeInteractionRepository();
        var kafkaProducer = new FakeKafkaProducer();
        var service = new PostService(postRepo, interactionRepo, kafkaProducer, NullLogger<PostService>.Instance);

        var authorId = Guid.NewGuid();
        var req = new CreatePostRequest
        {
            Title = "Hỏi về kinh nghiệm thi qua môn PRN232",
            Content = "Mọi người cho mình xin kinh nghiệm làm assignment môn PRN232 với ạ!",
            PostType = "Question",
            Major = "SE",
            Campus = "FU-HCM",
            ImageUrls = new List<string> { "https://example.com/slide1.jpg" },
            Tags = new List<string> { "prn232", "experience" }
        };

        var result = await service.CreatePostAsync(req, authorId, "Vinh MT", "https://avatar.url");

        Assert.NotNull(result);
        Assert.Equal("Hỏi về kinh nghiệm thi qua môn PRN232", result.Title);
        Assert.Equal("Question", result.PostType);
        Assert.Equal("SE", result.Major);
        Assert.Equal("FU-HCM", result.Campus);
        Assert.Single(result.Images);
        Assert.Equal(2, result.Tags.Count);
        Assert.Single(kafkaProducer.Events);
    }

    [Fact]
    public async Task PostService_UpdatePost_OtherUser_ThrowsForbiddenException()
    {
        var postRepo = new FakePostRepository();
        var interactionRepo = new FakeInteractionRepository();
        var kafkaProducer = new FakeKafkaProducer();
        var service = new PostService(postRepo, interactionRepo, kafkaProducer, NullLogger<PostService>.Instance);

        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var post = new Post
        {
            AuthorId = authorId,
            Title = "Original Title",
            Content = "Original Content with 10 chars",
            PostType = "Sharing",
            Major = "SE",
            Campus = "FU-HCM"
        };
        await postRepo.AddAsync(post);

        var updateReq = new UpdatePostRequest
        {
            Title = "Hacked Title",
            Content = "Hacked Content updated"
        };

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdatePostAsync(post.Id, updateReq, otherUserId, isAdmin: false));
    }

    [Fact]
    public async Task EventService_CreateEvent_RSVP_Success()
    {
        var eventRepo = new FakeEventRepository();
        var service = new EventService(eventRepo, NullLogger<EventService>.Instance);

        var creatorId = Guid.NewGuid();
        var req = new CreateEventRequest
        {
            Title = "Ăn tết 2027 FPT Alumni",
            Description = "Hội ngộ anh em cựu sinh viên ăn tất niên đón tết 2027!",
            Location = "Hola Campus & Online",
            Campus = "FU-HL",
            StartTime = DateTime.UtcNow.AddDays(30),
            EndTime = DateTime.UtcNow.AddDays(30).AddHours(5)
        };

        var created = await service.CreateEventAsync(req, creatorId, "Trần Văn B", "https://avatar.url");

        Assert.NotNull(created);
        Assert.Equal("Ăn tết 2027 FPT Alumni", created.Title);
        Assert.Equal("Going", created.MyStatus);

        // Another user RSVPs Interested
        var guestId = Guid.NewGuid();
        await service.RsvpEventAsync(created.Id, new RsvpEventRequest { Status = "Interested" }, guestId, "Lê Thị C", "https://avatar.url/c");

        var attendees = await service.GetAttendeesAsync(created.Id);
        Assert.Equal(2, attendees.Count);
        Assert.Contains(attendees, a => a.UserId == guestId && a.Status == "Interested");
    }

    [Fact]
    public async Task EventService_InvalidDateAndRsvpStatus_ThrowArgumentException()
    {
        var eventRepo = new FakeEventRepository();
        var service = new EventService(eventRepo, NullLogger<EventService>.Instance);
        var creatorId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateEventAsync(
            new CreateEventRequest
            {
                Title = "Invalid Time Event",
                Description = "End before start",
                Location = "FPT",
                StartTime = DateTime.UtcNow.AddDays(2),
                EndTime = DateTime.UtcNow.AddDays(1)
            },
            creatorId,
            "Creator",
            "https://avatar.url/a"));

        var validEvent = await service.CreateEventAsync(
            new CreateEventRequest
            {
                Title = "Valid Event",
                Description = "OK",
                Location = "FPT",
                StartTime = DateTime.UtcNow.AddDays(1),
                EndTime = DateTime.UtcNow.AddDays(2),
                MaxAttendees = 1
            },
            creatorId,
            "Creator",
            "https://avatar.url/a");

        await Assert.ThrowsAsync<ArgumentException>(() => service.RsvpEventAsync(
            validEvent.Id,
            new RsvpEventRequest { Status = "InvalidStatus" },
            Guid.NewGuid(),
            "Guest",
            "https://avatar.url/b"));
    }

    [Fact]
    public async Task InteractionAndCommentServices_EmitKafkaEvents_OnlyForOtherUsers()
    {
        var postRepo = new FakePostRepository();
        var interactionRepo = new FakeInteractionRepository();
        var kafkaProducer = new FakeKafkaProducer();
        var authorId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var post = await postRepo.AddAsync(new Post
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            AuthorName = "Author A",
            Title = "Test Post",
            Content = "Content"
        });

        var interactionService = new InteractionService(
            interactionRepo,
            postRepo,
            NullLogger<InteractionService>.Instance,
            kafkaProducer);

        // Self-like does not publish
        Assert.True(await interactionService.ToggleReactionAsync(post.Id, authorId, "Author A"));
        Assert.Empty(kafkaProducer.Events);

        // Like by another user publishes PostReactedEventV1
        Assert.True(await interactionService.ToggleReactionAsync(post.Id, actorId, "Actor B"));
        Assert.Single(kafkaProducer.Events);
        var reacted = Assert.IsType<Contracts.Events.PostReactedEventV1>(kafkaProducer.Events[0]);
        Assert.Equal(authorId, reacted.PostAuthorId);
        Assert.Equal(actorId, reacted.ActorUserId);

        // Unlike does not publish
        Assert.False(await interactionService.ToggleReactionAsync(post.Id, actorId, "Actor B"));
        Assert.Single(kafkaProducer.Events);
    }

    private class ThrowingKafkaProducer : IKafkaProducerService
    {
        public Task PublishPostCreatedAsync(Contracts.Events.PostCreatedEventV1 eventData, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Kafka unavailable");

        public Task PublishPostReactedAsync(Contracts.Events.PostReactedEventV1 eventData, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Kafka unavailable");

        public Task PublishCommentCreatedAsync(Contracts.Events.CommentCreatedEventV1 eventData, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Kafka unavailable");

        public Task PublishReportCreatedAsync(Contracts.Events.ReportCreatedEventV1 eventData, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Kafka unavailable");
    }

    [Fact]
    public async Task InteractionService_CreateReport_PersistsAndEmitsKafkaEvent()
    {
        var postRepo = new FakePostRepository();
        var interactionRepo = new FakeInteractionRepository();
        var kafkaProducer = new FakeKafkaProducer();
        var service = new InteractionService(
            interactionRepo,
            postRepo,
            NullLogger<InteractionService>.Instance,
            kafkaProducer);

        var targetId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var request = new CreateReportRequest
        {
            TargetType = "Post",
            TargetId = targetId,
            Reason = "  Spam  ",
            Details = "  Repeated promotional links  "
        };

        await service.CreateReportAsync(request, reporterId, "Nguyen Van A");

        Assert.Single(interactionRepo.Reports);
        var savedReport = interactionRepo.Reports[0];
        Assert.Equal("Post", savedReport.TargetType);
        Assert.Equal(targetId, savedReport.TargetId);
        Assert.Equal(reporterId, savedReport.ReporterId);
        Assert.Equal("Nguyen Van A", savedReport.ReporterName);
        Assert.Equal("Spam", savedReport.Reason);
        Assert.Equal("Repeated promotional links", savedReport.Details);
        Assert.Equal("Pending", savedReport.Status);

        Assert.Single(kafkaProducer.Events);
        var emitted = Assert.IsType<Contracts.Events.ReportCreatedEventV1>(kafkaProducer.Events[0]);
        Assert.Equal(savedReport.Id, emitted.ReportId);
        Assert.Equal("Post", emitted.TargetType);
        Assert.Equal(targetId, emitted.TargetId);
        Assert.Equal(reporterId, emitted.ReporterId);
        Assert.Equal("Nguyen Van A", emitted.ReporterName);
        Assert.Equal("Spam", emitted.Reason);
        Assert.Equal("Repeated promotional links", emitted.Details);
        Assert.Equal("Pending", emitted.Status);
    }

    [Fact]
    public async Task InteractionService_CreateReport_WhenKafkaThrows_StillPersistsGracefully()
    {
        var postRepo = new FakePostRepository();
        var interactionRepo = new FakeInteractionRepository();
        var kafkaProducer = new ThrowingKafkaProducer();
        var service = new InteractionService(
            interactionRepo,
            postRepo,
            NullLogger<InteractionService>.Instance,
            kafkaProducer);

        var targetId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();

        await service.CreateReportAsync(new CreateReportRequest
        {
            TargetType = "Comment",
            TargetId = targetId,
            Reason = "Harassment",
            Details = "Abusive comment"
        }, reporterId, "Tran Thi B");

        Assert.Single(interactionRepo.Reports);
        Assert.Equal("Comment", interactionRepo.Reports[0].TargetType);
        Assert.Equal(targetId, interactionRepo.Reports[0].TargetId);
    }

    private class FakeEventRepository : IEventRepository
    {
        public readonly List<Event> Events = new();
        public readonly List<EventAttendee> Attendees = new();

        public Task<Event> AddAsync(Event evt, CancellationToken cancellationToken = default)
        {
            Events.Add(evt);
            return Task.FromResult(evt);
        }

        public Task AddOrUpdateAttendeeAsync(EventAttendee attendee, CancellationToken cancellationToken = default)
        {
            Attendees.RemoveAll(a => a.EventId == attendee.EventId && a.UserId == attendee.UserId);
            Attendees.Add(attendee);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Event evt, CancellationToken cancellationToken = default)
        {
            Events.Remove(evt);
            return Task.CompletedTask;
        }

        public Task<EventAttendee?> GetAttendeeAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Attendees.FirstOrDefault(a => a.EventId == eventId && a.UserId == userId));
        }

        public Task<IReadOnlyList<EventAttendee>> GetAttendeesByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<EventAttendee>>(Attendees.Where(a => a.EventId == eventId).ToList());
        }

        public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Events.FirstOrDefault(e => e.Id == id));
        }

        public Task<(IReadOnlyList<Event> Items, int TotalCount)> GetEventsAsync(string? campus = null, bool? upcomingOnly = true, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
        {
            return Task.FromResult< (IReadOnlyList<Event>, int) >((Events, Events.Count));
        }

        public Task UpdateAsync(Event evt, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
