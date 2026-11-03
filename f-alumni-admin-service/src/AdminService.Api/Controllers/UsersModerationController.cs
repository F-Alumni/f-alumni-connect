using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminService.Api.Extensions;
using AdminService.Services.DTOs.Common;
using AdminService.Services.DTOs.Users;
using AdminService.Services.Interfaces;

namespace AdminService.Api.Controllers;

/// <summary>
/// Administrative User Moderation Endpoints (AD-02, AD-03, AD-04).
/// Requires ADMIN role. Operates through inter-service calls preserving microservice isolation.
/// </summary>
[ApiController]
[Route("api/v1/admin/users")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdminRole")]
public class UsersModerationController : ControllerBase
{
    private readonly IUserModerationService _userModerationService;
    private readonly ILogger<UsersModerationController> _logger;

    public UsersModerationController(
        IUserModerationService userModerationService,
        ILogger<UsersModerationController> logger)
    {
        _userModerationService = userModerationService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated directory of users with filtering across campus, major, company, role, and status (AD-02).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AdminUserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AdminUserSummaryDto>>> GetUsers(
        [FromQuery] UserFilterQuery query,
        CancellationToken cancellationToken)
    {
        var token = Request.GetBearerToken();
        var result = await _userModerationService.GetUsersListAsync(query, token, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves user detail by ID for administrative review.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminUserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserSummaryDto>> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var token = Request.GetBearerToken();
        var user = await _userModerationService.GetUserDetailAsync(id, token, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = $"User with ID '{id}' was not found." });
        }

        return Ok(user);
    }

    /// <summary>
    /// Suspends a user account and writes an immutable administrative audit record (AD-03, AD-07).
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SuspendUser(
        Guid id,
        [FromBody] SuspendUserRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        var adminEmail = User.GetUserEmail();
        var ipAddress = HttpContext.GetClientIpAddress();
        var token = Request.GetBearerToken();

        var success = await _userModerationService.SuspendUserAsync(
            adminId, adminEmail, id, request, ipAddress, token, cancellationToken);

        if (!success)
        {
            return BadRequest(new { message = $"Failed to suspend user '{id}'. The User service was unreachable or rejected the request." });
        }

        return Ok(new
        {
            message = "User successfully suspended.",
            userId = id,
            reason = request.Reason,
            suspendedBy = adminEmail,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unsuspends a previously suspended user account and writes an immutable audit record (AD-04, AD-07).
    /// </summary>
    [HttpPost("{id:guid}/unsuspend")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnsuspendUser(
        Guid id,
        [FromBody] UnsuspendUserRequest? request,
        CancellationToken cancellationToken)
    {
        var adminId = User.GetUserId();
        var adminEmail = User.GetUserEmail();
        var ipAddress = HttpContext.GetClientIpAddress();
        var token = Request.GetBearerToken();

        var success = await _userModerationService.UnsuspendUserAsync(
            adminId, adminEmail, id, request, ipAddress, token, cancellationToken);

        if (!success)
        {
            return BadRequest(new { message = $"Failed to unsuspend user '{id}'. The User service was unreachable or rejected the request." });
        }

        return Ok(new
        {
            message = "User successfully unsuspended.",
            userId = id,
            unsuspendedBy = adminEmail,
            timestamp = DateTime.UtcNow
        });
    }
}
