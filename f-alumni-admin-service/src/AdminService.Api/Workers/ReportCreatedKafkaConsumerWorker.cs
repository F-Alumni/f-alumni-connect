using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Contracts.Events;
using AdminService.Services.Interfaces;

namespace AdminService.Api.Workers;

/// <summary>
/// Background Kafka consumer worker that listens for forum.report_created.v1 events
/// and persists ModerationReport records into FAlumni_Admin_Db idempotently.
/// </summary>
public class ReportCreatedKafkaConsumerWorker : BackgroundService
{
    private static readonly string[] Topics =
    {
        ReportCreatedEventV1.DefaultTopic
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReportCreatedKafkaConsumerWorker> _logger;

    public ReportCreatedKafkaConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ReportCreatedKafkaConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"]
                               ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            _logger.LogInformation("Kafka:BootstrapServers is not configured; ReportCreatedKafkaConsumerWorker is disabled.");
            return;
        }

        await Task.Yield();
        await EnsureTopicsExistAsync(bootstrapServers, stoppingToken);

        var groupId = _configuration["Kafka:GroupId"] ?? "f-alumni-admin-service-v1";
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            TopicMetadataRefreshIntervalMs = 5000
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topics);
        _logger.LogInformation("AdminService Kafka consumer subscribed to {Topics} on {BootstrapServers}",
            string.Join(", ", Topics), bootstrapServers);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(500));
                if (consumeResult?.Message?.Value is null)
                {
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IReportEventProcessor>();
                await processor.ProcessRawEventAsync(consumeResult.Topic, consumeResult.Message.Value, stoppingToken);

                consumer.Commit(consumeResult);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogWarning("Kafka consume warning ({Reason}); backing off briefly.", ex.Error.Reason);
                await SafeDelayAsync(2000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing report event; backing off briefly.");
                await SafeDelayAsync(2000, stoppingToken);
            }
        }

        try
        {
            consumer.Close();
        }
        catch
        {
            // Ignore close errors on shutdown
        }
    }

    private async Task EnsureTopicsExistAsync(string bootstrapServers, CancellationToken stoppingToken)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = bootstrapServers
            }).Build();

            var specs = Topics.Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = 1,
                ReplicationFactor = 1
            });

            await adminClient.CreateTopicsAsync(specs);
            _logger.LogInformation("Ensured Kafka topics exist: {Topics}", string.Join(", ", Topics));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogDebug("Kafka topics already exist.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not pre-create Kafka topics on {BootstrapServers}; consumer will retry metadata automatically.", bootstrapServers);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }
    }

    private static async Task SafeDelayAsync(int milliseconds, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(milliseconds, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
    }
}
