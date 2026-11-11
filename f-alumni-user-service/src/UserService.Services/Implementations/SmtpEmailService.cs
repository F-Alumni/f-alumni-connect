using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Services.Interfaces;

namespace UserService.Services.Implementations;

/// <summary>
/// Sends OTP emails via Gmail SMTP (or any configured SMTP server).
/// Falls back gracefully to structured log output if SMTP credentials are not configured yet.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(
        string toEmail,
        string fullName,
        string otpCode,
        string purpose = "EmailVerification",
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"] ?? Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
        var portStr = _configuration["Smtp:Port"] ?? Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587";
        var username = _configuration["Smtp:Username"] ?? Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? string.Empty;
        var password = _configuration["Smtp:Password"] ?? Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? string.Empty;
        var fromEmail = _configuration["Smtp:FromEmail"] ?? Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? username;
        var fromName = _configuration["Smtp:FromName"] ?? "FPT Alumni Connect";
        var enableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) || ssl;

        var isReset = string.Equals(purpose, "PasswordReset", StringComparison.OrdinalIgnoreCase);
        var subject = isReset
            ? $"[FPT Alumni Connect] Mã OTP đặt lại mật khẩu: {otpCode}"
            : $"[FPT Alumni Connect] Mã OTP xác thực tài khoản: {otpCode}";

        var htmlBody = $@"
<div style=""font-family: Arial, sans-serif; max-width: 520px; margin: 0 auto; padding: 24px; border: 1px solid #e5e7eb; border-radius: 12px;"">
  <h2 style=""color: #f37021; margin-top: 0;"">FPT Alumni Connect</h2>
  <p>Xin chào <strong>{WebUtility.HtmlEncode(fullName)}</strong>,</p>
  <p>{(isReset ? "Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản FPT Alumni Connect." : "Cảm ơn bạn đã đăng ký tài khoản trên hệ thống FPT Alumni Connect.")}</p>
  <p>Mã xác thực OTP (6 chữ số) của bạn là:</p>
  <div style=""background: #f9fafb; border: 1px dashed #f37021; padding: 16px; text-align: center; font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #111827; border-radius: 8px; margin: 16px 0;"">
    {WebUtility.HtmlEncode(otpCode)}
  </div>
  <p style=""color: #6b7280; font-size: 13px;"">Mã OTP này có hiệu lực trong vòng <strong>15 phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
</div>";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation(
                "[SMTP Not Configured] Prepared OTP Email for {Email} (Purpose={Purpose}). Configure Smtp:Username and Smtp:Password (Gmail App Password) to send real emails.",
                MaskEmail(toEmail),
                purpose);
            return;
        }

        try
        {
            var port = int.TryParse(portStr, out var p) ? p : 587;
            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username.Trim(), password.Replace(" ", "").Trim()),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(string.IsNullOrWhiteSpace(fromEmail) ? username : fromEmail, fromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(new MailAddress(toEmail));

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            _logger.LogInformation("Successfully sent OTP email ({Purpose}) via Gmail SMTP to {Email}.", purpose, MaskEmail(toEmail));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send OTP email via SMTP ({Host}:{Port}) to {Email}: {Message}. Registration/Reset flow continues with OTP fallback.",
                host,
                portStr,
                MaskEmail(toEmail),
                ex.Message);
        }
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "***";
        }

        var parts = email.Split('@');
        var name = parts[0];
        var prefix = name.Length <= 2 ? name : name[..2];
        return $"{prefix}***@{parts[1]}";
    }
}
