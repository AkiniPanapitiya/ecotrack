using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;
using EcoTrack.MarketplaceService.Services;
using Moq;
using Xunit;

namespace EcoTrack.MarketplaceService.Tests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly OrderService _orderService;
    private readonly CancellationToken _ct = CancellationToken.None;

    public OrderServiceTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _listingRepoMock = new Mock<IListingRepository>();
        _orderService = new OrderService(_orderRepoMock.Object, _listingRepoMock.Object);
    }

    private static ListingResponseDto MakeAvailableListing(Guid? id = null)
    {
        var listingId = id ?? Guid.NewGuid();
        return new ListingResponseDto
        {
            Id = listingId,
            ValuationId = Guid.NewGuid(),
            RecyclerId = Guid.NewGuid(),
            Title = "Test Laptop",
            Description = "Test",
            Price = 15000m,
            PhotoPath = null,
            Status = "Available",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Valuation = null,
            ItemName = null,
            ItemQuantity = null
        };
    }

    private static OrderResponseDto MakeOrder(Guid listingId, Guid buyerId, decimal priceAtPurchase, string status = "Placed")
    {
        return new OrderResponseDto
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            BuyerId = buyerId,
            PriceAtPurchase = priceAtPurchase,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Listing = new ListingBriefDto
            {
                Id = listingId,
                Title = "Test Laptop",
                Price = priceAtPurchase,
                Status = "Reserved",
                PhotoPath = null
            }
        };
    }

    // ── PlaceOrderAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_ValidAvailableListing_Returns201AndOrderCreated()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var listing = MakeAvailableListing(listingId);
        var order = MakeOrder(listingId, buyerId, 15000m);

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);
        _orderRepoMock.Setup(r => r.CreateOrderAsync(listingId, buyerId, 15000m, _ct)).ReturnsAsync(order);

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Order placed successfully.", message);
        Assert.NotNull(result);
        Assert.Equal(listingId, result.ListingId);
        Assert.Equal(buyerId, result.BuyerId);
        Assert.Equal(15000m, result.PriceAtPurchase);
        Assert.Equal("Placed", result.Status);
        _orderRepoMock.Verify(r => r.CreateOrderAsync(listingId, buyerId, 15000m, _ct), Times.Once);
    }

    [Fact]
    public async Task PlaceOrder_ReservedListing_Returns409AndNoOrderCreated()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var listing = MakeAvailableListing(listingId);
        listing.Status = "Reserved";

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("This item is no longer available.", message);
        Assert.Null(result);
        _orderRepoMock.Verify(r => r.CreateOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), _ct), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_SoldListing_Returns409AndNoOrderCreated()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var listing = MakeAvailableListing(listingId);
        listing.Status = "Sold";

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("This item is no longer available.", message);
        Assert.Null(result);
        _orderRepoMock.Verify(r => r.CreateOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), _ct), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_ListedNotFound_Returns404()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync((ListingResponseDto?)null);

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("Listing not found", message);
        Assert.Null(result);
        _orderRepoMock.Verify(r => r.CreateOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), _ct), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_DeletedListing_Returns404()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var listing = MakeAvailableListing(listingId);
        listing.IsDeleted = true;

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("Listing not found", message);
        Assert.Null(result);
        _orderRepoMock.Verify(r => r.CreateOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>(), _ct), Times.Never);
    }

    [Fact]
    public async Task PlaceOrder_StoresCorrectBuyerItemAndPrice()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var listingPrice = 25000m;
        var listing = MakeAvailableListing(listingId);
        listing.Price = listingPrice;
        var order = MakeOrder(listingId, buyerId, listingPrice);

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);
        _orderRepoMock.Setup(r => r.CreateOrderAsync(listingId, buyerId, listingPrice, _ct)).ReturnsAsync(order);

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.True(success);
        Assert.Equal(buyerId, result.BuyerId);
        Assert.Equal(listingId, result.ListingId);
        Assert.Equal(listingPrice, result.PriceAtPurchase);
    }

    [Fact]
    public async Task PlaceOrder_RepoThrows_500AndRollback()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var listing = MakeAvailableListing(listingId);

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);
        _orderRepoMock.Setup(r => r.CreateOrderAsync(listingId, buyerId, 15000m, _ct))
            .ThrowsAsync(new InvalidOperationException("Listing is no longer available."));

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("This item is no longer available.", message);
        Assert.Null(result);
    }

    [Fact]
    public async Task PlaceOrder_RepoGenericFailure_Returns500()
    {
        var listingId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var listing = MakeAvailableListing(listingId);

        _listingRepoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);
        _orderRepoMock.Setup(r => r.CreateOrderAsync(listingId, buyerId, 15000m, _ct))
            .ThrowsAsync(new Exception("DB connection failed"));

        var (success, statusCode, message, result) = await _orderService.PlaceOrderAsync(listingId, buyerId, _ct);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Contains("Failed to place order", message);
        Assert.Null(result);
    }

    // ── GetMyOrdersAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyOrders_ReturnsOnlyCallersOrders()
    {
        var buyerId = Guid.NewGuid();
        var otherBuyerId = Guid.NewGuid();

        var myOrder = MakeOrder(Guid.NewGuid(), buyerId, 15000m, "Placed");
        var otherOrder = MakeOrder(Guid.NewGuid(), otherBuyerId, 20000m, "Completed");

        var myOrdersOnly = new List<OrderResponseDto> { myOrder };

        _orderRepoMock.Setup(r => r.GetByBuyerIdAsync(buyerId, 1, 10, _ct)).ReturnsAsync(myOrdersOnly);
        _orderRepoMock.Setup(r => r.GetOrderCountByBuyerIdAsync(buyerId, _ct)).ReturnsAsync(1);

        var (success, statusCode, message, result) = await _orderService.GetMyOrdersAsync(buyerId, 1, 10, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Orders retrieved.", message);
        Assert.NotNull(result);
        Assert.Equal(1, result.Orders.Count);
        Assert.All(result.Orders, o => Assert.Equal(buyerId, o.BuyerId));
    }

    [Fact]
    public async Task GetMyOrders_EmptyList_WhenNoOrders()
    {
        var buyerId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByBuyerIdAsync(buyerId, 1, 10, _ct)).ReturnsAsync(new List<OrderResponseDto>());
        _orderRepoMock.Setup(r => r.GetOrderCountByBuyerIdAsync(buyerId, _ct)).ReturnsAsync(0);

        var (success, statusCode, message, result) = await _orderService.GetMyOrdersAsync(buyerId, 1, 10, _ct);

        Assert.True(success);
        Assert.Empty(result.Orders);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetMyOrders_RespectsPagination()
    {
        var buyerId = Guid.NewGuid();
        var orders = new List<OrderResponseDto>
        {
            MakeOrder(Guid.NewGuid(), buyerId, 15000m),
            MakeOrder(Guid.NewGuid(), buyerId, 20000m)
        };

        _orderRepoMock.Setup(r => r.GetByBuyerIdAsync(buyerId, 2, 5, _ct)).ReturnsAsync(orders);
        _orderRepoMock.Setup(r => r.GetOrderCountByBuyerIdAsync(buyerId, _ct)).ReturnsAsync(12);

        var (success, statusCode, message, result) = await _orderService.GetMyOrdersAsync(buyerId, 2, 5, _ct);

        Assert.Equal(5, result.PageSize);
        Assert.Equal(2, result.Page);
        Assert.Equal(12, result.TotalCount);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task GetMyOrders_ClampsPageSizeTo50()
    {
        var buyerId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByBuyerIdAsync(buyerId, 1, 50, _ct)).ReturnsAsync(new List<OrderResponseDto>());
        _orderRepoMock.Setup(r => r.GetOrderCountByBuyerIdAsync(buyerId, _ct)).ReturnsAsync(0);

        _ = await _orderService.GetMyOrdersAsync(buyerId, 1, 100, _ct);

        _orderRepoMock.Verify(r => r.GetByBuyerIdAsync(buyerId, 1, 50, _ct), Times.Once);
        _orderRepoMock.Verify(r => r.GetByBuyerIdAsync(buyerId, 1, 100, _ct), Times.Never);
    }

    [Fact]
    public async Task GetMyOrders_ClampsPageTo1()
    {
        var buyerId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByBuyerIdAsync(buyerId, 1, 10, _ct)).ReturnsAsync(new List<OrderResponseDto>());
        _orderRepoMock.Setup(r => r.GetOrderCountByBuyerIdAsync(buyerId, _ct)).ReturnsAsync(0);

        _ = await _orderService.GetMyOrdersAsync(buyerId, 0, 10, _ct);

        _orderRepoMock.Verify(r => r.GetByBuyerIdAsync(buyerId, 1, 10, _ct), Times.Once);
        _orderRepoMock.Verify(r => r.GetByBuyerIdAsync(buyerId, 0, 10, _ct), Times.Never);
    }
}
