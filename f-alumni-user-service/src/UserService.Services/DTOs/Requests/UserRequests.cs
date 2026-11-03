using System.ComponentModel.DataAnnotations;

namespace UserService.Services.DTOs.Requests;

public class UpdateUserProfileRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500)]
    public string AvatarUrl { get; set; } = string.Empty;

    [StringLength(30)]
    public string StudentCode { get; set; } = string.Empty;

    [StringLength(50)]
    public string Campus { get; set; } = "HCM";

    public int? GraduationYear { get; set; }

    [StringLength(100)]
    public string Major { get; set; } = string.Empty;

    [StringLength(150)]
    public string CurrentCompany { get; set; } = string.Empty;

    [StringLength(150)]
    public string JobTitle { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Bio { get; set; } = string.Empty;

    [StringLength(500)]
    public string Skills { get; set; } = string.Empty;

    [StringLength(300)]
    public string LinkedInUrl { get; set; } = string.Empty;

    [StringLength(300)]
    public string GitHubUrl { get; set; } = string.Empty;

    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(500)]
    public string GoogleCalendarUrl { get; set; } = string.Empty;
}

public class UpdateAvatarRequest
{
    [Required(ErrorMessage = "Avatar URL is required.")]
    [StringLength(500)]
    public string AvatarUrl { get; set; } = string.Empty;
}

public class UpdateCalendarLinkRequest
{
    [Required(ErrorMessage = "Google Calendar URL is required.")]
    [StringLength(500)]
    public string GoogleCalendarUrl { get; set; } = string.Empty;
}

public class UpdatePrivacySettingsRequest
{
    public bool ShowEmailPublicly { get; set; } = true;
    public bool ShowPhonePublicly { get; set; } = false;
    public bool ShowCompanyPublicly { get; set; } = true;
    public bool ShowCalendarPublicly { get; set; } = true;
}

public class BlockUserRequest
{
    [StringLength(300)]
    public string Reason { get; set; } = string.Empty;
}

public class SuspendUserRequest
{
    [Required(ErrorMessage = "Suspension reason is required.")]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

public class UpdateUserRoleRequest
{
    /// <summary>
    /// USER or ADMIN
    /// </summary>
    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(USER|ADMIN)$", ErrorMessage = "Role must be USER or ADMIN.")]
    public string Role { get; set; } = "USER";

    /// <summary>
    /// Student, Alumni, or Admin
    /// </summary>
    [StringLength(30)]
    public string AccountType { get; set; } = "Alumni";
}
