using ForumService.Repositories.Entities;

namespace ForumService.Repositories.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Event> Items, int TotalCount)> GetEventsAsync(
        string? campus = null,
        bool? upcomingOnly = true,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<Event> AddAsync(Event evt, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event evt, CancellationToken cancellationToken = default);
    Task DeleteAsync(Event evt, CancellationToken cancellationToken = default);
    Task<EventAttendee?> GetAttendeeAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventAttendee>> GetAttendeesByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task AddOrUpdateAttendeeAsync(EventAttendee attendee, CancellationToken cancellationToken = default);
}
