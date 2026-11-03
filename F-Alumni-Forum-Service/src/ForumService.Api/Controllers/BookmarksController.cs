using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForumService.Api.Extensions;
using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Api.Controllers;

[ApiController]
public class BookmarksController : ControllerBase
{
    private readonly IInteractionService _interactionService;

    public BookmarksController(IInteractionService interactionService)
    {
        _interactionService = interactionService;
    }

    /// <summary>
    /// FO-09: Bookmark or remove a bookmark on a post (toggle).
    /// </summary>
    [HttpPost("api/v1/posts/{postId:guid}/bookmark")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleBookmark([FromRoute] Guid postId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var isBookmarked = await _interactionService.ToggleBookmarkAsync(postId, userId, cancellationToken);
        return Ok(new { PostId = postId, IsBookmarked = isBookmarked });
    }

    /// <summary>
    /// FO-09: Get all saved/bookmarked posts for the current user.
    /// </summary>
    [HttpGet("api/v1/bookmarks/me")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<PostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookmarks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var result = await _interactionService.GetMyBookmarksAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
