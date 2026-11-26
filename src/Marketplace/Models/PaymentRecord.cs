namespace EcoTrack.MarketplaceService.Models;

public class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid RecyclerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// Allowed payment record statuses (record only, no real payment gateway).
/// Flow: Pending -> Paid -> Refunded
public static class PaymentStatus
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Refunded = "Refunded";

    public static readonly string[] All = { Pending, Paid, Refunded };
}
