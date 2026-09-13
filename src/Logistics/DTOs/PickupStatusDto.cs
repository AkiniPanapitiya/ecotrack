namespace EcoTrack.LogisticsService.DTOs;

public class PickupStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}