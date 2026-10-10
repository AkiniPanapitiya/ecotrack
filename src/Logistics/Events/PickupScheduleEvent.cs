namespace EcoTrack.LogisticsService.Events;

public class PickupScheduledEvent
{
    public const string Topic = "logistics.pickup.scheduled";
    public const string DlqTopic = "logistics.pickup.scheduled.dlq";

    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = "PickupScheduled";
    public string PickupRequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string RecyclerId { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public string ScheduledTimeSlot { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}