using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Contracts.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ForumService.Services.Interfaces;

namespace ForumService.Services.Implementations;

public class KafkaProducerService : IKafkaProducerService
{
    private static readonly HttpClient SharedHttpClient = new() { Timeout = TimeSpan.FromSeconds(2.5) };
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _bootstrapServers;
    private readonly bool _kafkaEnabled;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _kafkaEnabled = !string.Equals(configuration["Kafka:Enabled"], "false", StringComparison.OrdinalIgnoreCase);
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? string.Empty;
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
        var envelope = new EventEnvelope<T>(
            eventType: eventType,
            version: version,
            producer: "f-alumni-forum-service",
            data: eventData,
            correlationId: partitionKey
        );

        var jsonPayload = JsonSerializer.Serialize(envelope);

        if (!_kafkaEnabled || string.IsNullOrWhiteSpace(_bootstrapServers))
        {
            await ForwardViaHttpBridgeAsync(topic, jsonPayload, cancellationToken);
            return;
        }

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
            _logger.LogWarning("Could not publish {EventType} to Kafka ({BootstrapServers}): {Message}. Falling back to HTTP event bridge.", eventType, _bootstrapServers, ex.Message);
            await ForwardViaHttpBridgeAsync(topic, jsonPayload, cancellationToken);
        }
    }

    private async Task ForwardViaHttpBridgeAsync(string topic, string rawEnvelopeJson, CancellationToken cancellationToken)
    {
        string? targetUrl = null;
        if (topic is "forum.post_reacted.v1" or "forum.comment_created.v1")
        {
            var baseUrl = (_configuration["Services:NotificationService:Url"] ?? "http://localhost:5006").TrimEnd('/');
            targetUrl = $"{baseUrl}/api/v1/internal/notifications/events/{topic}";
        }
        else if (topic == ReportCreatedEventV1.DefaultTopic)
        {
            var adminUrl = (_configuration["Services:AdminService:Url"] ?? "http://localhost:5005").TrimEnd('/');
            targetUrl = $"{adminUrl}/api/v1/internal/moderation/reports/events/{topic}";
        }

        if (targetUrl is null)
        {
            return;
        }

        try
        {
            using var content = new StringContent(rawEnvelopeJson, Encoding.UTF8, "application/json");
            await SharedHttpClient.PostAsync(targetUrl, content, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "HTTP event bridge unreachable at {Url}; continuing gracefully.", targetUrl);
        }
    }
}
