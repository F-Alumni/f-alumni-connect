using System.ComponentModel.DataAnnotations;

namespace AdminService.Services.DTOs.Users;

public class SuspendUserRequest
{
    [Required(ErrorMessage = "Reason for suspension is required.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "Reason must be between 3 and 1000 characters.")]
    public string Reason { get; set; } = string.Empty;

    [Range(1, 365, ErrorMessage = "Suspension duration must be between 1 and 365 days if specified.")]
    public int? DurationDays { get; set; }
}

public class UnsuspendUserRequest
{
    [StringLength(1000, ErrorMessage = "Reason must not exceed 1000 characters.")]
    public string? Reason { get; set; }
}
