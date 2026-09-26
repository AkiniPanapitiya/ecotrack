namespace EcoTrack.MarketplaceService.Models;

public class Listing
{
    public Guid Id { get; set; }
    public Guid ValuationId { get; set; }
    public Guid RecyclerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? PhotoPath { get; set; }
    public string Status { get; set; } = "Available";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
