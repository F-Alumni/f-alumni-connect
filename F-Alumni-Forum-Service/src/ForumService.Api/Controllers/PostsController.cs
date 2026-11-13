using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ForumService.Api.Extensions;
using ForumService.Api.Hubs;
using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Api.Controllers;

[ApiController]
[Route("api/v1/posts")]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IHubContext<ForumHub> _hubContext;

    public PostsController(IPostService postService, IHubContext<ForumHub> hubContext)
    {
        _postService = postService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// FO-05, FO-12, FO-13, FO-14, FO-15, FO-16: Get community news feed with filtering, search, sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeed(
        [FromQuery] string? campus = null,
        [FromQuery] string? major = null,
        [FromQuery] string? postType = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isAnswered = null,
        [FromQuery] string sort = "latest",
        [FromQuery] int page = 1,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var resolvedPage = pageNumber ?? page;
        var canSeeHidden = includeHidden && User.IsAdmin();
        var result = await _postService.GetFeedAsync(campus, major, postType, search, isAnswered, sort, resolvedPage, pageSize, currentUserId, cancellationToken, canSeeHidden);
        return Ok(result);
    }

    /// <summary>
    /// FO-10: Get posts created by the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<PostResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPosts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var result = await _postService.GetMyPostsAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// FO-03, FO-04: Get detailed post by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var post = await _postService.GetPostByIdAsync(id, currentUserId, cancellationToken);
        return Ok(post);
    }

    /// <summary>
    /// FO-01, FO-02, FO-03: Create a new post (Question, Sharing, Recruitment) with attachments and tags.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var fullName = User.GetFullName();
        var avatarUrl = User.GetAvatarUrl();

        var created = await _postService.CreatePostAsync(request, userId, fullName, avatarUrl, cancellationToken);

        // Real-time broadcast to all feed listeners
        await _hubContext.Clients.All.SendAsync("ReceiveNewPost", created, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// FO-04: Edit an existing post.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdatePostRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        var updated = await _postService.UpdatePostAsync(id, request, userId, isAdmin, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// FO-04: Delete a post.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        await _postService.DeletePostAsync(id, userId, isAdmin, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// FO-16: Mark a question post as resolved / answered.
    /// </summary>
    [HttpPatch("{id:guid}/answered")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAnswered([FromRoute] Guid id, [FromQuery] bool isAnswered = true, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        await _postService.MarkAnsweredAsync(id, isAnswered, userId, cancellationToken);
        return NoContent();
    }

    public record SetPostVisibilityRequest(bool IsHidden);

    /// <summary>
    /// AD-06 / AD-07: Admin toggles visibility of a post (Hide / Restore).
    /// </summary>
    [HttpPatch("{id:guid}/visibility")]
    [Authorize(Policy = "RequireAdminRole")]
    [ProducesResponseType(typeof(PostResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetVisibility(
        [FromRoute] Guid id,
        [FromBody] SetPostVisibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _postService.SetVisibilityAsync(id, request.IsHidden, cancellationToken);
        return Ok(updated);
    }
}
