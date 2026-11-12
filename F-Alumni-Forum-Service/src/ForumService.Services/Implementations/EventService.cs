using Microsoft.Extensions.Logging;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;
using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;
using ForumService.Services.Exceptions;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Implementations;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly INotificationService? _notificationService;
    private readonly ILogger<EventService> _logger;

    public EventService(
        IEventRepository eventRepository,
        ILogger<EventService> logger,
        INotificationService? notificationService = null)
    {
        _eventRepository = eventRepository;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid creatorId, string creatorName, string creatorAvatarUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Tiêu đề sự kiện không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new ArgumentException("Mô tả sự kiện không được để trống.");
        }
        if (string.IsNullOrWhiteSpace(request.Location))
        {
            throw new ArgumentException("Địa điểm tổ chức không được để trống.");
        }
        if (request.EndTime <= request.StartTime)
        {
            throw new ArgumentException("Thời gian kết thúc sự kiện phải sau thời gian bắt đầu.");
        }

        var evt = new Event
        {
            CreatorId = creatorId,
            CreatorName = creatorName,
            CreatorAvatarUrl = creatorAvatarUrl,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            BannerUrl = request.BannerUrl ?? string.Empty,
            Location = request.Location.Trim(),
            Campus = request.Campus,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            MaxAttendees = request.MaxAttendees,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _eventRepository.AddAsync(evt, cancellationToken);
        _logger.LogInformation("Created event {EventId} '{Title}' by {CreatorId}", created.Id, created.Title, creatorId);

        // Auto-join creator as Going
        await _eventRepository.AddOrUpdateAttendeeAsync(new EventAttendee
        {
            EventId = created.Id,
            UserId = creatorId,
            UserName = creatorName,
            UserAvatarUrl = creatorAvatarUrl,
            Status = "Going"
        }, cancellationToken);

        return MapToResponse(created, "Going");
    }

    public async Task<EventResponse> GetEventByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken cancellationToken = default)
    {
        var evt = await _eventRepository.GetByIdAsync(id, cancellationToken);
        if (evt == null)
        {
            throw new NotFoundException("Event", id);
        }

        string? myStatus = null;
        if (currentUserId.HasValue)
        {
            var attendee = await _eventRepository.GetAttendeeAsync(id, currentUserId.Value, cancellationToken);
            myStatus = attendee?.Status;
        }

        return MapToResponse(evt, myStatus);
    }

    public async Task<PagedResponse<EventResponse>> GetEventsAsync(
        string? campus = null,
        bool? upcomingOnly = true,
        int page = 1,
        int pageSize = 10,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _eventRepository.GetEventsAsync(campus, upcomingOnly, page, pageSize, cancellationToken);

        var responses = new List<EventResponse>();
        foreach (var evt in items)
        {
            string? myStatus = null;
            if (currentUserId.HasValue)
            {
                var attendee = await _eventRepository.GetAttendeeAsync(evt.Id, currentUserId.Value, cancellationToken);
                myStatus = attendee?.Status;
            }
            responses.Add(MapToResponse(evt, myStatus));
        }

        return new PagedResponse<EventResponse>(responses, totalCount, page, pageSize);
    }

    public async Task RsvpEventAsync(Guid eventId, RsvpEventRequest request, Guid userId, string userName, string userAvatarUrl, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var normalizedStatus = request.Status?.Trim() switch
        {
            var s when string.Equals(s, "Going", StringComparison.OrdinalIgnoreCase) => "Going",
            var s when string.Equals(s, "Interested", StringComparison.OrdinalIgnoreCase) => "Interested",
            var s when string.Equals(s, "NotGoing", StringComparison.OrdinalIgnoreCase) => "NotGoing",
            _ => throw new ArgumentException("Trạng thái RSVP phải là Going, Interested hoặc NotGoing.")
        };

        var evt = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (evt == null)
        {
            throw new NotFoundException("Event", eventId);
        }

        if (normalizedStatus == "Going" && evt.MaxAttendees.HasValue)
        {
            var existing = await _eventRepository.GetAttendeeAsync(eventId, userId, cancellationToken);
            var alreadyGoing = existing != null && string.Equals(existing.Status, "Going", StringComparison.OrdinalIgnoreCase);
            if (!alreadyGoing && evt.GoingCount >= evt.MaxAttendees.Value)
            {
                throw new InvalidOperationException("Sự kiện đã đạt số lượng người tham gia tối đa.");
            }
        }

        var attendee = new EventAttendee
        {
            EventId = eventId,
            UserId = userId,
            UserName = userName,
            UserAvatarUrl = userAvatarUrl,
            Status = normalizedStatus
        };

        await _eventRepository.AddOrUpdateAttendeeAsync(attendee, cancellationToken);
        _logger.LogInformation("User {UserId} RSVP {Status} for event {EventId}", userId, request.Status, eventId);

        // Auto-trigger notification for event creator if another user RSVPs
        if (_notificationService != null && userId != evt.CreatorId && normalizedStatus != "NotGoing")
        {
            var shortTitle = evt.Title.Length > 45 ? evt.Title[..45] + "..." : evt.Title;
            var statusText = normalizedStatus == "Going" ? "sẽ tham gia" : "quan tâm đến";
            try
            {
                await _notificationService.CreateNotificationAsync(
                    recipientId: evt.CreatorId,
                    senderId: userId,
                    senderName: userName,
                    senderAvatarUrl: userAvatarUrl,
                    type: "EVENT_RSVP",
                    title: "Phản hồi sự kiện mới",
                    body: $"{userName} {statusText} sự kiện \"{shortTitle}\"",
                    eventId: eventId,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create EVENT_RSVP notification for event {EventId}", eventId);
            }
        }
    }

    public async Task<IReadOnlyList<EventAttendeeResponse>> GetAttendeesAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var attendees = await _eventRepository.GetAttendeesByEventIdAsync(eventId, cancellationToken);
        return attendees.Select(a => new EventAttendeeResponse
        {
            Id = a.Id,
            EventId = a.EventId,
            UserId = a.UserId,
            UserName = a.UserName,
            UserAvatarUrl = a.UserAvatarUrl,
            Status = a.Status,
            RegisteredAt = a.RegisteredAt
        }).ToList();
    }

    public async Task DeleteEventAsync(Guid eventId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
        if (evt == null)
        {
            throw new NotFoundException("Event", eventId);
        }

        if (evt.CreatorId != currentUserId && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền xóa sự kiện này.");
        }

        await _eventRepository.DeleteAsync(evt, cancellationToken);
        _logger.LogInformation("Deleted event {EventId} by user {UserId}", eventId, currentUserId);
    }

    private static EventResponse MapToResponse(Event evt, string? myStatus)
    {
        return new EventResponse
        {
            Id = evt.Id,
            CreatorId = evt.CreatorId,
            CreatorName = evt.CreatorName,
            CreatorAvatarUrl = evt.CreatorAvatarUrl,
            Title = evt.Title,
            Description = evt.Description,
            BannerUrl = evt.BannerUrl,
            Location = evt.Location,
            Campus = evt.Campus,
            StartTime = evt.StartTime,
            EndTime = evt.EndTime,
            MaxAttendees = evt.MaxAttendees,
            GoingCount = evt.GoingCount,
            InterestedCount = evt.InterestedCount,
            CreatedAt = evt.CreatedAt,
            MyStatus = myStatus
        };
    }
}
