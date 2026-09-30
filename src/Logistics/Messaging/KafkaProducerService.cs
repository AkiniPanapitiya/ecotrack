using System.Text.Json;
using Confluent.Kafka;

namespace EcoTrack.LogisticsService.Messaging;

public interface IEventProducer
{
    Task PublishAsync(string topic, string key, object payload, CancellationToken ct = default);
}

public class KafkaProducerService : IEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration config, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All   // NFR05: broker must fully acknowledge → no message loss
        };
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync(string topic, string key, object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
        var result = await _producer.ProduceAsync(
            topic, new Message<string, string> { Key = key, Value = json }, ct);
        _logger.LogInformation("Published to {Topic} @ offset {Offset}", topic, result.Offset);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}