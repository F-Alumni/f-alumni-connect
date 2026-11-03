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

    public async Task PublishPostCreatedAsync(PostCreatedEventV1 eventData, CancellationToken cancellationToken = default)
    {
        var topic = PostCreatedEventV1.DefaultTopic;
        var envelope = new EventEnvelope<PostCreatedEventV1>(
            eventType: PostCreatedEventV1.EventTypeName,
            version: PostCreatedEventV1.CurrentVersion,
            producer: "f-alumni-forum-service",
            data: eventData
        );

        var jsonPayload = JsonSerializer.Serialize(envelope);
        var partitionKey = eventData.PostId.ToString();

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

            _logger.LogInformation("Successfully published PostCreatedEvent to Kafka topic {Topic} at offset {Offset}", topic, deliveryResult.Offset);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Could not publish event to Kafka ({BootstrapServers}): {Message}. Continuing gracefully.", _bootstrapServers, ex.Message);
        }
    }
}
