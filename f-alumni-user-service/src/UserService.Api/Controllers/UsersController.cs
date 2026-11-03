using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services.DTOs.Common;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;
using UserService.Services.Interfaces;

namespace UserService.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileAppService _userProfileService;

    public UsersController(IUserProfileAppService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    /// <summary>
    /// US-06 (Sheet) / US-09 (Guideline) &amp; AD-01/AD-02: Search and filter alumni directory by name, campus, major, graduationYear, company.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<UserProfileResponse>>> GetDirectory(
        [FromQuery] string? search = null,
        [FromQuery] string? campus = null,
        [FromQuery] int? graduationYear = null,
        [FromQuery] string? major = null,
        [FromQuery] string? company = null,
        [FromQuery] string? status = null,
        [FromQuery] string? role = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _userProfileService.GetDirectoryPagedAsync(
            search,
            campus,
            graduationYear,
            major,
            company,
            status,
            role,
            pageNumber,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// US-06 (Sheet) / US-09 (Guideline) Alias: Search alumni directory.
    /// </summary>
    [HttpGet("directory")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<UserProfileResponse>>> GetDirectoryAlias(
        [FromQuery] string? search = null,
        [FromQuery] string? campus = null,
        [FromQuery] int? graduationYear = null,
        [FromQuery] string? major = null,
        [FromQuery] string? company = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return GetDirectory(search, campus, graduationYear, major, company, "Active", null, pageNumber, pageSize, cancellationToken);
    }

    /// <summary>
    /// Get current authenticated user's full profile (extracted strictly from JWT claims).
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var profile = await _userProfileService.GetByIdAsync(userId, isOwnerOrAdmin: true, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// US-04 (Sheet) / US-06 (Guideline): Update current authenticated user's profile (name, avatar, campus, major, graduationYear, bio, calendar link).
    /// </summary>
    [HttpPut("me")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyProfile(
        [FromBody] UpdateUserProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdateProfileAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Update current authenticated user's avatar URL.
    /// </summary>
    [HttpPut("me/avatar")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyAvatar(
        [FromBody] UpdateAvatarRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdateAvatarAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// US-11 (Sheet): Add or update Google Calendar booking link on user profile.
    /// </summary>
    [HttpPut("me/calendar")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyCalendarLink(
        [FromBody] UpdateCalendarLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdateCalendarLinkAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// US-09 (Sheet): Customize which fields are publicly visible on user profile.
    /// </summary>
    [HttpPut("me/privacy")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileResponse>> UpdateMyPrivacy(
        [FromBody] UpdatePrivacySettingsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var updated = await _userProfileService.UpdatePrivacySettingsAsync(userId, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// US-10 (Sheet): Get list of users blocked by the current user.
    /// </summary>
    [HttpGet("me/blocks")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(IReadOnlyList<BlockedUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BlockedUserResponse>>> GetMyBlockedUsers(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var list = await _userProfileService.GetBlockedUsersAsync(userId, cancellationToken);
        return Ok(list);
    }

    /// <summary>
    /// US-10 (Sheet): Block another user account.
    /// </summary>
    [HttpPost("{id:guid}/block")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(typeof(BlockedUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BlockedUserResponse>> BlockUser(
        Guid id,
        [FromBody] BlockUserRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var blocked = await _userProfileService.BlockUserAsync(userId, id, request ?? new BlockUserRequest(), cancellationToken);
        return Ok(blocked);
    }

    /// <summary>
    /// US-10 (Sheet): Unblock a previously blocked user account.
    /// </summary>
    [HttpDelete("{id:guid}/block")]
    [Authorize(Policy = "RequireUserRole")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnblockUser(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        await _userProfileService.UnblockUserAsync(userId, id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// US-05 (Sheet) / US-08 (Guideline): Get public profile of a user by ID (respects US-09 privacy settings).
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var isOwnerOrAdmin = User.IsInRole("ADMIN") || (TryGetCurrentUserId(out var currentId) && currentId == id);
        var profile = await _userProfileService.GetByIdAsync(id, isOwnerOrAdmin, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// AD-03: Authorized endpoint for Admin / Admin Service to suspend a user account.
    /// </summary>
    [HttpPost("{id:guid}/suspend")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> SuspendUser(
        Guid id,
        [FromBody] SuspendUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userProfileService.SuspendUserAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// AD-03 / AD-04: Authorized endpoint for Admin / Admin Service to unsuspend a user account.
    /// </summary>
    [HttpPost("{id:guid}/unsuspend")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UnsuspendUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _userProfileService.UnsuspendUserAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update user role (USER / ADMIN) and account type (Student / Alumni / Admin).
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userProfileService.UpdateUserRoleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claimVal = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claimVal, out userId);
    }
}
