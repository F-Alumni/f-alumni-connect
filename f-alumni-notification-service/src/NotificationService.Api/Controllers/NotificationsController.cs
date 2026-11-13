using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Services.DTOs;
using NotificationService.Services.Interfaces;

namespace NotificationService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationAppService _notificationService;

    public NotificationsController(INotificationAppService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedNotificationsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] string? type = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetMyNotificationsAsync(
            userId, unreadOnly, type, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(new { unreadCount });
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var updated = await _notificationService.MarkAsReadAsync(id, userId, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("read-all")]
    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var updatedCount = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { updatedCount });
    }

    [HttpDelete("read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRead(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var deletedCount = await _notificationService.DeleteReadAsync(userId, cancellationToken);
        return Ok(new { deletedCount });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        await _notificationService.DeleteAsync(id, userId, cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        if (Guid.TryParse(sub, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User identity claim is missing or invalid.");
    }
}
