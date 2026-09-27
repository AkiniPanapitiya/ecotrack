using System.ComponentModel.DataAnnotations;

namespace EcoTrack.MarketplaceService.DTOs;

public class CreateOrderRequestDto
{
    [Required(ErrorMessage = "ListingId is required.")]
    public Guid ListingId { get; set; }
}

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public Guid ListingId { get; set; }
    public Guid BuyerId { get; set; }
    public decimal PriceAtPurchase { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Related data
    public ListingBriefDto? Listing { get; set; }
}

public class ListingBriefDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
}

public class MyOrdersResponseDto
{
    public List<OrderResponseDto> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
