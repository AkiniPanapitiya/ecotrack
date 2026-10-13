namespace EcoTrack.LogisticsService.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}