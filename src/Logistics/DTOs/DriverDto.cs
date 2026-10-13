using System.ComponentModel.DataAnnotations;

namespace EcoTrack.LogisticsService.DTOs;

public class CreateDriverRequestDto
{
    [Required(ErrorMessage = "Driver name is required.")]
    [StringLength(150, ErrorMessage = "Driver name must not exceed 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\+?[0-9]{9,15}$", ErrorMessage = "Phone number must be 9 to 15 digits.")]
    public string Phone { get; set; } = string.Empty;

    [StringLength(30, ErrorMessage = "Vehicle number must not exceed 30 characters.")]
    public string? VehicleNumber { get; set; }
}

public class AssignDriverRequestDto
{
    [Required(ErrorMessage = "Driver ID is required.")]
    public Guid DriverId { get; set; }
}

public class DriverDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
}

public class PickupDriverDto
{
    public Guid PickupId { get; set; }
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string DriverPhone { get; set; } = string.Empty;
    public string? VehicleNumber { get; set; }
}