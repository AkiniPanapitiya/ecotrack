namespace EcoTrack.LogisticsService.Models;

/// <summary>
/// ECO-26 / E4.2: Proof that a pickup was collected and dropped off by the
/// assigned driver/recycler. One record per pickup request.
/// </summary>
public class DropoffRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PickupRequestId { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTime DropoffDate { get; set; }
    public TimeSpan? DropoffTime { get; set; }
    public string? ConditionNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
