using Microsoft.AspNetCore.SignalR;

namespace ForumService.Api.Hubs;

public class ForumHub : Hub
{
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
