namespace EcoTrack.IdentityService.Models;

public class RecyclerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string FacilityAddress { get; set; } = string.Empty;
    public decimal OperationalCapacityKg { get; set; }
    public string VerificationStatus { get; set; } = "Pending"; // "Pending", "Approved", "Rejected", "Suspended"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
