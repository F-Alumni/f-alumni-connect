using System.Text.Json;
using Confluent.Kafka;
using Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationWorker.Abstractions;
using NotificationWorker.Services;
using NotificationWorker.Workers;
using Xunit;

namespace NotificationWorker.Tests;

public class WorkerTests
{
    [Fact]
    public async Task ProcessMessageAsync_ValidRegistrationEvent_DispatchesEmailAndRecordsDelivery()
    {
        // Arrange
        var logger = new TestLogger<NotificationEventProcessor>();
        var emailLogger = new TestLogger<MockEmailSender>();
        var emailSender = new MockEmailSender(emailLogger);
        var tracker = new InMemoryProcessedEventTracker();
        var processor = new NotificationEventProcessor(emailSender, tracker, logger);

        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "student@fpt.edu.vn", "Nguyen Van A", "123456");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData,
            "corr-12345"
        );
        var json = JsonSerializer.Serialize(envelope);

        // Act
        var result = await processor.ProcessMessageAsync(json);

        // Assert
        Assert.Equal(EventProcessingStatus.Success, result);
        Assert.Single(emailSender.SentEmails);
        var sentRecord = emailSender.SentEmails[0];
        Assert.Equal("student@fpt.edu.vn", sentRecord.ToEmail);
        Assert.Contains("123456", sentRecord.Body);
        Assert.True(await tracker.HasBeenProcessedAsync(envelope.EventId));
    }

    [Fact]
    public async Task ProcessMessageAsync_DuplicateEvent_DoesNotSendSecondEmail()
    {
        // Arrange
        var logger = new TestLogger<NotificationEventProcessor>();
        var emailSender = new MockEmailSender(new TestLogger<MockEmailSender>());
        var tracker = new InMemoryProcessedEventTracker();
        var processor = new NotificationEventProcessor(emailSender, tracker, logger);

        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "student@fpt.edu.vn", "Nguyen Van A", "123456");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData
        );
        var json = JsonSerializer.Serialize(envelope);

        // Act 1: Initial delivery
        var firstResult = await processor.ProcessMessageAsync(json);

        // Act 2: Redelivery of duplicate message (e.g. consumer restarted before offset commit)
        var secondResult = await processor.ProcessMessageAsync(json);

        // Assert: Exactly ONE email sent, second invocation identified as duplicate
        Assert.Equal(EventProcessingStatus.Success, firstResult);
        Assert.Equal(EventProcessingStatus.Duplicate, secondResult);
        Assert.Single(emailSender.SentEmails);
    }

    [Fact]
    public async Task ProcessMessageAsync_EmailSenderThrows_DoesNotMarkEventProcessed()
    {
        // Arrange
        var logger = new TestLogger<NotificationEventProcessor>();
        var failingEmailSender = new ThrowingEmailSender();
        var tracker = new InMemoryProcessedEventTracker();
        var processor = new NotificationEventProcessor(failingEmailSender, tracker, logger);

        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "student@fpt.edu.vn", "Nguyen Van A", "123456");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData
        );
        var json = JsonSerializer.Serialize(envelope);

        // Act
        var result = await processor.ProcessMessageAsync(json);

        // Assert: Processing failed, event is NOT marked processed (enabling at-least-once retry)
        Assert.Equal(EventProcessingStatus.Failed, result);
        Assert.False(await tracker.HasBeenProcessedAsync(envelope.EventId));
    }

    [Fact]
    public async Task SafeLogging_VerificationTokenNeverLoggedToLogger()
    {
        // Arrange
        var processorLogger = new TestLogger<NotificationEventProcessor>();
        var emailLogger = new TestLogger<MockEmailSender>();
        var emailSender = new MockEmailSender(emailLogger);
        var tracker = new InMemoryProcessedEventTracker();
        var processor = new NotificationEventProcessor(emailSender, tracker, processorLogger);

        var userId = Guid.NewGuid();
        var secretOtp = "998877";
        var eventData = new UserRegisteredEventV1(userId, "privacy@fpt.edu.vn", "Secret Student", secretOtp);
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData,
            "trace-corr-999"
        );
        var json = JsonSerializer.Serialize(envelope);

        // Act
        await processor.ProcessMessageAsync(json);

        // Assert: EventId and CorrelationId are logged for traceability
        var allProcessorLogs = string.Join("\n", processorLogger.Messages);
        var allEmailLogs = string.Join("\n", emailLogger.Messages);

        Assert.Contains(envelope.EventId.ToString(), allProcessorLogs);
        Assert.Contains("trace-corr-999", allProcessorLogs);

        // CRITICAL ASSERTION: The sensitive OTP verification code is NEVER printed in any logs!
        Assert.DoesNotContain(secretOtp, allProcessorLogs);
        Assert.DoesNotContain(secretOtp, allEmailLogs);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a json payload")]
    [InlineData("{\"invalid\": true}")]
    public async Task ProcessMessageAsync_MalformedJson_RejectsGracefully(string badJson)
    {
        // Arrange
        var logger = new TestLogger<NotificationEventProcessor>();
        var emailSender = new MockEmailSender(new TestLogger<MockEmailSender>());
        var tracker = new InMemoryProcessedEventTracker();
        var processor = new NotificationEventProcessor(emailSender, tracker, logger);

        // Act
        var result = await processor.ProcessMessageAsync(badJson);

        // Assert
        Assert.Equal(EventProcessingStatus.Malformed, result);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task BackgroundService_ConsumesRealKafkaConsumeResult_DispatchesEmailAndCommits()
    {
        // Arrange
        var emailSender = new MockEmailSender(new TestLogger<MockEmailSender>());
        var tracker = new InMemoryProcessedEventTracker();
        var processor = new NotificationEventProcessor(emailSender, tracker, new TestLogger<NotificationEventProcessor>());

        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "real.event@fpt.edu.vn", "Tran Van B", "654321");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData,
            "corr-kafka-real"
        );
        var jsonPayload = JsonSerializer.Serialize(envelope);

        // Build a real Confluent.Kafka ConsumeResult
        var realConsumeResult = new ConsumeResult<string, string>
        {
            Topic = UserRegisteredEventV1.DefaultTopic,
            Partition = new Partition(0),
            Offset = new Offset(105),
            Message = new Message<string, string>
            {
                Key = userId.ToString(),
                Value = jsonPayload
            }
        };

        var mockConsumer = new Moq.Mock<IConsumer<string, string>>();
        var consumed = false;
        mockConsumer.Setup(c => c.Consume(Moq.It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (!consumed)
                {
                    consumed = true;
                    return realConsumeResult;
                }
                // Block until cancelled
                Thread.Sleep(500);
                return null!;
            });

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Kafka:Topic"] = UserRegisteredEventV1.DefaultTopic
        }).Build();

        var workerLogger = new TestLogger<NotificationBackgroundService>();
        var worker = new NotificationBackgroundService(mockConsumer.Object, processor, configuration, workerLogger);

        using var cts = new CancellationTokenSource();

        // Act: Run worker in background and cancel after consuming
        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(200); // Allow consumer loop to process message
        cts.Cancel();
        await workerTask;

        // Assert: Subscribe was called, email was sent, and offset was committed
        mockConsumer.Verify(c => c.Subscribe(UserRegisteredEventV1.DefaultTopic), Moq.Times.Once);
        mockConsumer.Verify(c => c.Commit(Moq.It.Is<ConsumeResult<string, string>>(r => r.Offset.Value == 105)), Moq.Times.Once);
        Assert.Single(emailSender.SentEmails);
        Assert.Equal("real.event@fpt.edu.vn", emailSender.SentEmails[0].ToEmail);
    }

    [Fact]
    public void MaskEmail_VariousEmailFormats_MasksSafely()
    {
        Assert.Equal("st***@fpt.edu.vn", MockEmailSender.MaskEmail("student@fpt.edu.vn"));
        Assert.Equal("a***@fpt.edu.vn", MockEmailSender.MaskEmail("a@fpt.edu.vn"));
        Assert.Equal("***", MockEmailSender.MaskEmail(""));
        Assert.Equal("***", MockEmailSender.MaskEmail("invalid-email"));
    }
}

// =========================================================================
// Test Doubles
// =========================================================================

public class ThrowingEmailSender : IEmailSender
{
    public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulated transient SMTP server connection failure.");
    }
}

public class TestLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (Messages)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
