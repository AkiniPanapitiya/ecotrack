using EcoTrack.MarketplaceService.DTOs;

namespace EcoTrack.MarketplaceService.Repositories;

public interface IReportRepository
{
    /// fromUtc is inclusive, toUtcExclusive is exclusive. Null means not filtered.
    Task<List<SalesReportCategoryRowDto>> GetSalesByCategoryAsync(
        Guid? recyclerId, string? category, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default);

    Task<List<string>> GetCategoriesAsync(Guid? recyclerId, CancellationToken cancellationToken = default);
}
