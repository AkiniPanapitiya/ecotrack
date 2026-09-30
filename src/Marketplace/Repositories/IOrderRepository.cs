using System.Data;
using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.DTOs;
using MySqlConnector;

namespace EcoTrack.MarketplaceService.Repositories;

public interface IOrderRepository
{
    Task<OrderResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderResponseDto?> CreateOrderAsync(Guid listingId, Guid buyerId, decimal priceAtPurchase, CancellationToken cancellationToken = default);
    Task<List<OrderResponseDto>> GetByBuyerIdAsync(Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetOrderCountByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default);
}
