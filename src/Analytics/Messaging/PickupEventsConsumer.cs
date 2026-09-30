using Confluent.Kafka;

namespace AnalyticsService.Messaging;

public class PickupEventsConsumer : BackgroundService
{
    private readonly ILogger<PickupEventsConsumer> _logger;
    private readonly IConfiguration _config;

    public PickupEventsConsumer(ILogger<PickupEventsConsumer> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
    }

        private void ConsumeLoop(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "analytics-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = true
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe("pickup.lifecycle.events");
        _logger.LogInformation("Analytics consumer subscribed to pickup.lifecycle.events");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                _logger.LogInformation("CONSUMED from {Topic}: {Value}",
                    result.Topic, result.Message.Value);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogWarning("Consume error: {Reason}. Retrying...", ex.Error.Reason);
                Thread.Sleep(2000);
            }
        }

        consumer.Close();
    }
}