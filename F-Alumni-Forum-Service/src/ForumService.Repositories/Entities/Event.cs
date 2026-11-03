namespace ForumService.Repositories.Entities;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string CreatorAvatarUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BannerUrl { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Campus { get; set; } = "All"; // FU-HCM, FU-HL, FU-DN, FU-CT, FU-QN, All
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? MaxAttendees { get; set; }
    public int GoingCount { get; set; }
    public int InterestedCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
}

public class EventAttendee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserAvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Going"; // Going, Interested, NotGoing
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public Event? Event { get; set; }
}
