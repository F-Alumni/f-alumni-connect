using System.ComponentModel.DataAnnotations;

namespace AdminService.Services.DTOs.Announcements;

public class SystemAnnouncementResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid CreatedByAdminId { get; set; }
    public string CreatedByAdminEmail { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class CreateAnnouncementRequest
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 250 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [StringLength(10000, MinimumLength = 5, ErrorMessage = "Content must be between 5 and 10000 characters.")]
    public string Content { get; set; } = string.Empty;

    [StringLength(50)]
    public string Type { get; set; } = "General"; // General, Urgent, Academic, Event, Maintenance

    [StringLength(20)]
    public string Priority { get; set; } = "Normal"; // Normal, High, Urgent

    public bool IsPublished { get; set; } = true;

    public DateTime? ExpiresAtUtc { get; set; }
}

public class UpdateAnnouncementRequest
{
    [StringLength(250, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 250 characters.")]
    public string? Title { get; set; }

    [StringLength(10000, MinimumLength = 5, ErrorMessage = "Content must be between 5 and 10000 characters.")]
    public string? Content { get; set; }

    [StringLength(50)]
    public string? Type { get; set; }

    [StringLength(20)]
    public string? Priority { get; set; }

    public bool? IsPublished { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

public class AnnouncementFilterQuery
{
    public bool IncludeUnpublished { get; set; } = false;
    public string? Type { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
