namespace EcoTrack.LogisticsService.Models;

public class PickupRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal EstimatedWeightKg { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string? SpecialInstructions { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<PickupItem> Items { get; set; } = new();
}

public class PickupItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PickupRequestId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string ItemCondition { get; set; } = "Used";
    public decimal EstimatedWeightKg { get; set; }
}
