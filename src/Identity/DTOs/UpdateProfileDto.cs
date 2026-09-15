using System.ComponentModel.DataAnnotations;

namespace EcoTrack.IdentityService.DTOs;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be exactly 10 digits.")]    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    // Recycler Specific updates
    public string? CompanyName { get; set; }
    public string? FacilityAddress { get; set; }
    public decimal? OperationalCapacityKg { get; set; }
}
