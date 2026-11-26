using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.DTOs;
using MySqlConnector;

namespace EcoTrack.MarketplaceService.Repositories;

/// Sales, Inventory & Revenue report queries (Story 21, E6.3).
/// Reads the vw_SalesReportItems view from 03_init_marketplace_db.sql.
public class ReportRepository : IReportRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    // The date range applies to sales and revenue (by order date).
    // Inventory is the current listing count, so it ignores the date range.
    private const string InRange = "OrderId IS NOT NULL AND (@From IS NULL OR OrderedAt >= @From) AND (@To IS NULL OR OrderedAt < @To)";
    private const string IsSale = InRange + " AND OrderStatus <> 'Cancelled'";
    private const string Filter = "(@RecyclerId IS NULL OR RecyclerId = @RecyclerId) AND (@Category IS NULL OR Category = @Category)";

    private const string SalesByCategorySql = @"
            SELECT Category,
                   SUM(CASE WHEN " + IsSale + @" THEN 1 ELSE 0 END) AS TotalSales,
                   SUM(CASE WHEN " + IsSale + @" THEN PriceAtPurchase ELSE 0 END) AS SalesValue,
                   SUM(CASE WHEN " + InRange + @" AND PaymentStatus = 'Paid' THEN PaymentAmount ELSE 0 END) AS Revenue,
                   SUM(CASE WHEN " + IsSale + @" AND PaymentStatus = 'Pending' THEN PaymentAmount ELSE 0 END) AS PendingAmount,
                   SUM(CASE WHEN " + InRange + @" AND PaymentStatus = 'Refunded' THEN PaymentAmount ELSE 0 END) AS RefundedAmount,
                   SUM(CASE WHEN ListingStatus = 'Available' THEN 1 ELSE 0 END) AS InStock,
                   SUM(CASE WHEN ListingStatus = 'Reserved' THEN 1 ELSE 0 END) AS Reserved,
                   SUM(CASE WHEN ListingStatus = 'Sold' THEN 1 ELSE 0 END) AS Sold,
                   COUNT(*) AS TotalListings
            FROM vw_SalesReportItems
            WHERE " + Filter + @"
            GROUP BY Category
            ORDER BY Category;";

    private const string CategoriesSql =
        "SELECT DISTINCT Category FROM vw_SalesReportItems WHERE " + Filter + " ORDER BY Category;";

    public ReportRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<SalesReportCategoryRowDto>> GetSalesByCategoryAsync(
        Guid? recyclerId, string? category, DateTime? fromUtc, DateTime? toUtcExclusive, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var command = new MySqlCommand(SalesByCategorySql, connection);
        AddFilterParameters(command, recyclerId, category);
        command.Parameters.AddWithValue("@From", fromUtc.HasValue ? fromUtc.Value : DBNull.Value);
        command.Parameters.AddWithValue("@To", toUtcExclusive.HasValue ? toUtcExclusive.Value : DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<SalesReportCategoryRowDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SalesReportCategoryRowDto
            {
                Category = reader.GetString("Category"),
                TotalSales = Convert.ToInt32(reader["TotalSales"]),
                SalesValue = Convert.ToDecimal(reader["SalesValue"]),
                Revenue = Convert.ToDecimal(reader["Revenue"]),
                PendingAmount = Convert.ToDecimal(reader["PendingAmount"]),
                RefundedAmount = Convert.ToDecimal(reader["RefundedAmount"]),
                InStock = Convert.ToInt32(reader["InStock"]),
                Reserved = Convert.ToInt32(reader["Reserved"]),
                Sold = Convert.ToInt32(reader["Sold"]),
                TotalListings = Convert.ToInt32(reader["TotalListings"])
            });
        }
        return results;
    }

    public async Task<List<string>> GetCategoriesAsync(Guid? recyclerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var command = new MySqlCommand(CategoriesSql, connection);
        AddFilterParameters(command, recyclerId, null);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            results.Add(reader.GetString("Category"));
        return results;
    }

    // A null parameter means "not filtered"
    private static void AddFilterParameters(MySqlCommand command, Guid? recyclerId, string? category)
    {
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.HasValue ? recyclerId.Value.ToString() : DBNull.Value);
        command.Parameters.AddWithValue("@Category", string.IsNullOrWhiteSpace(category) ? DBNull.Value : category);
    }
}
