using Microsoft.AspNetCore.SignalR;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Api.Hubs;

public class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<ForumHub> _hubContext;

    public SignalRNotificationBroadcaster(IHubContext<ForumHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastNotificationAsync(Guid recipientId, NotificationResponse notification, int unreadCount, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"user_{recipientId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        await _hubContext.Clients.Group($"user_{recipientId}").SendAsync("ReceiveUnreadCount", unreadCount, cancellationToken);
    }

    public async Task BroadcastUnreadCountAsync(Guid recipientId, int unreadCount, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"user_{recipientId}").SendAsync("ReceiveUnreadCount", unreadCount, cancellationToken);
    }
}
