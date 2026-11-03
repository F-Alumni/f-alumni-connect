using NotificationWorker.Abstractions;

namespace NotificationWorker.Services;

/// <summary>
/// Record representing a successfully dispatched email in the mock development delivery store.
/// </summary>
public record SentEmailRecord(
    string ToEmail,
    string Subject,
    string Body,
    DateTime SentAtUtc
);

/// <summary>
/// Development implementation of IEmailSender that logs safe, redacted mock delivery results
/// without requiring actual SMTP credentials or sending real network emails.
/// </summary>
public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;
    private readonly List<SentEmailRecord> _sentEmails = [];

    public IReadOnlyList<SentEmailRecord> SentEmails
    {
        get
        {
            lock (_sentEmails) return _sentEmails.ToList();
        }
    }

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var record = new SentEmailRecord(toEmail, subject, body, DateTime.UtcNow);
        lock (_sentEmails)
        {
            _sentEmails.Add(record);
        }

        // Mask recipient email (e.g. "student2026@fpt.edu.vn" -> "st***@fpt.edu.vn")
        var maskedEmail = MaskEmail(toEmail);

        // Safe logging: Log recipient and subject, but NEVER print raw OTP codes or secrets!
        _logger.LogInformation(
            "[MOCK EMAIL DELIVERED] Recipient: {MaskedEmail} | Subject: {Subject} | Provider: DevMock | Timestamp: {Timestamp:O}",
            maskedEmail, subject, record.SentAtUtc);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Masks the local part of an email address for privacy and log safety.
    /// </summary>
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "***";
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        var name = parts[0];
        var domain = parts[1];
        var maskedName = name.Length <= 2 ? name + "***" : name[..2] + "***";
        return $"{maskedName}@{domain}";
    }
}
