using System.Data;
using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.DTOs;
using MySqlConnector;

namespace EcoTrack.MarketplaceService.Repositories;

public interface IListingRepository
{
    Task<ListingResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ValuationResponseDto?> GetValuationByIdAsync(Guid valuationId, CancellationToken cancellationToken = default);
    Task<bool> CreateAsync(ListingResponseDto listing, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid id, UpdateListingRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ListingResponseDto>> BrowseAsync(string? keyword, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ValuationExistsAsync(Guid valuationId, CancellationToken cancellationToken = default);
    Task<bool> ListingExistsForValuationAsync(Guid valuationId, CancellationToken cancellationToken = default);
}
