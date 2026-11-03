using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Api.Extensions;
using AdminService.Services.DTOs.Announcements;
using AdminService.Services.DTOs.Common;
using AdminService.Services.Interfaces;

namespace AdminService.Api.Controllers;

/// <summary>
/// System Announcement Endpoints (AD-08).
/// Manages campus-wide notifications broadcasted to all students and alumni.
/// Requires ADMIN role.
/// </summary>
[ApiController]
[Route("api/v1/admin/announcements")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdminRole")]
public class AnnouncementsController : ControllerBase
{
    private readonly ISystemAnnouncementService _announcementService;
    private readonly ILogger<AnnouncementsController> _logger;

    public AnnouncementsController(
        ISystemAnnouncementService announcementService,
        ILogger<AnnouncementsController> logger)
    {
        _announcementService = announcementService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of announcements with status and type filters (AD-08).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SystemAnnouncementResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<SystemAnnouncementResponse>>> GetAnnouncements(
        [FromQuery] AnnouncementFilterQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _announcementService.GetAnnouncementsAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single announcement by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SystemAnnouncementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemAnnouncementResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var announcement = await _announcementService.GetAnnouncementByIdAsync(id, cancellationToken);
        if (announcement is null)
        {
            return NotFound(new { message = $"System announcement with ID '{id}' was not found." });
        }

        return Ok(announcement);
    }

    /// <summary>
    /// Publishes a new system-wide announcement to all members (AD-08).
    /// Automatically records an immutable audit log entry.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SystemAnnouncementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SystemAnnouncementResponse>> Create(
        [FromBody] CreateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        var adminEmail = User.GetUserEmail();
        var ipAddress = HttpContext.GetClientIpAddress();

        var created = await _announcementService.CreateAnnouncementAsync(
            adminId, adminEmail, request, ipAddress, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing system announcement (AD-08).
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SystemAnnouncementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SystemAnnouncementResponse>> Update(
        Guid id,
        [FromBody] UpdateAnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        var adminEmail = User.GetUserEmail();
        var ipAddress = HttpContext.GetClientIpAddress();

        var updated = await _announcementService.UpdateAnnouncementAsync(
            adminId, adminEmail, id, request, ipAddress, cancellationToken);

        return Ok(updated);
    }

    /// <summary>
    /// Deletes an existing system announcement (AD-08).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        var adminEmail = User.GetUserEmail();
        var ipAddress = HttpContext.GetClientIpAddress();

        await _announcementService.DeleteAnnouncementAsync(
            adminId, adminEmail, id, ipAddress, cancellationToken);

        return NoContent();
    }
}
