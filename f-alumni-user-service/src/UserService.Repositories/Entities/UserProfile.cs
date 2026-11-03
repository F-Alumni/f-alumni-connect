namespace UserService.Repositories.Entities;

/// <summary>
/// Alumni/Student detailed profile information stored in FAlumni_User_Db.
/// Supports US-04 (Campus, Major, GraduationYear, Bio, Avatar), US-09 (Privacy settings), US-11 (Google Calendar Link).
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string AvatarUrl { get; set; } = "https://cdn.falumni.local/avatars/default.jpg";

    public string StudentCode { get; set; } = string.Empty;

    /// <summary>
    /// FPTU Campus (e.g., HCM, HN, DN, CT, QN) — required by US-04 &amp; US-06.
    /// </summary>
    public string Campus { get; set; } = "HCM";

    public int? GraduationYear { get; set; }

    public string Major { get; set; } = string.Empty;

    public string CurrentCompany { get; set; } = string.Empty;

    public string JobTitle { get; set; } = string.Empty;

    public string Bio { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public string LinkedInUrl { get; set; } = string.Empty;

    public string GitHubUrl { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// US-11: Google Calendar booking link so other students/alumni can schedule a meeting.
    /// </summary>
    public string GoogleCalendarUrl { get; set; } = string.Empty;

    /// <summary>
    /// US-09: Privacy toggles for public profile visibility.
    /// </summary>
    public bool ShowEmailPublicly { get; set; } = true;
    public bool ShowPhonePublicly { get; set; } = false;
    public bool ShowCompanyPublicly { get; set; } = true;
    public bool ShowCalendarPublicly { get; set; } = true;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
