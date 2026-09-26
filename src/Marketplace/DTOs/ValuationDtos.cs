using EcoTrack.MarketplaceService.Models;

namespace EcoTrack.MarketplaceService.DTOs;

public class CreateValuationRequestDto
{
    public Guid PickupItemId { get; set; }
    public decimal Price { get; set; }
    public string Condition { get; set; } = string.Empty;
}

public class UpdateValuationRequestDto
{
    public decimal Price { get; set; }
    public string Condition { get; set; } = string.Empty;
}

public class ValuationResponseDto
{
    public Guid Id { get; set; }
    public Guid PickupItemId { get; set; }
    public Guid RecyclerId { get; set; }
    public decimal Price { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
