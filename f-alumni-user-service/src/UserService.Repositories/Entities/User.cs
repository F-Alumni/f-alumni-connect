namespace UserService.Repositories.Entities;

/// <summary>
/// Core User identity entity owned exclusively by UserService (FAlumni_User_Db).
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Authorization Role: USER or ADMIN.
    /// </summary>
    public string Role { get; set; } = "USER";

    /// <summary>
    /// Account Type: Student, Alumni, or Admin.
    /// </summary>
    public string AccountType { get; set; } = "Alumni";

    /// <summary>
    /// Account lifecycle status: PendingVerification, Active, or Suspended.
    /// </summary>
    public string Status { get; set; } = "PendingVerification";

    public bool EmailVerified { get; set; }

    public string? SuspensionReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public UserProfile? Profile { get; set; }

    public ICollection<VerificationToken> VerificationTokens { get; set; } = new List<VerificationToken>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
