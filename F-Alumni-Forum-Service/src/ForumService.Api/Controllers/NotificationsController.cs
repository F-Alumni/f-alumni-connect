using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForumService.Api.Extensions;
using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get paged notifications for the current authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var result = await _notificationService.GetMyNotificationsAsync(userId, unreadOnly, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get unread notification count for the notification bell badge.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(new { UnreadCount = count });
    }

    /// <summary>
    /// Mark a specific notification as read.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var success = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
        if (!success)
        {
            return NotFound(new { Message = "Thông báo không tồn tại hoặc không thuộc về người dùng này." });
        }
        return Ok(new { Message = "Đã đánh dấu thông báo là đã đọc." });
    }

    /// <summary>
    /// Mark all unread notifications of the current user as read.
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var count = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { UpdatedCount = count, Message = $"Đã đánh dấu {count} thông báo là đã đọc." });
    }

    /// <summary>
    /// Delete a notification.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var success = await _notificationService.DeleteAsync(id, userId, cancellationToken);
        if (!success)
        {
            return NotFound(new { Message = "Thông báo không tồn tại hoặc không thuộc về người dùng này." });
        }
        return NoContent();
    }
}
