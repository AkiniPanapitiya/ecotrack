namespace EcoTrack.LogisticsService.Models;

public class Driver
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecyclerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}