using System.ComponentModel.DataAnnotations;

namespace AdminService.Services.DTOs.Moderation;

public class ModerationReportResponse
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public string ReporterEmail { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ResolvedByAdminId { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ResolveReportRequest
{
    [Required(ErrorMessage = "Action taken is required (e.g., ContentRemoved, UserSuspended, WarningIssued).")]
    public string ActionTaken { get; set; } = string.Empty;

    [Required(ErrorMessage = "Resolution notes are required.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Resolution notes must be between 3 and 1000 characters.")]
    public string ResolutionNotes { get; set; } = string.Empty;

    public bool DeleteTargetContent { get; set; } = false;
}

public class DismissReportRequest
{
    [Required(ErrorMessage = "Dismissal reason is required.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Dismissal reason must be between 3 and 1000 characters.")]
    public string DismissalReason { get; set; } = string.Empty;
}

public class ModerationReportFilterQuery
{
    public string? Status { get; set; }
    public string? TargetType { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}
