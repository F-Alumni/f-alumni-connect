using System.Text.Json;
using Confluent.Kafka;
using Contracts.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AiService.Services.Interfaces;

namespace AiService.Api.Workers;

/// <summary>
/// Background Kafka consumer worker that subscribes to the 'post.created.v1' domain event topic.
/// Automatically ingests new forum posts into the RAG knowledge base in real-time.
/// </summary>
public class PostCreatedConsumerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostCreatedConsumerWorker> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PostCreatedConsumerWorker(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<PostCreatedConsumerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:29092";

        var topic = _configuration["Kafka:PostCreatedTopic"] 
            ?? PostCreatedEventV1.DefaultTopic;

        var consumerGroup = _configuration["Kafka:ConsumerGroup"] 
            ?? "falumni-ai-service.post-created-group";

        // Optional switch to disable consumer (e.g. for pure unit testing without Kafka)
        var isKafkaEnabled = _configuration.GetValue("Kafka:Enabled", true);
        if (!isKafkaEnabled || string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogInformation("Kafka PostCreatedConsumerWorker is disabled by configuration.");
            return;
        }

        // Delay 2 seconds to allow ASP.NET Core and DbContext initialization to complete
        await Task.Delay(2000, stoppingToken);

        _logger.LogInformation("Initializing Kafka PostCreatedConsumerWorker. Servers: {Servers}, Topic: {Topic}, Group: {Group}",
            bootstrapServers, topic, consumerGroup);

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Manual commit after processing
            SocketTimeoutMs = 10000,
            SessionTimeoutMs = 30000,
            EnablePartitionEof = false
        };

        // Run consumer on a background thread
        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var consumer = new ConsumerBuilder<string, string>(config).Build();
                    consumer.Subscribe(topic);
                    _logger.LogInformation("Subscribed to Kafka topic '{Topic}'. Waiting for messages...", topic);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(2));
                            if (consumeResult == null || consumeResult.Message == null || string.IsNullOrWhiteSpace(consumeResult.Message.Value))
                            {
                                continue;
                            }

                            var rawJson = consumeResult.Message.Value;
                            _logger.LogInformation("Received Kafka message on partition {Partition} at offset {Offset}",
                                consumeResult.Partition.Value, consumeResult.Offset.Value);

                            PostCreatedEventV1? postEvent = null;
                            try
                            {
                                var envelope = JsonSerializer.Deserialize<EventEnvelope<PostCreatedEventV1>>(rawJson, JsonOptions);
                                postEvent = envelope?.Data;
                            }
                            catch
                            {
                                // Fallback: direct payload deserialization
                                postEvent = JsonSerializer.Deserialize<PostCreatedEventV1>(rawJson, JsonOptions);
                            }

                            if (postEvent != null && postEvent.PostId != Guid.Empty)
                            {
                                using var scope = _serviceProvider.CreateScope();
                                var ingestionService = scope.ServiceProvider.GetRequiredService<IRagIngestionService>();

                                await ingestionService.IngestForumPostAsync(
                                    postEvent.PostId,
                                    postEvent.Title,
                                    postEvent.Content,
                                    postEvent.AuthorName,
                                    postEvent.Major,
                                    postEvent.Campus,
                                    stoppingToken);

                                _logger.LogInformation("Successfully ingested post {PostId} '{Title}' into RAG knowledge base.",
                                    postEvent.PostId, postEvent.Title);
                            }
                            else
                            {
                                _logger.LogWarning("Skipped Kafka message: payload does not contain valid PostCreatedEventV1 data.");
                            }

                            consumer.Commit(consumeResult);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogWarning("Kafka consume warning: {Reason}. Retrying in 5 seconds...", ex.Error.Reason);
                            await Task.Delay(5000, stoppingToken);
                        }
                    }

                    consumer.Close();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Kafka Consumer connection error: {Message}. Re-attempting in 10 seconds...", ex.Message);
                    try
                    {
                        await Task.Delay(10000, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }, stoppingToken);
    }
}
