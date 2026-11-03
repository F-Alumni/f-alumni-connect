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
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IHubContext<ForumHub> _hubContext;

    public EventsController(IEventService eventService, IHubContext<ForumHub> hubContext)
    {
        _eventService = eventService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// List upcoming community events (similar to Facebook Events).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EventResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? campus = null,
        [FromQuery] bool? upcomingOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var result = await _eventService.GetEventsAsync(campus, upcomingOnly, page, pageSize, currentUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get event detail by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var currentUserId = User.Identity?.IsAuthenticated == true ? User.GetUserId() : (Guid?)null;
        var evt = await _eventService.GetEventByIdAsync(id, currentUserId, cancellationToken);
        return Ok(evt);
    }

    /// <summary>
    /// Create a new event (e.g. "Ăn tết 2027", Alumni Offline Meetup).
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var fullName = User.GetFullName();
        var avatarUrl = User.GetAvatarUrl();

        var created = await _eventService.CreateEventAsync(request, userId, fullName, avatarUrl, cancellationToken);

        // Real-time broadcast event to events channel
        await _hubContext.Clients.Group("events_channel").SendAsync("ReceiveNewEvent", created, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Join or update RSVP status for an event (Going, Interested, NotGoing).
    /// </summary>
    [HttpPost("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RsvpEvent(
        [FromRoute] Guid id,
        [FromBody] RsvpEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var fullName = User.GetFullName();
        var avatarUrl = User.GetAvatarUrl();

        await _eventService.RsvpEventAsync(id, request, userId, fullName, avatarUrl, cancellationToken);

        // Real-time broadcast attendee update
        await _hubContext.Clients.Group("events_channel").SendAsync("ReceiveEventRsvp", new
        {
            EventId = id,
            UserId = userId,
            UserName = fullName,
            Status = request.Status
        }, cancellationToken);

        return Ok(new { EventId = id, Status = request.Status, Message = $"Bạn đã cập nhật trạng thái: {request.Status}" });
    }

    /// <summary>
    /// List all attendees/participants for an event.
    /// </summary>
    [HttpGet("{id:guid}/attendees")]
    [ProducesResponseType(typeof(IReadOnlyList<EventAttendeeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendees([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var attendees = await _eventService.GetAttendeesAsync(id, cancellationToken);
        return Ok(attendees);
    }

    /// <summary>
    /// Delete an event.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEvent([FromRoute] Guid id, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        var isAdmin = User.IsAdmin();

        await _eventService.DeleteEventAsync(id, userId, isAdmin, cancellationToken);
        return NoContent();
    }
}
