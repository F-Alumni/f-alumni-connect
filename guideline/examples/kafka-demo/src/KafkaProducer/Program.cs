using System.Text.Json;
using Confluent.Kafka;
using KafkaDemo.Contracts;

namespace KafkaProducer;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var bootstrapServers = args.Length > 0 ? args[0] : (Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092");
        var topic = UserRegisteredEventV1.DefaultTopic;

        Console.WriteLine("=================================================");
        Console.WriteLine("  🎓 FPT Alumni Connect — Kafka Producer Demo");
        Console.WriteLine("=================================================");
        Console.WriteLine($"Bootstrap Servers: {bootstrapServers}");
        Console.WriteLine($"Target Topic:      {topic}");

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 5000
        };

        var sampleUserId = Guid.NewGuid();
        var sampleEventData = new UserRegisteredEventV1(
            userId: sampleUserId,
            email: "student2026@fpt.edu.vn",
            fullName: "Nguyen Van A",
            verificationCode: "827194"
        );

        var envelope = new EventEnvelope<UserRegisteredEventV1>(
            eventType: UserRegisteredEventV1.EventTypeName,
            version: UserRegisteredEventV1.CurrentVersion,
            producer: "f-alumni-user-service",
            data: sampleEventData
        );

        var jsonPayload = JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });
        var partitionKey = sampleUserId.ToString();

        Console.WriteLine($"\n[1] Prepared EventEnvelope (Partition Key = UserId: {partitionKey}):");
        Console.WriteLine(jsonPayload);

        try
        {
            using var producer = new ProducerBuilder<string, string>(config).Build();
            Console.WriteLine($"\n[2] Publishing event to topic '{topic}'...");

            var deliveryResult = await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = partitionKey,
                Value = jsonPayload
            });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[3] Delivery Report: SUCCESS!");
            Console.WriteLine($"    Topic:     {deliveryResult.Topic}");
            Console.WriteLine($"    Partition: {deliveryResult.Partition.Value}");
            Console.WriteLine($"    Offset:    {deliveryResult.Offset.Value}");
            Console.WriteLine($"    Timestamp: {deliveryResult.Timestamp.UtcDateTime:O}");
            Console.ResetColor();
            return 0;
        }
        catch (ProduceException<string, string> ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[!] Kafka broker not reachable ({bootstrapServers}): {ex.Error.Reason}");
            Console.WriteLine("    Note: Start Kafka broker on port 9092 to see live network delivery.");
            Console.ResetColor();
            return 1;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[!] Error publishing event: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}
