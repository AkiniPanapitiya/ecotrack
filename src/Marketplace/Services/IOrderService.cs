using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Services;

public interface IOrderService
{
    Task<(bool Success, int StatusCode, string Message, OrderResponseDto? Order)> PlaceOrderAsync(
        Guid listingId, Guid buyerId, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, MyOrdersResponseDto Orders)> GetMyOrdersAsync(
        Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default);
}
