using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Responses;

namespace ForumService.Services.Interfaces;

public interface INotificationService
{
    Task<PagedResponse<NotificationResponse>> GetMyNotificationsAsync(
        Guid recipientId,
        bool? unreadOnly = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<bool> MarkAsReadAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default);
    Task<int> MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid recipientId, CancellationToken cancellationToken = default);

    Task<NotificationResponse> CreateNotificationAsync(
        Guid recipientId,
        Guid senderId,
        string senderName,
        string senderAvatarUrl,
        string type,
        string title,
        string body,
        Guid? postId = null,
        Guid? commentId = null,
        Guid? eventId = null,
        string? linkUrl = null,
        CancellationToken cancellationToken = default);
}
