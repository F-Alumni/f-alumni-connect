using Confluent.Kafka;
using Contracts.Events;
using NotificationWorker.Services;

namespace NotificationWorker.Workers;

/// <summary>
/// .NET BackgroundService that continuously consumes user registration events from Apache Kafka
/// and coordinates email dispatching via NotificationEventProcessor.
/// </summary>
public class NotificationBackgroundService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly NotificationEventProcessor _processor;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly string _topic;

    public NotificationBackgroundService(
        IConsumer<string, string> consumer,
        NotificationEventProcessor processor,
        IConfiguration configuration,
        ILogger<NotificationBackgroundService> logger)
    {
        _consumer = consumer;
        _processor = processor;
        _logger = logger;
        _topic = configuration["Kafka:Topic"] ?? UserRegisteredEventV1.DefaultTopic;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Notification Background Worker started. Listening for events on Kafka topic '{Topic}'...", _topic);

        try
        {
            _consumer.Subscribe(_topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to Kafka topic '{Topic}'.", _topic);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Blocking consume with cancellation support
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult?.Message is null) continue;

                var rawJson = consumeResult.Message.Value;
                var offset = consumeResult.Offset.Value;
                var partition = consumeResult.Partition.Value;

                _logger.LogInformation(
                    "Received Kafka message from Partition {Partition} at Offset {Offset}",
                    partition, offset);

                var status = await _processor.ProcessMessageAsync(rawJson, stoppingToken);

                // Commit offset strategy:
                // - Success: side effect finished -> commit
                // - Duplicate: already handled -> commit to advance past duplicate
                // - Malformed: poison pill -> commit to avoid infinite crash loop
                // - Failed: transient email error -> DO NOT commit, allow Kafka to retry
                if (status != EventProcessingStatus.Failed)
                {
                    _consumer.Commit(consumeResult);
                    _logger.LogDebug("Offset {Offset} committed successfully.", offset);
                }
                else
                {
                    _logger.LogWarning("Offset {Offset} NOT committed due to transient processing failure. Will retry.", offset);
                    await Task.Delay(2000, stoppingToken); // Short backoff before retrying
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Cancellation requested. Stopping consumer loop...");
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogWarning("Kafka consume exception ({Reason}). Retrying in 3 seconds...", ex.Error.Reason);
                try
                {
                    await Task.Delay(3000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Notification Worker loop. Retrying in 3 seconds...");
                try
                {
                    await Task.Delay(3000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        try
        {
            _consumer.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing Kafka consumer.");
        }

        _logger.LogInformation("Notification Background Worker stopped cleanly.");
    }
}
