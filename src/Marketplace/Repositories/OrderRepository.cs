using System.Data;
using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.DTOs;
using MySqlConnector;

namespace EcoTrack.MarketplaceService.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OrderResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT o.Id AS o_Id, o.ListingId AS o_ListingId, o.BuyerId AS o_BuyerId,
                   o.PriceAtPurchase AS o_PriceAtPurchase, o.Status AS o_Status,
                   o.CreatedAt AS o_CreatedAt, o.UpdatedAt AS o_UpdatedAt,
                   l.Id AS l_Id, l.ValuationId AS l_ValuationId, l.Title AS l_Title,
                   l.Price AS l_Price, l.PhotoPath AS l_PhotoPath, l.Status AS l_Status
            FROM Orders o
            INNER JOIN Listings l ON l.Id = o.ListingId
            WHERE o.Id = @Id AND l.IsDeleted = 0
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapOrderWithListing(reader);
        return null;
    }

    public async Task<OrderResponseDto?> CreateOrderAsync(
        Guid listingId, Guid buyerId, decimal priceAtPurchase, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var orderId = Guid.NewGuid();

        // Use a transaction so order + listing status update are atomic
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            // Insert the order
            const string insertSql = @"
                INSERT INTO Orders (Id, ListingId, BuyerId, PriceAtPurchase, Status, CreatedAt, UpdatedAt)
                VALUES (@Id, @ListingId, @BuyerId, @PriceAtPurchase, 'Placed', @CreatedAt, @UpdatedAt);";
            await using var insertCmd = new MySqlCommand(insertSql, connection, transaction);
            insertCmd.Parameters.AddWithValue("@Id", orderId.ToString());
            insertCmd.Parameters.AddWithValue("@ListingId", listingId.ToString());
            insertCmd.Parameters.AddWithValue("@BuyerId", buyerId.ToString());
            insertCmd.Parameters.AddWithValue("@PriceAtPurchase", priceAtPurchase);
            insertCmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);
            insertCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            var rows = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            if (rows == 0)
                throw new InvalidOperationException("Failed to create order.");

            // Update listing status to Reserved (conditional — only if still Available)
            const string updateSql = @"
                UPDATE Listings
                SET Status = 'Reserved', UpdatedAt = @UpdatedAt
                WHERE Id = @ListingId AND Status = 'Available';";
            await using var updateCmd = new MySqlCommand(updateSql, connection, transaction);
            updateCmd.Parameters.AddWithValue("@ListingId", listingId.ToString());
            updateCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
            var updated = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
            if (updated == 0)
                throw new InvalidOperationException("Listing is no longer available.");

            await transaction.CommitAsync(cancellationToken);

            // Fetch created order with listing data
            const string selectSql = @"
                SELECT o.Id AS o_Id, o.ListingId AS o_ListingId, o.BuyerId AS o_BuyerId,
                       o.PriceAtPurchase AS o_PriceAtPurchase, o.Status AS o_Status,
                       o.CreatedAt AS o_CreatedAt, o.UpdatedAt AS o_UpdatedAt,
                       l.Id AS l_Id, l.ValuationId AS l_ValuationId, l.Title AS l_Title,
                       l.Price AS l_Price, l.PhotoPath AS l_PhotoPath, l.Status AS l_Status
                FROM Orders o
                INNER JOIN Listings l ON l.Id = o.ListingId
                WHERE o.Id = @Id
                LIMIT 1;";
            await using var selectCmd = new MySqlCommand(selectSql, connection);
            selectCmd.Parameters.AddWithValue("@Id", orderId.ToString());
            await using var reader = await selectCmd.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
                return MapOrderWithListing(reader);

            return null;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<OrderResponseDto>> GetByBuyerIdAsync(
        Guid buyerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var skip = (page - 1) * pageSize;

        var sql = $@"
            SELECT o.Id AS o_Id, o.ListingId AS o_ListingId, o.BuyerId AS o_BuyerId,
                   o.PriceAtPurchase AS o_PriceAtPurchase, o.Status AS o_Status,
                   o.CreatedAt AS o_CreatedAt, o.UpdatedAt AS o_UpdatedAt,
                   l.Id AS l_Id, l.ValuationId AS l_ValuationId, l.Title AS l_Title,
                   l.Price AS l_Price, l.PhotoPath AS l_PhotoPath, l.Status AS l_Status
            FROM Orders o
            INNER JOIN Listings l ON l.Id = o.ListingId
            WHERE o.BuyerId = @BuyerId AND l.IsDeleted = 0
            ORDER BY o.CreatedAt DESC
            LIMIT @Limit OFFSET @Offset;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BuyerId", buyerId.ToString());
        command.Parameters.AddWithValue("@Limit", pageSize);
        command.Parameters.AddWithValue("@Offset", skip);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<OrderResponseDto>();
        while (await reader.ReadAsync(cancellationToken))
            results.Add(MapOrderWithListing(reader));
        return results;
    }

    public async Task<int> GetOrderCountByBuyerIdAsync(Guid buyerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM Orders o INNER JOIN Listings l ON l.Id = o.ListingId WHERE o.BuyerId = @BuyerId AND l.IsDeleted = 0;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BuyerId", buyerId.ToString());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> ListingStatusIsAvailableAsync(Guid listingId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM Listings WHERE Id = @Id AND Status = 'Available' AND IsDeleted = 0 LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", listingId.ToString());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static OrderResponseDto MapOrderWithListing(MySqlDataReader reader)
    {
        return new OrderResponseDto
        {
            Id = Guid.Parse(reader.GetString("o_Id")),
            ListingId = Guid.Parse(reader.GetString("o_ListingId")),
            BuyerId = Guid.Parse(reader.GetString("o_BuyerId")),
            PriceAtPurchase = reader.GetDecimal("o_PriceAtPurchase"),
            Status = reader.GetString("o_Status"),
            CreatedAt = reader.GetDateTime("o_CreatedAt"),
            UpdatedAt = reader.GetDateTime("o_UpdatedAt"),
            Listing = new ListingBriefDto
            {
                Id = Guid.Parse(reader.GetString("l_Id")),
                Title = reader.GetString("l_Title"),
                Price = reader.GetDecimal("l_Price"),
                Status = reader.GetString("l_Status"),
                PhotoPath = reader.IsDBNull(reader.GetOrdinal("l_PhotoPath")) ? null : reader.GetString("l_PhotoPath")
            }
        };
    }
}
