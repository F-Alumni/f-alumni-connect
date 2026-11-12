using System.Text.Json;
using Confluent.Kafka;
using Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Implementations;

public class KafkaProducerService : IKafkaProducerService
{
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _bootstrapServers;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:29092";
    }

    public Task PublishPostCreatedAsync(PostCreatedEventV1 eventData, CancellationToken cancellationToken = default)
    {
        return PublishEnvelopeAsync(
            PostCreatedEventV1.DefaultTopic,
            PostCreatedEventV1.EventTypeName,
            PostCreatedEventV1.CurrentVersion,
            eventData.PostId.ToString(),
            eventData,
            cancellationToken);
    }

    public Task PublishPostReactedAsync(PostReactedEventV1 eventData, CancellationToken cancellationToken = default)
    {
        return PublishEnvelopeAsync(
            PostReactedEventV1.DefaultTopic,
            PostReactedEventV1.EventTypeName,
            PostReactedEventV1.CurrentVersion,
            eventData.PostId.ToString(),
            eventData,
            cancellationToken);
    }

    public Task PublishCommentCreatedAsync(CommentCreatedEventV1 eventData, CancellationToken cancellationToken = default)
    {
        return PublishEnvelopeAsync(
            CommentCreatedEventV1.DefaultTopic,
            CommentCreatedEventV1.EventTypeName,
            CommentCreatedEventV1.CurrentVersion,
            eventData.PostId.ToString(),
            eventData,
            cancellationToken);
    }

    public Task PublishReportCreatedAsync(ReportCreatedEventV1 eventData, CancellationToken cancellationToken = default)
    {
        return PublishEnvelopeAsync(
            ReportCreatedEventV1.DefaultTopic,
            ReportCreatedEventV1.EventTypeName,
            ReportCreatedEventV1.CurrentVersion,
            eventData.ReportId.ToString(),
            eventData,
            cancellationToken);
    }

    private async Task PublishEnvelopeAsync<T>(
        string topic,
        string eventType,
        string version,
        string partitionKey,
        T eventData,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_bootstrapServers))
        {
            return;
        }

        var envelope = new EventEnvelope<T>(
            eventType: eventType,
            version: version,
            producer: "f-alumni-forum-service",
            data: eventData,
            correlationId: partitionKey
        );

        var jsonPayload = JsonSerializer.Serialize(envelope);

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                Acks = Acks.All,
                MessageTimeoutMs = 3000
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();
            var deliveryResult = await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = partitionKey,
                Value = jsonPayload
            }, cancellationToken);

            _logger.LogInformation("Successfully published {EventType} to Kafka topic {Topic} at offset {Offset}", eventType, topic, deliveryResult.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not publish {EventType} to Kafka ({BootstrapServers}): {Message}. Continuing gracefully.", eventType, _bootstrapServers, ex.Message);
        }
    }
}
