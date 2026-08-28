namespace EcoTrack.IdentityService.DTOs;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Recycler Specific Info (if role == Recycler)
    public RecyclerProfileDto? RecyclerProfile { get; set; }
}

public class RecyclerProfileDto
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    public string FacilityAddress { get; set; } = string.Empty;
    public decimal OperationalCapacityKg { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
}
