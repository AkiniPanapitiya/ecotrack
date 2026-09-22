using System;

namespace EcoTrack.MarketplaceService.Events;

public class MarketplaceOrderPlacedEvent
{
    public const string Topic = "marketplace.order.placed";
    public const string DlqTopic = "marketplace.order.placed.dlq";
    public const string ConsumerGroup = "marketplace-order-group";

    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string OrderId { get; set; } = string.Empty;
    public string BuyerId { get; set; } = string.Empty;
    public string ListingId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}
