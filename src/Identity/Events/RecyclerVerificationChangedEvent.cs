using System;

namespace EcoTrack.IdentityService.Events;

public class RecyclerVerificationChangedEvent
{
    public const string Topic = "recycler.verification.changed";
    public const string DlqTopic = "recycler.verification.changed.dlq";
    public const string ConsumerGroup = "identity-recycler-group";

    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string RecyclerId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
