using System.ComponentModel.DataAnnotations;

namespace EcoTrack.IdentityService.DTOs;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [RegularExpression("^(User|Recycler|Admin|Driver)$", ErrorMessage = "Role must be User, Recycler, Admin, or Driver.")]
    public string Role { get; set; } = "User";

    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }

    // Recycler specific properties (required if Role == "Recycler")
    public string? CompanyName { get; set; }
    public string? BusinessRegistrationNumber { get; set; }
    public string? FacilityAddress { get; set; }
    public decimal? OperationalCapacityKg { get; set; }
}
