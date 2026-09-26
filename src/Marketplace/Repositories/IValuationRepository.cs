using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Repositories;

public interface IValuationRepository
{
    Task<ValuationResponseDto?> GetByPickupItemIdAsync(Guid pickupItemId, CancellationToken cancellationToken = default);
    Task<ValuationResponseDto?> CreateAsync(ValuationResponseDto valuation, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateValuationRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> PickupItemExistsAsync(Guid pickupItemId, CancellationToken cancellationToken = default);
}
