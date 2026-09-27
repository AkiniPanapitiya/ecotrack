using System;

namespace EcoTrack.IdentityService.Events;

public class DeadLetterEnvelope<T>
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public string OriginalTopic { get; set; } = string.Empty;
    public string DlqTopic => $"{OriginalTopic}.dlq";
    public string FailedConsumerGroup { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public int RetryCount { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
    public T? Payload { get; set; }
}
