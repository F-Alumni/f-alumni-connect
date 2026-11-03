using Confluent.Kafka;
using KafkaDemo.Contracts;

namespace KafkaConsumer;

public class Program
{
    public static int Main(string[] args)
    {
        var bootstrapServers = args.Length > 0 ? args[0] : (Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092");
        var topic = UserRegisteredEventV1.DefaultTopic;
        var consumerGroup = "notification-worker.user-registered-group";

        Console.WriteLine("=================================================");
        Console.WriteLine("  🎓 FPT Alumni Connect — Kafka Consumer Demo");
        Console.WriteLine("=================================================");
        Console.WriteLine($"Bootstrap Servers: {bootstrapServers}");
        Console.WriteLine($"Subscribed Topic:  {topic}");
        Console.WriteLine($"Consumer Group:    {consumerGroup}");

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // At-least-once processing: commit manually AFTER processing
            EnablePartitionEof = false
        };

        var processor = new EventProcessor();
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine("\n[!] Shutting down consumer gracefully...");
            cts.Cancel();
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(topic);
            Console.WriteLine("\n[*] Waiting for incoming Kafka messages (Press Ctrl+C to exit)...");

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(cts.Token);
                    if (consumeResult?.Message is null) continue;

                    var rawJson = consumeResult.Message.Value;
                    var key = consumeResult.Message.Key;
                    var partition = consumeResult.Partition.Value;
                    var offset = consumeResult.Offset.Value;

                    Console.WriteLine($"\n--> Received Message [Partition: {partition}, Offset: {offset}, Key: {key}]");

                    var result = processor.ProcessRawMessage(rawJson, out var envelope, out var error);

                    switch (result)
                    {
                        case ProcessResult.Success:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"    [✓] Processed Event: {envelope!.EventType} (EventId: {envelope.EventId})");
                            Console.WriteLine($"        Recipient: {envelope.Data.FullName} <{envelope.Data.Email}>");
                            Console.WriteLine($"        Verification OTP: {envelope.Data.VerificationCode}");
                            Console.ResetColor();

                            // Commit offset after business side-effects succeed
                            consumer.Commit(consumeResult);
                            Console.WriteLine($"    [Offset {offset} committed successfully]");
                            break;

                        case ProcessResult.Duplicate:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"    [⚠] Duplicate event skipped: EventId {envelope!.EventId} already processed (Idempotent).");
                            Console.ResetColor();
                            // Still commit to advance past the duplicate message
                            consumer.Commit(consumeResult);
                            break;

                        case ProcessResult.Malformed:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"    [✗] Malformed message rejected: {error}");
                            Console.WriteLine("    [Action] Routed to Dead-Letter Topic (DLT) or skipped to prevent consumer crash.");
                            Console.ResetColor();
                            consumer.Commit(consumeResult);
                            break;
                    }
                }
                catch (ConsumeException ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\n[!] Broker consume exception: {ex.Error.Reason}");
                    Console.ResetColor();
                    break;
                }
            }

            consumer.Close();
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[*] Consumer stopped cleanly.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[!] Consumer fatal error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}
