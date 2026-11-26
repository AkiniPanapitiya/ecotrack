namespace EcoTrack.LogisticsService.DTOs;

// ECO-26 / E4.2: Drop-off & Dispatch Record
// Validation lives in DropoffService so the API always answers with { message }
// (e.g. "Drop-off date is required.") instead of a ModelState error bag.
public class CreateDropoffRecordRequestDto
{
    public DateTime? DropoffDate { get; set; }

    /// <summary>Optional 24-hour time of the drop-off, "HH:mm".</summary>
    public string? DropoffTime { get; set; }

    public string? ConditionNotes { get; set; }
}

public class DropoffRecordDto
{
    public Guid Id { get; set; }
    public Guid PickupRequestId { get; set; }
    public Guid RecordedBy { get; set; }
    public DateTime DropoffDate { get; set; }
    public string? DropoffTime { get; set; }
    public string? ConditionNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PickupStatus { get; set; } = string.Empty;
}
