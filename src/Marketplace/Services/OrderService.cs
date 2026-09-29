using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;

namespace EcoTrack.MarketplaceService.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IListingRepository _listingRepository;

    public OrderService(IOrderRepository orderRepository, IListingRepository listingRepository)
    {
        _orderRepository = orderRepository;
        _listingRepository = listingRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, OrderResponseDto? Order)> PlaceOrderAsync(
        Guid listingId, Guid buyerId, CancellationToken cancellationToken = default)
    {
        // Check listing exists and is Available
        var listing = await _listingRepository.GetByIdAsync(listingId, cancellationToken);
        if (listing == null || listing.IsDeleted == true)
            return (false, 404, "Listing not found.", null);

        if (listing.Status != "Available")
            return (false, 409, "This item is no longer available.", null);

        // Create order + mark listing Reserved in one transaction
        try
        {
            var order = await _orderRepository.CreateOrderAsync(
                listingId, buyerId, listing.Price, cancellationToken);
            if (order == null)
                return (false, 500, "Failed to place order.", null);

            return (true, 201, "Order placed successfully.", order);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no longer available"))
        {
            return (false, 409, "This item is no longer available.", null);
        }
        catch (MySqlConnector.MySqlException ex) when (ex.Number == 1062)
        {
            return (false, 409, "This item has already been ordered.", null);
        }
        catch
        {
            return (false, 500, "Failed to place order. Please try again.", null);
        }
    }

    public async Task<(bool Success, int StatusCode, string Message, MyOrdersResponseDto Orders)> GetMyOrdersAsync(
        Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var orders = await _orderRepository.GetByBuyerIdAsync(buyerId, page, pageSize, cancellationToken);
        var totalCount = await _orderRepository.GetOrderCountByBuyerIdAsync(buyerId, cancellationToken);

        return (
            true,
            200,
            "Orders retrieved.",
            new MyOrdersResponseDto
            {
                Orders = orders,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < totalCount
            }
        );
    }
}
