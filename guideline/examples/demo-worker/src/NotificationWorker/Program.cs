using Confluent.Kafka;
using NotificationWorker.Abstractions;
using NotificationWorker.Services;
using NotificationWorker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Register dependencies
builder.Services.AddSingleton<IEmailSender, MockEmailSender>();
builder.Services.AddSingleton<IProcessedEventTracker, InMemoryProcessedEventTracker>();
builder.Services.AddSingleton<NotificationEventProcessor>();

// Register Kafka Consumer
builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var bootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
    var groupId = config["Kafka:GroupId"] ?? "notification-worker.user-registered-group";

    var consumerConfig = new ConsumerConfig
    {
        BootstrapServers = bootstrapServers,
        GroupId = groupId,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false, // Manual commit after processing
        EnablePartitionEof = false
    };

    return new ConsumerBuilder<string, string>(consumerConfig).Build();
});

// Register Hosted Background Service
builder.Services.AddHostedService<NotificationBackgroundService>();

var host = builder.Build();
host.Run();
