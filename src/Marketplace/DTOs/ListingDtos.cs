using System.ComponentModel.DataAnnotations;
using EcoTrack.MarketplaceService.Models;

namespace EcoTrack.MarketplaceService.DTOs;

public class CreateListingRequestDto
{
    [Required(ErrorMessage = "ValuationId is required.")]
    public Guid ValuationId { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; set; }

    public string? PhotoPath { get; set; }
}

public class UpdateListingRequestDto
{
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string? Title { get; set; }

    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    public decimal? Price { get; set; }

    public string? PhotoPath { get; set; }

    [RegularExpression("^(Available|Reserved|Sold)$", ErrorMessage = "Status must be Available, Reserved, or Sold.")]
    public string? Status { get; set; }
}

public class ListingResponseDto
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

    // Related data
    public ValuationResponseDto? Valuation { get; set; }
    public string? ItemName { get; set; }
    public int? ItemQuantity { get; set; }
}
