using Microsoft.EntityFrameworkCore;
using ForumService.Repositories.Context;
using ForumService.Repositories.Entities;
using ForumService.Repositories.Interfaces;

namespace ForumService.Repositories.Implementations;

public class EventRepository : IEventRepository
{
    private readonly ForumServiceDbContext _dbContext;

    public EventRepository(ForumServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Events
            .Include(e => e.Attendees)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Event> Items, int TotalCount)> GetEventsAsync(
        string? campus = null,
        bool? upcomingOnly = true,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Events.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(campus) && !campus.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.Campus == campus || e.Campus == "All");
        }

        if (upcomingOnly == true)
        {
            query = query.Where(e => e.EndTime >= DateTime.UtcNow);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(e => e.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Event> AddAsync(Event evt, CancellationToken cancellationToken = default)
    {
        await _dbContext.Events.AddAsync(evt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return evt;
    }

    public async Task UpdateAsync(Event evt, CancellationToken cancellationToken = default)
    {
        _dbContext.Events.Update(evt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Event evt, CancellationToken cancellationToken = default)
    {
        _dbContext.Events.Remove(evt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EventAttendee?> GetAttendeeAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventAttendees
            .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<EventAttendee>> GetAttendeesByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.EventAttendees
            .AsNoTracking()
            .Where(a => a.EventId == eventId)
            .OrderByDescending(a => a.RegisteredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddOrUpdateAttendeeAsync(EventAttendee attendee, CancellationToken cancellationToken = default)
    {
        var existing = await GetAttendeeAsync(attendee.EventId, attendee.UserId, cancellationToken);
        var evt = await _dbContext.Events.FindAsync([attendee.EventId], cancellationToken);

        if (existing == null)
        {
            await _dbContext.EventAttendees.AddAsync(attendee, cancellationToken);
            if (evt != null)
            {
                if (attendee.Status == "Going") evt.GoingCount++;
                else if (attendee.Status == "Interested") evt.InterestedCount++;
            }
        }
        else
        {
            if (evt != null)
            {
                if (existing.Status == "Going") evt.GoingCount = Math.Max(0, evt.GoingCount - 1);
                else if (existing.Status == "Interested") evt.InterestedCount = Math.Max(0, evt.InterestedCount - 1);

                if (attendee.Status == "Going") evt.GoingCount++;
                else if (attendee.Status == "Interested") evt.InterestedCount++;
            }
            existing.Status = attendee.Status;
            existing.RegisteredAt = DateTime.UtcNow;
            _dbContext.EventAttendees.Update(existing);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
