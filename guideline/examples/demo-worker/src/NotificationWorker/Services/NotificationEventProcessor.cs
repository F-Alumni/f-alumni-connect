using System.Text.Json;
using Contracts.Events;
using NotificationWorker.Abstractions;

namespace NotificationWorker.Services;

public enum EventProcessingStatus
{
    Success,
    Duplicate,
    Malformed,
    Failed
}

/// <summary>
/// Core domain event handler for processing user registration events and triggering email notifications.
/// Handles safe deserialization, idempotency deduplication, email composition, and safe logging.
/// </summary>
public class NotificationEventProcessor
{
    private readonly IEmailSender _emailSender;
    private readonly IProcessedEventTracker _tracker;
    private readonly ILogger<NotificationEventProcessor> _logger;

    public NotificationEventProcessor(
        IEmailSender emailSender,
        IProcessedEventTracker tracker,
        ILogger<NotificationEventProcessor> logger)
    {
        _emailSender = emailSender;
        _tracker = tracker;
        _logger = logger;
    }

    /// <summary>
    /// Processes a raw Kafka JSON message containing an EventEnvelope of UserRegisteredEventV1.
    /// </summary>
    public async Task<EventProcessingStatus> ProcessMessageAsync(
        string rawJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _logger.LogWarning("Rejected message: payload is null, empty, or whitespace.");
            return EventProcessingStatus.Malformed;
        }

        // 1. Safe Deserialization
        EventEnvelope<UserRegisteredEventV1>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope<UserRegisteredEventV1>>(rawJson);
            if (envelope is null || envelope.EventId == Guid.Empty || envelope.Data is null || string.IsNullOrWhiteSpace(envelope.Data.Email))
            {
                _logger.LogWarning("Rejected message: missing required envelope properties, empty EventId, or empty recipient email.");
                return EventProcessingStatus.Malformed;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse message as JSON EventEnvelope<UserRegisteredEventV1>.");
            return EventProcessingStatus.Malformed;
        }

        // 2. Safe Logging: Log EventId and CorrelationId ONLY — NEVER log raw OTP verification codes!
        _logger.LogInformation(
            "Consuming event: EventId={EventId}, CorrelationId={CorrelationId}, EventType={EventType}, Producer={Producer}",
            envelope.EventId, envelope.CorrelationId, envelope.EventType, envelope.Producer);

        // 3. Idempotency Check: Prevent duplicate email sending
        if (await _tracker.HasBeenProcessedAsync(envelope.EventId, cancellationToken))
        {
            _logger.LogWarning(
                "Duplicate event skipped: EventId={EventId}, CorrelationId={CorrelationId} was already processed (Idempotent).",
                envelope.EventId, envelope.CorrelationId);
            return EventProcessingStatus.Duplicate;
        }

        // 4. Compose Verification Email
        // Note: The token is passed securely in memory to the email body, but NEVER written to logger.
        var subject = "Chào mừng bạn đến với FPT Alumni Connect — Mã xác thực tài khoản";
        var body = $"Xin chào {envelope.Data.FullName},\n\n" +
                   $"Cảm ơn bạn đã đăng ký tài khoản tại cổng thông tin FPT Alumni Connect.\n" +
                   $"Mã xác thực OTP của bạn là: {envelope.Data.VerificationCode}\n\n" +
                   $"Mã xác thực này có hiệu lực trong vòng 15 phút. Vui lòng không chia sẻ mã cho bất kỳ ai.\n\n" +
                   $"Trân trọng,\nBan Quản trị FPT Alumni Connect";

        // 5. Dispatch Email via IEmailSender
        try
        {
            await _emailSender.SendEmailAsync(envelope.Data.Email, subject, body, cancellationToken);

            // 6. Record as processed only after email dispatch succeeds
            await _tracker.MarkAsProcessedAsync(envelope.EventId, cancellationToken);

            _logger.LogInformation(
                "Email dispatched successfully: EventId={EventId}, CorrelationId={CorrelationId}",
                envelope.EventId, envelope.CorrelationId);

            return EventProcessingStatus.Success;
        }
        catch (Exception ex)
        {
            // Email dispatch failed: do NOT mark as processed so it can be retried by Kafka
            _logger.LogError(ex,
                "Email dispatch failed: EventId={EventId}, CorrelationId={CorrelationId}. Event will be retried.",
                envelope.EventId, envelope.CorrelationId);
            return EventProcessingStatus.Failed;
        }
    }
}
