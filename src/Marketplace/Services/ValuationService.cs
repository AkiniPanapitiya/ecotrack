using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;

namespace EcoTrack.MarketplaceService.Services;

public class ValuationService : IValuationService
{
    private readonly IValuationRepository _valuationRepository;
    private static readonly string[] ValidConditions = { "Good", "Fair", "Poor" };

    public ValuationService(IValuationRepository valuationRepository)
    {
        _valuationRepository = valuationRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, ValuationResponseDto? Valuation)> CreateValuationAsync(
        Guid pickupItemId, CreateValuationRequestDto dto, string recyclerId, CancellationToken cancellationToken = default)
    {
        // Validation: price is required
        if (dto.Price == 0)
            return (false, 400, "Price is required.", null);

        // Validation: price must be > 0
        if (dto.Price < 0)
            return (false, 400, "Price must be greater than 0.", null);

        // Validation: condition must be valid
        if (!ValidConditions.Contains(dto.Condition))
            return (false, 400, "Condition must be Good, Fair, or Poor.", null);

        // Check if pickup item exists
        var itemExists = await _valuationRepository.PickupItemExistsAsync(pickupItemId, cancellationToken);
        if (!itemExists)
            return (false, 404, "Pickup item not found.", null);

        // Check if valuation already exists for this item
        var existing = await _valuationRepository.GetByPickupItemIdAsync(pickupItemId, cancellationToken);
        if (existing != null)
            return (false, 409, "This item has already been valued. Please update the existing valuation instead.", null);

        var valuation = new ValuationResponseDto
        {
            Id = Guid.NewGuid(),
            PickupItemId = pickupItemId,
            RecyclerId = Guid.Parse(recyclerId),
            Price = dto.Price,
            Condition = dto.Condition.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _valuationRepository.CreateAsync(valuation, cancellationToken);
        if (created == null)
            return (false, 500, "Failed to create valuation.", null);

        return (true, 201, "Valuation created successfully.", valuation);
    }

    public async Task<(bool Success, int StatusCode, string Message, ValuationResponseDto? Valuation)> UpdateValuationAsync(
        Guid pickupItemId, UpdateValuationRequestDto dto, CancellationToken cancellationToken = default)
    {
        // Validation: price is required
        if (dto.Price == 0)
            return (false, 400, "Price is required.", null);

        // Validation: price must be > 0
        if (dto.Price < 0)
            return (false, 400, "Price must be greater than 0.", null);

        // Validation: condition must be valid
        if (!ValidConditions.Contains(dto.Condition))
            return (false, 400, "Condition must be Good, Fair, or Poor.", null);

        // Find existing valuation
        var existing = await _valuationRepository.GetByPickupItemIdAsync(pickupItemId, cancellationToken);
        if (existing == null)
            return (false, 404, "No valuation found for this item.", null);

        var updated = await _valuationRepository.UpdateAsync(existing.Id, dto, cancellationToken);
        if (!updated)
            return (false, 500, "Failed to update valuation.", null);

        existing.Price = dto.Price;
        existing.Condition = dto.Condition.Trim();
        existing.UpdatedAt = DateTime.UtcNow;

        return (true, 200, "Valuation updated successfully.", existing);
    }

    public async Task<(bool Success, int StatusCode, string Message, ValuationResponseDto? Valuation)> GetValuationByPickupItemAsync(
        Guid pickupItemId, CancellationToken cancellationToken = default)
    {
        var valuation = await _valuationRepository.GetByPickupItemIdAsync(pickupItemId, cancellationToken);
        if (valuation == null)
            return (false, 404, "No valuation found for this item.", null);
        return (true, 200, "Valuation retrieved.", valuation);
    }

    public async Task<List<ValuationResponseDto>> GetValuationsByRecyclerAsync(
        string recyclerId, CancellationToken cancellationToken = default)
    {
        return await _valuationRepository.GetByRecyclerIdAsync(recyclerId, cancellationToken);
    }
}
