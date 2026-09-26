using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;

namespace EcoTrack.MarketplaceService.Services;

public class ListingService : IListingService
{
    private readonly IListingRepository _listingRepository;

    public ListingService(IListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, ListingResponseDto? Listing)> CreateListingAsync(
        Guid valuationId, CreateListingRequestDto dto, string recyclerId, CancellationToken cancellationToken = default)
    {
        // Validation: title is required
        if (string.IsNullOrWhiteSpace(dto.Title))
            return (false, 400, "Title is required.", null);

        // Validation: price must be > 0
        if (dto.Price <= 0)
            return (false, 400, "Price must be greater than 0.", null);

        // Check: valuation must exist
        var valuationExists = await _listingRepository.ValuationExistsAsync(valuationId, cancellationToken);
        if (!valuationExists)
            return (false, 404, "No valuation found for the specified item.", null);

        // Check: listing already exists for this valuation
        var existing = await _listingRepository.ListingExistsForValuationAsync(valuationId, cancellationToken);
        if (existing)
            return (false, 409, "A listing already exists for this valuation. Use PUT to update it.", null);

        // Get valuation to build related data
        var listingWithValuation = await _listingRepository.GetByValuationIdAsync(valuationId, cancellationToken);
        if (listingWithValuation == null)
            return (false, 404, "Valuation not found.", null);

        var listing = new ListingResponseDto
        {
            Id = Guid.NewGuid(),
            ValuationId = valuationId,
            RecyclerId = Guid.Parse(recyclerId),
            Title = dto.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            Price = dto.Price,
            PhotoPath = string.IsNullOrWhiteSpace(dto.PhotoPath) ? null : dto.PhotoPath.Trim(),
            Status = "Available",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Valuation = listingWithValuation.Valuation
        };

        var created = await _listingRepository.CreateAsync(listing, cancellationToken);
        if (!created)
            return (false, 500, "Failed to create listing.", null);

        return (true, 201, "Listing created successfully.", listing);
    }

    public async Task<(bool Success, int StatusCode, string Message, ListingResponseDto? Listing)> UpdateListingAsync(
        Guid id, UpdateListingRequestDto dto, CancellationToken cancellationToken = default)
    {
        // Get existing listing
        var existing = await _listingRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return (false, 404, "Listing not found.", null);

        // Build update DTO with only provided fields
        var updateDto = new UpdateListingRequestDto();

        if (dto.Title != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return (false, 400, "Title is required.", null);
            updateDto.Title = dto.Title.Trim();
        }

        if (dto.Price.HasValue)
        {
            if (dto.Price.Value <= 0)
                return (false, 400, "Price must be greater than 0.", null);
            updateDto.Price = dto.Price.Value;
        }

        if (dto.Description != null)
            updateDto.Description = dto.Description.Trim();

        if (dto.PhotoPath != null)
            updateDto.PhotoPath = dto.PhotoPath.Trim();

        if (dto.Status != null)
            updateDto.Status = dto.Status;

        var updated = await _listingRepository.UpdateAsync(id, updateDto, cancellationToken);
        if (!updated)
            return (false, 500, "Failed to update listing.", null);

        // Fetch updated listing
        var updatedListing = await _listingRepository.GetByIdAsync(id, cancellationToken);
        return (true, 200, "Listing updated successfully.", updatedListing);
    }

    public async Task<(bool Success, int StatusCode, string Message, ListingResponseDto? Listing)> GetListingByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _listingRepository.GetByIdAsync(id, cancellationToken);
        if (listing == null)
            return (false, 404, "Listing not found.", null);
        return (true, 200, "Listing retrieved.", listing);
    }

    public async Task<(bool Success, int StatusCode, string Message, List<ListingResponseDto> Listings, int TotalCount, int Page, int PageSize)> BrowseListingsAsync(
        string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var listings = await _listingRepository.BrowseAsync(keyword, page, pageSize, cancellationToken);
        return (true, 200, "Listings retrieved.", listings, listings.Count, page, pageSize);
    }

    public async Task<(bool Success, int StatusCode, string Message)> DeleteListingAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await _listingRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
            return (false, 404, "Listing not found.");
        return (true, 200, "Listing removed successfully.");
    }
}
