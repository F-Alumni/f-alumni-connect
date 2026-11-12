using System.Security.Claims;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Api.Hubs;

/// <summary>
/// Real-time SignalR Hub for FPT Alumni Connect Chat Service (CH-02, CH-05, CH-06).
/// Endpoint: /hubs/chat
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly IChatAppService _chatAppService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatAppService chatAppService, ILogger<ChatHub> logger)
    {
        _chatAppService = chatAppService;
        _logger = logger;
    }

    public static string GetConversationGroupName(Guid conversationId) => $"conversation:{conversationId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("SignalR client connected: ConnectionId={ConnectionId}, UserId={UserId}",
            Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var currentUserId = GetCurrentUserId();
        await EnsureParticipantOrThrowHubExceptionAsync(conversationId, currentUserId);

        await Groups.AddToGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
        _logger.LogInformation("Connection {ConnectionId} (User {UserId}) joined conversation group {ConversationId}",
            Context.ConnectionId, currentUserId, conversationId);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetConversationGroupName(conversationId));
    }

    /// <summary>
    /// CH-02: Real-time message sending via WebSocket/SignalR.
    /// </summary>
    public async Task SendMessage(Guid conversationId, SendMessageRequest request)
    {
        if (request is null)
        {
            throw new HubException("Message payload cannot be null.");
        }

        try
        {
            var senderId = GetCurrentUserId();
            var message = await _chatAppService.SendMessageAsync(conversationId, senderId, request);
            await Clients.Group(GetConversationGroupName(conversationId))
                .SendAsync("ReceiveMessage", message);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException or UnauthorizedAccessException)
        {
            throw new HubException(ex.Message);
        }
    }

    /// <summary>
    /// CH-06: Broadcast typing indicator to other participants in the conversation.
    /// </summary>
    public async Task SendTypingIndicator(Guid conversationId, bool isTyping)
    {
        var senderId = GetCurrentUserId();
        await EnsureParticipantOrThrowHubExceptionAsync(conversationId, senderId);

        await Clients.OthersInGroup(GetConversationGroupName(conversationId))
            .SendAsync("UserTyping", new
            {
                ConversationId = conversationId,
                UserId = senderId,
                IsTyping = isTyping,
                TimestampUtc = DateTime.UtcNow
            });
    }

    /// <summary>
    /// CH-05: Mark conversation messages as read and notify participants in real time.
    /// </summary>
    public async Task MarkConversationRead(Guid conversationId)
    {
        try
        {
            var readerId = GetCurrentUserId();
            var result = await _chatAppService.MarkConversationReadAsync(conversationId, readerId);
            await Clients.Group(GetConversationGroupName(conversationId))
                .SendAsync("MessagesRead", result);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException or UnauthorizedAccessException)
        {
            throw new HubException(ex.Message);
        }
    }

    private async Task EnsureParticipantOrThrowHubExceptionAsync(Guid conversationId, Guid userId)
    {
        try
        {
            await _chatAppService.EnsureParticipantAsync(conversationId, userId);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException or UnauthorizedAccessException)
        {
            throw new HubException(ex.Message);
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? Context.User?.FindFirstValue("sub");

        if (Guid.TryParse(claim, out var userId) && userId != Guid.Empty)
        {
            return userId;
        }

        throw new HubException("Unauthorized: valid user identifier claim is required.");
    }
}
