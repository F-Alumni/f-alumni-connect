using Microsoft.AspNetCore.SignalR;
using ForumService.Api.Extensions;

namespace ForumService.Api.Hubs;

public class ForumHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId() ?? Guid.Empty;
        if (userId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public async Task JoinUserGroup(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var currentUserId = Context.User?.GetUserId() ?? Guid.Empty;
        if (currentUserId == Guid.Empty || !Guid.TryParse(userId, out var requestedId) || requestedId != currentUserId)
        {
            throw new HubException("Unauthorized to subscribe to another user's notification group.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{currentUserId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var currentUserId = Context.User?.GetUserId() ?? Guid.Empty;
        if (currentUserId != Guid.Empty && Guid.TryParse(userId, out var requestedId) && requestedId == currentUserId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{currentUserId}");
        }
    }

    public async Task JoinPostGroup(string postId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"post_{postId}");
    }

    public async Task LeavePostGroup(string postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"post_{postId}");
    }

    public async Task JoinEventsGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "events_channel");
    }

    public async Task LeaveEventsGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "events_channel");
    }
}
