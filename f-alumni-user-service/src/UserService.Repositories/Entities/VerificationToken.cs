namespace UserService.Repositories.Entities;

/// <summary>
/// Stores OTP verification codes for email activation and password reset.
/// </summary>
public class VerificationToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string OtpCode { get; set; } = string.Empty;

    /// <summary>
    /// Purpose of token: EmailVerification or PasswordReset.
    /// </summary>
    public string Purpose { get; set; } = "EmailVerification";

    public DateTime ExpiresAtUtc { get; set; }

    public bool IsUsed { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
