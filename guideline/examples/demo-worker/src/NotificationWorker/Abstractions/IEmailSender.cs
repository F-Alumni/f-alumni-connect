namespace NotificationWorker.Abstractions;

/// <summary>
/// Abstraction for delivering notification emails.
/// Enables swapping development mock with production SMTP (MailKit) or cloud provider (SendGrid, SES).
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Dispatches an email message asynchronously.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="body">Plain-text or HTML message content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
