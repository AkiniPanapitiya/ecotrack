using System.ComponentModel.DataAnnotations;

namespace EcoTrack.MarketplaceService.DTOs;

public class UpdatePaymentStatusRequestDto
{
    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = string.Empty;
}

public class PaymentRecordResponseDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid RecyclerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Related data
    public string OrderStatus { get; set; } = string.Empty;
    public string? ListingTitle { get; set; }
}

public class PaymentListResponseDto
{
    public List<PaymentRecordResponseDto> Payments { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasMore { get; set; }
}
