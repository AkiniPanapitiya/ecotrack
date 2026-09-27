using System.ComponentModel.DataAnnotations;

namespace EcoTrack.LogisticsService.DTOs;

public class CreateDisposalCertificateRequestDto
{
    [Required(ErrorMessage = "Pickup item ID is required.")]
    public Guid PickupItemId { get; set; }

    [Required(ErrorMessage = "Disposal method is required.")]
    [StringLength(100, ErrorMessage = "Disposal method must not exceed 100 characters.")]
    public string DisposalMethod { get; set; } = string.Empty;
}

public class DisposalCertificateDto
{
    public Guid Id { get; set; }
    public Guid PickupItemId { get; set; }
    public Guid RecyclerId { get; set; }
    public string RecyclerName { get; set; } = string.Empty;
    public string DisposalMethod { get; set; } = string.Empty;
    public DateTime DisposedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Related data
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ItemCondition { get; set; } = string.Empty;
}

public class ItemWithCertificationDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ItemCondition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DisposalCertificateDto? Certificate { get; set; }
}
