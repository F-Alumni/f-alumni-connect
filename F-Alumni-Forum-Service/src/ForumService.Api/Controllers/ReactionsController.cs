using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ForumService.Api.Extensions;
using ForumService.Api.Hubs;
using ForumService.Services.Interfaces;

namespace ForumService.Api.Controllers;

[ApiController]
[Route("api/v1/posts/{postId:guid}/react")]
public class ReactionsController : ControllerBase
{
    private readonly IInteractionService _interactionService;
    private readonly IHubContext<ForumHub> _hubContext;

    public ReactionsController(IInteractionService interactionService, IHubContext<ForumHub> hubContext)
    {
        _interactionService = interactionService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// FO-08: Like or unlike a post (toggle).
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleLike([FromRoute] Guid postId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var fullName = User.GetFullName();
        var avatarUrl = User.GetAvatarUrl();
        var isLiked = await _interactionService.ToggleReactionAsync(postId, userId, fullName, avatarUrl, cancellationToken);

        // Real-time broadcast reaction event
        await _hubContext.Clients.Group($"post_{postId}").SendAsync("ReceiveReaction", new
        {
            PostId = postId,
            UserId = userId,
            IsLiked = isLiked
        }, cancellationToken);

        return Ok(new { PostId = postId, IsLiked = isLiked });
    }
}
