using System.Security.Claims;
using ChatService.Api.Hubs;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;
using ChatService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatService.Tests.Hubs;

public class ChatHubAuthorizationTests
{
    private static readonly Guid UserA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid NonMember = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ConversationId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    [Fact]
    public void ChatHub_HasAuthorizeAttribute()
    {
        var authorizeAttr = typeof(ChatHub).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);
        Assert.NotEmpty(authorizeAttr);
    }

    [Fact]
    public async Task OnConnectedAsync_MissingOrInvalidUserClaim_ThrowsHubException()
    {
        var hubWithoutClaims = CreateHub(userIdClaim: null);
        await Assert.ThrowsAsync<HubException>(() => hubWithoutClaims.OnConnectedAsync());

        var hubWithInvalidClaim = CreateHub(userIdClaim: "not-a-guid");
        await Assert.ThrowsAsync<HubException>(() => hubWithInvalidClaim.OnConnectedAsync());
    }

    [Fact]
    public async Task JoinConversation_NonMember_ThrowsHubException_AndDoesNotAddToGroup()
    {
        var groups = new RecordingGroupManager();
        var hub = CreateHub(NonMember.ToString(), groups: groups);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinConversation(ConversationId));
        Assert.Empty(groups.AddedGroups);
    }

    [Fact]
    public async Task JoinConversation_ValidMember_AddsToConversationGroup()
    {
        var groups = new RecordingGroupManager();
        var hub = CreateHub(UserA.ToString(), groups: groups);

        await hub.JoinConversation(ConversationId);
        Assert.Single(groups.AddedGroups);
        Assert.Equal(ChatHub.GetConversationGroupName(ConversationId), groups.AddedGroups[0].GroupName);
    }

    [Fact]
    public async Task SendMessage_NonMember_ThrowsHubException()
    {
        var hub = CreateHub(NonMember.ToString());
        var request = new SendMessageRequest { SenderUserId = UserA, Content = "Spoofed" };

        await Assert.ThrowsAsync<HubException>(() => hub.SendMessage(ConversationId, request));
    }

    [Fact]
    public async Task SendMessage_Member_IgnoresSpoofedSenderUserId_UsesJwtClaim()
    {
        var appService = new StubChatAppService(new HashSet<Guid> { UserA, UserB });
        var clients = new RecordingHubCallerClients();
        var hub = CreateHub(UserA.ToString(), appService: appService, clients: clients);

        var request = new SendMessageRequest { SenderUserId = UserB, Content = "Hello from A" };
        await hub.SendMessage(ConversationId, request);

        Assert.Equal(UserA, appService.LastSenderUserId);
        Assert.Single(clients.GroupProxy.SentMessages);
        Assert.Equal("ReceiveMessage", clients.GroupProxy.SentMessages[0].Method);
    }

    [Fact]
    public async Task SendTypingIndicator_NonMember_ThrowsHubException()
    {
        var hub = CreateHub(NonMember.ToString());
        await Assert.ThrowsAsync<HubException>(() => hub.SendTypingIndicator(ConversationId, true));
    }

    [Fact]
    public async Task MarkConversationRead_NonMember_ThrowsHubException()
    {
        var hub = CreateHub(NonMember.ToString());
        await Assert.ThrowsAsync<HubException>(() => hub.MarkConversationRead(ConversationId));
    }

    private static ChatHub CreateHub(
        string? userIdClaim,
        StubChatAppService? appService = null,
        RecordingGroupManager? groups = null,
        RecordingHubCallerClients? clients = null)
    {
        appService ??= new StubChatAppService(new HashSet<Guid> { UserA, UserB });
        groups ??= new RecordingGroupManager();
        clients ??= new RecordingHubCallerClients();

        var claims = new List<Claim>();
        if (userIdClaim is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaim));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        var hub = new ChatHub(appService, NullLogger<ChatHub>.Instance)
        {
            Context = new StubHubCallerContext("conn-1", principal),
            Groups = groups,
            Clients = clients
        };

        return hub;
    }

    private sealed class StubChatAppService : IChatAppService
    {
        private readonly HashSet<Guid> _participants;
        public Guid? LastSenderUserId { get; private set; }

        public StubChatAppService(HashSet<Guid> participants)
        {
            _participants = participants;
        }

        public Task EnsureParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
        {
            if (conversationId != ConversationId)
            {
                throw new KeyNotFoundException($"Conversation '{conversationId}' was not found.");
            }

            if (!_participants.Contains(userId))
            {
                throw new UnauthorizedAccessException($"User '{userId}' is not a participant.");
            }

            return Task.CompletedTask;
        }

        public async Task<ChatMessageResponse> SendMessageAsync(
            Guid conversationId,
            Guid senderUserId,
            SendMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            await EnsureParticipantAsync(conversationId, senderUserId, cancellationToken);
            LastSenderUserId = senderUserId;
            return new ChatMessageResponse
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderUserId = senderUserId,
                Content = request.Content ?? string.Empty,
                Status = "Sent",
                SentAt = DateTime.UtcNow
            };
        }

        public async Task<MarkReadResponse> MarkConversationReadAsync(
            Guid conversationId,
            Guid readerUserId,
            CancellationToken cancellationToken = default)
        {
            await EnsureParticipantAsync(conversationId, readerUserId, cancellationToken);
            return new MarkReadResponse
            {
                ConversationId = conversationId,
                ReaderUserId = readerUserId,
                MarkedCount = 1,
                ReadAtUtc = DateTime.UtcNow
            };
        }

        public Task<ConversationResponse> CreateOrGetConversationAsync(Guid creatorUserId, CreateConversationRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ConversationResponse?> GetConversationByIdAsync(Guid conversationId, Guid currentUserId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PagedResponse<ConversationResponse>> GetUserConversationsAsync(Guid userId, bool includeHidden = false, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PagedResponse<ChatMessageResponse>> GetMessagesAsync(Guid conversationId, Guid currentUserId, string? search = null, int page = 1, int pageSize = 30, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<ParticipantDto> UpdateConversationSettingsAsync(Guid conversationId, Guid userId, UpdateConversationSettingsRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }

    private sealed class StubHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly ClaimsPrincipal? _user;
        private readonly IDictionary<object, object?> _items = new Dictionary<object, object?>();
        private readonly IFeatureCollection _features = new FeatureCollection();

        public StubHubCallerContext(string connectionId, ClaimsPrincipal? user)
        {
            _connectionId = connectionId;
            _user = user;
        }

        public override string ConnectionId => _connectionId;
        public override string? UserIdentifier => _user?.FindFirstValue(ClaimTypes.NameIdentifier);
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items => _items;
        public override IFeatureCollection Features => _features;
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> AddedGroups { get; } = new();

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            AddedGroups.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingClientProxy : IClientProxy
    {
        public List<(string Method, object?[] Args)> SentMessages { get; } = new();

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            SentMessages.Add((method, args));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHubCallerClients : IHubCallerClients
    {
        public RecordingClientProxy GroupProxy { get; } = new();
        private readonly RecordingClientProxy _defaultProxy = new();

        public IClientProxy All => _defaultProxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _defaultProxy;
        public IClientProxy Caller => _defaultProxy;
        public IClientProxy Client(string connectionId) => _defaultProxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _defaultProxy;
        public IClientProxy Group(string groupName) => GroupProxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => GroupProxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => GroupProxy;
        public IClientProxy Others => _defaultProxy;
        public IClientProxy OthersInGroup(string groupName) => GroupProxy;
        public IClientProxy User(string userId) => _defaultProxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => _defaultProxy;
    }
}
