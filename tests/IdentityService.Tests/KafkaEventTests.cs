using System;
using System.Text.Json;
using EcoTrack.IdentityService.Events;
using EcoTrack.LogisticsService.Events;
using EcoTrack.MarketplaceService.Events;
using Xunit;

namespace EcoTrack.IdentityService.Tests;

public class KafkaEventTests
{
    [Fact]
    public void Kafka_Sprint3_Topics_ShouldFollowStandardNamingConventions()
    {
        // Topics must be lowercase, dot-separated: <domain>.<entity>.<action>
        Assert.Equal("recycler.verification.changed", RecyclerVerificationChangedEvent.Topic);
        Assert.Equal("marketplace.order.placed", MarketplaceOrderPlacedEvent.Topic);
        Assert.Equal("ewaste.disposal.certified", EwasteDisposalCertifiedEvent.Topic);
    }

    [Fact]
    public void Kafka_DeadLetterQueue_Topics_ShouldHaveDlqSuffix()
    {
        // DLQ topics must be <topic>.dlq
        Assert.Equal("recycler.verification.changed.dlq", RecyclerVerificationChangedEvent.DlqTopic);
        Assert.Equal("marketplace.order.placed.dlq", MarketplaceOrderPlacedEvent.DlqTopic);
        Assert.Equal("ewaste.disposal.certified.dlq", EwasteDisposalCertifiedEvent.DlqTopic);
    }

    [Fact]
    public void Kafka_DeadLetterEnvelope_ShouldRouteToExpectedDlqTopic()
    {
        // Arrange
        var evt = new RecyclerVerificationChangedEvent
        {
            RecyclerId = "rec-101",
            UserId = "user-202",
            PreviousStatus = "Pending",
            NewStatus = "Approved",
            Remarks = "KYC verified by Admin"
        };

        var envelope = new DeadLetterEnvelope<RecyclerVerificationChangedEvent>
        {
            OriginalTopic = RecyclerVerificationChangedEvent.Topic,
            FailedConsumerGroup = "logistics-recycler-sync",
            ExceptionMessage = "Database lock timeout",
            RetryCount = 3,
            Payload = evt
        };

        // Assert
        Assert.Equal("recycler.verification.changed.dlq", envelope.DlqTopic);
        Assert.Equal(3, envelope.RetryCount);
        Assert.NotNull(envelope.Payload);
        Assert.Equal("Approved", envelope.Payload.NewStatus);
    }

    [Fact]
    public void Kafka_EventSerialization_ShouldPreserveCorrelationAndTimestamps()
    {
        // Arrange
        var orderEvent = new MarketplaceOrderPlacedEvent
        {
            OrderId = "ord-999",
            BuyerId = "buyer-123",
            ListingId = "list-456",
            Quantity = 2,
            TotalPrice = 90000.00m,
            ShippingAddress = "No 12, Galle Road, Colombo"
        };

        // Act
        string json = JsonSerializer.Serialize(orderEvent);
        var deserialized = JsonSerializer.Deserialize<MarketplaceOrderPlacedEvent>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(orderEvent.OrderId, deserialized.OrderId);
        Assert.Equal(orderEvent.CorrelationId, deserialized.CorrelationId);
        Assert.Equal(90000.00m, deserialized.TotalPrice);
    }

    [Fact]
    public void Kafka_EwasteDisposalCertifiedEvent_ShouldHaveValidConsumerGroup()
    {
        // Arrange & Assert
        Assert.False(string.IsNullOrWhiteSpace(EwasteDisposalCertifiedEvent.ConsumerGroup));
        Assert.Equal("logistics-disposal-group", EwasteDisposalCertifiedEvent.ConsumerGroup);
    }
}
