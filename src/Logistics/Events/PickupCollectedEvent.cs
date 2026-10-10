namespace EcoTrack.LogisticsService.Events;

public class PickupCollectedEvent
{
    public const string Topic = "logistics.pickup.collected";
    public const string DlqTopic = "logistics.pickup.collected.dlq";

    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = "PickupCollected";
    public string PickupRequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RecyclerId { get; set; } = string.Empty;
    public decimal EstimatedWeightKg { get; set; }
    public int ItemCount { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}