using System.ComponentModel.DataAnnotations;

namespace EcoTrack.LogisticsService.DTOs;

public class CreatePickupRequestDto
{
    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; set; } = string.Empty;

    [Range(0.1, 10000.0, ErrorMessage = "Estimated weight must be between 0.1 kg and 10,000 kg.")]
    public decimal EstimatedWeightKg { get; set; }

    [Required(ErrorMessage = "Pickup address is required.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "Address must be at least 5 characters.")]
    public string PickupAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contact phone is required.")]
    [Phone(ErrorMessage = "Invalid phone number format.")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Preferred date is required.")]
    public DateTime PreferredDate { get; set; }

    [Required(ErrorMessage = "Time slot is required.")]
    public string TimeSlot { get; set; } = "Morning (09:00 - 12:00)";

    public string? SpecialInstructions { get; set; }

    public List<CreatePickupItemDto>? Items { get; set; }
}

public class CreatePickupItemDto
{
    [Required(ErrorMessage = "Item name is required.")]
    public string ItemName { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 1;

    public string ItemCondition { get; set; } = "Used";

    public decimal EstimatedWeightKg { get; set; }
}

public class PickupRequestDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? RecyclerId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal EstimatedWeightKg { get; set; }
    public string PickupAddress { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string? ScheduledTimeSlot { get; set; }
    public string? SpecialInstructions { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PickupItemDto> Items { get; set; } = new();
}

public class PickupItemDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ItemCondition { get; set; } = "Used";
    public decimal EstimatedWeightKg { get; set; }
}

public class ConfirmScheduleRequestDto
{
    [Required(ErrorMessage = "Recycler ID is required.")]
    public Guid RecyclerId { get; set; }

    [Required(ErrorMessage = "Scheduled date is required.")]
    public DateTime ScheduledDate { get; set; }

    [Required(ErrorMessage = "Scheduled time slot is required.")]
    public string ScheduledTimeSlot { get; set; } = string.Empty;
}