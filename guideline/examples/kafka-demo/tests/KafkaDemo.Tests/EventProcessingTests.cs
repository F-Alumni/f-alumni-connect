using System.Text.Json;
using Confluent.Kafka;
using KafkaConsumer;
using KafkaDemo.Contracts;
using Xunit;

namespace KafkaDemo.Tests;

public class EventProcessingTests
{
    private readonly EventProcessor _processor = new();

    [Fact]
    public void ProcessRawMessage_ValidJson_ReturnsSuccessAndDeserializedEnvelope()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "student@fpt.edu.vn", "Le Van B", "654321");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData
        );
        var json = JsonSerializer.Serialize(envelope);

        // Act
        var result = _processor.ProcessRawMessage(json, out var deserialized, out var errorMessage);

        // Assert
        Assert.Equal(ProcessResult.Success, result);
        Assert.Null(errorMessage);
        Assert.NotNull(deserialized);
        Assert.Equal(envelope.EventId, deserialized.EventId);
        Assert.Equal("user.registered.v1", deserialized.EventType);
        Assert.Equal(userId, deserialized.Data.UserId);
        Assert.Equal("student@fpt.edu.vn", deserialized.Data.Email);
        Assert.True(_processor.IsProcessed(envelope.EventId));
    }

    [Fact]
    public void ProcessRawMessage_DuplicateEventId_ReturnsDuplicateAndAvoidsReprocessing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "student@fpt.edu.vn", "Le Van B", "654321");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData
        );
        var json = JsonSerializer.Serialize(envelope);

        // Act 1: First delivery
        var firstResult = _processor.ProcessRawMessage(json, out _, out _);
        Assert.Equal(ProcessResult.Success, firstResult);

        // Act 2: Redelivery of duplicate message (At-least-once replay scenario)
        var secondResult = _processor.ProcessRawMessage(json, out var duplicateEnvelope, out var errorMessage);

        // Assert: Second delivery recognized as duplicate (Idempotent)
        Assert.Equal(ProcessResult.Duplicate, secondResult);
        Assert.Null(errorMessage);
        Assert.NotNull(duplicateEnvelope);
        Assert.Equal(envelope.EventId, duplicateEnvelope.EventId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("{\"invalid\": true}")]
    [InlineData("not a json payload")]
    public void ProcessRawMessage_MalformedPayload_ReturnsMalformedWithoutCrashing(string badPayload)
    {
        // Act
        var result = _processor.ProcessRawMessage(badPayload, out var envelope, out var errorMessage);

        // Assert
        Assert.Equal(ProcessResult.Malformed, result);
        Assert.Null(envelope);
        Assert.NotNull(errorMessage);
    }

    [Fact]
    public void EventEnvelope_SerializationRoundTrip_MatchesContractAndJsonFieldNames()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "test@fpt.edu.vn", "Tran D", "123456");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData,
            "trace-correlation-123"
        );

        // Act
        var json = JsonSerializer.Serialize(envelope);

        // Assert JSON schema field names (camelCase)
        Assert.Contains("\"eventId\":", json);
        Assert.Contains("\"eventType\":\"user.registered.v1\"", json);
        Assert.Contains("\"version\":\"1.0\"", json);
        Assert.Contains("\"occurredAt\":", json);
        Assert.Contains("\"correlationId\":\"trace-correlation-123\"", json);
        Assert.Contains("\"producer\":\"f-alumni-user-service\"", json);
        Assert.Contains("\"data\":", json);
        Assert.Contains("\"userId\":", json);
        Assert.Contains("\"email\":\"test@fpt.edu.vn\"", json);
        Assert.Contains("\"fullName\":\"Tran D\"", json);
        Assert.Contains("\"verificationCode\":\"123456\"", json);
    }

    [Fact]
    public void EventProcessor_SimulateAtLeastOnceDelivery_ProcessesOnceAndSkipsSubsequentDeliveries()
    {
        // Arrange: At-least-once delivery simulation where broker redelivers message multiple times
        var userId = Guid.NewGuid();
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            new UserRegisteredEventV1(userId, "replay@fpt.edu.vn", "Replay Test", "111222")
        );
        var json = JsonSerializer.Serialize(envelope);

        // Act 1: Initial delivery
        var result1 = _processor.ProcessRawMessage(json, out _, out _);

        // Act 2 & 3: Replay deliveries (e.g. consumer restarted before committing offset)
        var result2 = _processor.ProcessRawMessage(json, out _, out _);
        var result3 = _processor.ProcessRawMessage(json, out _, out _);

        // Assert
        Assert.Equal(ProcessResult.Success, result1);
        Assert.Equal(ProcessResult.Duplicate, result2);
        Assert.Equal(ProcessResult.Duplicate, result3);
    }

    [Fact]
    public async Task KafkaMockBroker_ProducerPublishesWithKeyAndReturnsPersistedStatus()
    {
        // Arrange: Use librdkafka's in-process mock broker to test producer publish and delivery report
        var topic = UserRegisteredEventV1.DefaultTopic;
        var producerConfig = new ProducerConfig();
        producerConfig.Set("test.mock.num.brokers", "1");

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        var userId = Guid.NewGuid();
        var eventData = new UserRegisteredEventV1(userId, "mock@fpt.edu.vn", "Tran C", "999888");
        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            UserRegisteredEventV1.EventTypeName,
            UserRegisteredEventV1.CurrentVersion,
            "f-alumni-user-service",
            eventData
        );
        var jsonPayload = JsonSerializer.Serialize(envelope);

        // Act: Produce to mock cluster with partition key = userId
        var deliveryResult = await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = userId.ToString(),
            Value = jsonPayload
        });

        // Assert delivery report: key must match userId, status must be Persisted
        Assert.NotNull(deliveryResult);
        Assert.Equal(PersistenceStatus.Persisted, deliveryResult.Status);
        Assert.Equal(topic, deliveryResult.Topic);
        Assert.Equal(userId.ToString(), deliveryResult.Message.Key);
    }
}
