using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Services;

public interface IListingService
{
    Task<(bool Success, int StatusCode, string Message, ListingResponseDto? Listing)> CreateListingAsync(
        Guid valuationId, CreateListingRequestDto dto, string recyclerId, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, ListingResponseDto? Listing)> UpdateListingAsync(
        Guid id, UpdateListingRequestDto dto, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, ListingResponseDto? Listing)> GetListingByIdAsync(
        Guid id, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, List<ListingResponseDto> Listings, int TotalCount, int Page, int PageSize)> BrowseListingsAsync(
        string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message)> DeleteListingAsync(
        Guid id, CancellationToken cancellationToken = default);
}
