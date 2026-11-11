namespace UserService.Services.Interfaces;

/// <summary>
/// Sends transactional emails (OTP verification, password reset) via Gmail SMTP or configured SMTP provider.
/// </summary>
public interface IEmailService
{
    Task SendOtpEmailAsync(
        string toEmail,
        string fullName,
        string otpCode,
        string purpose = "EmailVerification",
        CancellationToken cancellationToken = default);
}
