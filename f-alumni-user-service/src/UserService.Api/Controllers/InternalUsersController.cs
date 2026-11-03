using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Services.DTOs.Requests;
using UserService.Services.DTOs.Responses;
using UserService.Services.Interfaces;

namespace UserService.Api.Controllers;

/// <summary>
/// Internal service-to-service endpoints (e.g. Admin Service -> User Service).
/// Matches sequence diagram in docs/02-architecture.md &amp; docs/03-service-boundaries.md.
/// </summary>
[ApiController]
[Route("api/v1/internal/users")]
[Produces("application/json")]
public class InternalUsersController : ControllerBase
{
    private readonly IUserProfileAppService _userProfileService;

    public InternalUsersController(IUserProfileAppService userProfileService)
    {
        _userProfileService = userProfileService;
    }

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
}
