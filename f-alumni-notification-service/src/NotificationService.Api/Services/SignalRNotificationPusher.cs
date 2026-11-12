using Microsoft.AspNetCore.SignalR;
using NotificationService.Api.Hubs;
using NotificationService.Services.DTOs;
using NotificationService.Services.Interfaces;

namespace NotificationService.Api.Services;

public class SignalRNotificationPusher : INotificationRealtimePusher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushToUserAsync(Guid recipientUserId, NotificationResponse notification, CancellationToken cancellationToken = default)
    {
        if (recipientUserId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return _hubContext.Clients
            .Group(NotificationHub.GetUserGroupName(recipientUserId))
            .SendAsync("ReceiveNotification", notification, cancellationToken);
    }
}
