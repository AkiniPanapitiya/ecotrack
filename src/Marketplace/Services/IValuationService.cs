using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Services;

public interface IValuationService
{
    Task<(bool Success, int StatusCode, string Message, ValuationResponseDto? Valuation)> CreateValuationAsync(
        Guid pickupItemId, CreateValuationRequestDto dto, string recyclerId, CancellationToken cancellationToken = default);
    Task<(bool Success, int StatusCode, string Message, ValuationResponseDto? Valuation)> UpdateValuationAsync(
        Guid pickupItemId, UpdateValuationRequestDto dto, CancellationToken cancellationToken = default);
    Task<(bool Success, int StatusCode, string Message, ValuationResponseDto? Valuation)> GetValuationByPickupItemAsync(
        Guid pickupItemId, CancellationToken cancellationToken = default);
    Task<List<ValuationResponseDto>> GetValuationsByRecyclerAsync(
        string recyclerId, CancellationToken cancellationToken = default);
}
