using ForumService.Services.DTOs.Common;
using ForumService.Services.DTOs.Requests;
using ForumService.Services.DTOs.Responses;

namespace ForumService.Services.Interfaces;

public interface IEventService
{
    Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid creatorId, string creatorName, string creatorAvatarUrl, CancellationToken cancellationToken = default);
    Task<EventResponse> GetEventByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResponse<EventResponse>> GetEventsAsync(
        string? campus = null,
        bool? upcomingOnly = true,
        int page = 1,
        int pageSize = 10,
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default);
    Task RsvpEventAsync(Guid eventId, RsvpEventRequest request, Guid userId, string userName, string userAvatarUrl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventAttendeeResponse>> GetAttendeesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task DeleteEventAsync(Guid eventId, Guid currentUserId, bool isAdmin = false, CancellationToken cancellationToken = default);
}
