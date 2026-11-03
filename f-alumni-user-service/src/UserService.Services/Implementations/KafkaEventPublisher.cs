using System.Text.Json;
using Confluent.Kafka;
using Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UserService.Services.Interfaces;

namespace UserService.Services.Implementations;

public class KafkaEventPublisher : IKafkaEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IConfiguration configuration, ILogger<KafkaEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishUserRegisteredAsync(
        UserRegisteredEventV1 eventData,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        return PublishDomainEventAsync(
            topic: UserRegisteredEventV1.DefaultTopic,
            eventType: UserRegisteredEventV1.EventTypeName,
            partitionKey: eventData.UserId.ToString(),
            data: eventData,
            correlationId: correlationId,
            cancellationToken: cancellationToken);
    }

    public async Task PublishDomainEventAsync<T>(
        string topic,
        string eventType,
        string partitionKey,
        T data,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");

        var envelope = new EventEnvelope<T>(
            eventType: eventType,
            version: "1.0",
            producer: "f-alumni-user-service",
            data: data,
            correlationId: correlationId);

        var jsonPayload = JsonSerializer.Serialize(envelope);

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogInformation(
                "[Kafka Disabled/No Broker] Event prepared locally: Topic={Topic}, EventType={EventType}, Key={Key}, EventId={EventId}",
                topic, eventType, partitionKey, envelope.EventId);
            return;
        }

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.Leader,
                MessageTimeoutMs = 2000,
                SocketTimeoutMs = 2000
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();
            var result = await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = partitionKey,
                Value = jsonPayload
            }, cancellationToken);

            _logger.LogInformation(
                "Published Kafka event {EventType} (EventId={EventId}) to {Topic} [Partition={Partition}, Offset={Offset}]",
                eventType, envelope.EventId, result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            // Graceful fallback when running on Render or local dev without Kafka running
            _logger.LogWarning(
                "Kafka broker '{BootstrapServers}' unreachable ({Message}). Event {EventType} (EventId={EventId}) logged locally.",
                bootstrapServers, ex.Message, eventType, envelope.EventId);
        }
    }
}
