using System;

namespace EcoTrack.LogisticsService.Events;

public class EwasteDisposalCertifiedEvent
{
    public const string Topic = "ewaste.disposal.certified";
    public const string DlqTopic = "ewaste.disposal.certified.dlq";
    public const string ConsumerGroup = "logistics-disposal-group";

    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string CertificateId { get; set; } = string.Empty;
    public string PickupRequestId { get; set; } = string.Empty;
    public string RecyclerId { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public string DisposalMethod { get; set; } = string.Empty;
    public string CertificateNumber { get; set; } = string.Empty;
    public DateTime CertifiedAt { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
