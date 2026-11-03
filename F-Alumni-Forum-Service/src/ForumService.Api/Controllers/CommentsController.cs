using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ForumService.Api.Extensions;
using ForumService.Api.Hubs;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Interfaces;

namespace ForumService.Api.Controllers;

[ApiController]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IHubContext<ForumHub> _hubContext;

    public CommentsController(ICommentService commentService, IHubContext<ForumHub> hubContext)
    {
        _commentService = commentService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// FO-06: Get all top-level comments and nested replies for a post.
    /// </summary>
    [HttpGet("api/v1/posts/{postId:guid}/comments")]
    [ProducesResponseType(typeof(IReadOnlyList<CommentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByPostId([FromRoute] Guid postId, CancellationToken cancellationToken = default)
    {
        var comments = await _commentService.GetCommentsByPostIdAsync(postId, cancellationToken);
        return Ok(comments);
    }

    /// <summary>
    /// FO-06, FO-07: Add a new comment or threaded reply under a post.
    /// </summary>
    [HttpPost("api/v1/posts/{postId:guid}/comments")]
    [Authorize]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddComment(
        [FromRoute] Guid postId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var fullName = User.GetFullName();
        var avatarUrl = User.GetAvatarUrl();

        var comment = await _commentService.AddCommentAsync(postId, request, userId, fullName, avatarUrl, cancellationToken);

        // Real-time broadcast to all viewers watching this post
        await _hubContext.Clients.Group($"post_{postId}").SendAsync("ReceiveComment", comment, cancellationToken);

        return Created($"api/v1/comments/{comment.Id}", comment);
    }

    /// <summary>
    /// FO-07: Edit an existing comment.
    /// </summary>
    [HttpPut("api/v1/comments/{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateComment(
        [FromRoute] Guid id,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        var updated = await _commentService.UpdateCommentAsync(id, request, userId, isAdmin, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// FO-07: Delete a comment.
    /// </summary>
    [HttpDelete("api/v1/comments/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteComment([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        await _commentService.DeleteCommentAsync(id, userId, isAdmin, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// FO-16: Post author accepts this comment as the official solution.
    /// </summary>
    [HttpPost("api/v1/comments/{id:guid}/accept")]
    [Authorize]
    [ProducesResponseType(typeof(CommentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptAnswer([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var comment = await _commentService.AcceptAnswerAsync(id, userId, cancellationToken);
        return Ok(comment);
    }
}
