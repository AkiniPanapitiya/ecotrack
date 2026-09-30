namespace EcoTrack.MarketplaceService.Models;

public class Valuation
{
    public Guid Id { get; set; }
    public Guid PickupItemId { get; set; }
    public Guid RecyclerId { get; set; }
    public decimal Price { get; set; }
    public string Condition { get; set; } = "Good";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
