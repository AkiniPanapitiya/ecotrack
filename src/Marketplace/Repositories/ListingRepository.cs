using System.Data;
using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.DTOs;
using MySqlConnector;

namespace EcoTrack.MarketplaceService.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ListingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ListingResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT l.Id AS l_Id, l.ValuationId AS l_ValuationId, l.RecyclerId AS l_RecyclerId,
                   l.Title AS l_Title, l.Description AS l_Description,
                   l.Price AS l_Price, l.PhotoPath AS l_PhotoPath, l.Status AS l_Status,
                   l.IsDeleted AS l_IsDeleted,
                   l.CreatedAt AS l_CreatedAt, l.UpdatedAt AS l_UpdatedAt,
                   v.Id AS v_Id, v.PickupItemId AS v_PickupItemId,
                   v.Price AS v_Price, v.Condition AS v_Condition,
                   v.CreatedAt AS v_CreatedAt, v.UpdatedAt AS v_UpdatedAt,
                   pi.ItemName, pi.Quantity
            FROM Listings l
            INNER JOIN ItemValuations v ON v.Id = l.ValuationId
            INNER JOIN ecotrack_logistics_db.PickupItems pi ON pi.Id = v.PickupItemId
            WHERE l.Id = @Id AND l.IsDeleted = 0
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapListing(reader);
        return null;
    }

    public async Task<ValuationResponseDto?> GetValuationByIdAsync(Guid valuationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT v.Id AS v_Id, v.PickupItemId AS v_PickupItemId,
                   v.Price AS v_Price, v.Condition AS v_Condition,
                   v.CreatedAt AS v_CreatedAt, v.UpdatedAt AS v_UpdatedAt,
                   pi.ItemName, pi.Quantity
            FROM ItemValuations v
            INNER JOIN ecotrack_logistics_db.PickupItems pi ON pi.Id = v.PickupItemId
            WHERE v.Id = @ValuationId
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ValuationId", valuationId.ToString());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapValuation(reader);
        return null;
    }

    public async Task<List<ListingResponseDto>> BrowseAsync(
        string? keyword, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var conditions = new List<string> { "l.Status = 'Available'", "l.IsDeleted = 0" };
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            conditions.Add("(l.Title LIKE @Keyword OR l.Description LIKE @Keyword)");
            parameters.Add(new MySqlParameter("@Keyword", $"%{keyword}%"));
        }

        var whereClause = string.Join(" AND ", conditions);
        var skip = (page - 1) * pageSize;

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM Listings l WHERE {whereClause};";
        await using var countCommand = new MySqlCommand(countSql, connection);
        foreach (var p in parameters)
            countCommand.Parameters.Add(p);
        var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken));

        // Get paginated results
        var dataSql = $@"
            SELECT l.Id AS l_Id, l.ValuationId AS l_ValuationId, l.RecyclerId AS l_RecyclerId,
                   l.Title AS l_Title, l.Description AS l_Description,
                   l.Price AS l_Price, l.PhotoPath AS l_PhotoPath, l.Status AS l_Status,
                   l.IsDeleted AS l_IsDeleted,
                   l.CreatedAt AS l_CreatedAt, l.UpdatedAt AS l_UpdatedAt,
                   v.Id AS v_Id, v.PickupItemId AS v_PickupItemId,
                   v.Price AS v_Price, v.Condition AS v_Condition,
                   v.CreatedAt AS v_CreatedAt, v.UpdatedAt AS v_UpdatedAt,
                   pi.ItemName, pi.Quantity
            FROM Listings l
            INNER JOIN ItemValuations v ON v.Id = l.ValuationId
            INNER JOIN ecotrack_logistics_db.PickupItems pi ON pi.Id = v.PickupItemId
            WHERE {whereClause}
            ORDER BY l.CreatedAt DESC
            LIMIT @Limit OFFSET @Offset;";
        await using var dataCommand = new MySqlCommand(dataSql, connection);
        foreach (var p in parameters)
            dataCommand.Parameters.Add(p);
        dataCommand.Parameters.AddWithValue("@Limit", pageSize);
        dataCommand.Parameters.AddWithValue("@Offset", skip);
        await using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);
        var results = new List<ListingResponseDto>();
        while (await reader.ReadAsync(cancellationToken))
            results.Add(MapListing(reader));
        return results;
    }

    public async Task<bool> CreateAsync(ListingResponseDto listing, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO Listings (Id, ValuationId, RecyclerId, Title, Description, Price, PhotoPath, Status, IsDeleted, CreatedAt, UpdatedAt)
            VALUES (@Id, @ValuationId, @RecyclerId, @Title, @Description, @Price, @PhotoPath, @Status, @IsDeleted, @CreatedAt, @UpdatedAt);";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", listing.Id.ToString());
        command.Parameters.AddWithValue("@ValuationId", listing.ValuationId.ToString());
        command.Parameters.AddWithValue("@RecyclerId", listing.RecyclerId.ToString());
        command.Parameters.AddWithValue("@Title", listing.Title);
        command.Parameters.AddWithValue("@Description", (object?)listing.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("@Price", listing.Price);
        command.Parameters.AddWithValue("@PhotoPath", (object?)listing.PhotoPath ?? DBNull.Value);
        command.Parameters.AddWithValue("@Status", listing.Status);
        command.Parameters.AddWithValue("@IsDeleted", 0);
        command.Parameters.AddWithValue("@CreatedAt", listing.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", listing.UpdatedAt);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateListingRequestDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Build dynamic UPDATE — only update fields that were provided
        var sets = new List<string>();
        var parameters = new List<MySqlParameter>();
        parameters.Add(new MySqlParameter("@Id", id.ToString()));

        if (dto.Title != null)
        {
            sets.Add("Title = @Title");
            parameters.Add(new MySqlParameter("@Title", dto.Title));
        }
        if (dto.Description != null)
        {
            sets.Add("Description = @Description");
            parameters.Add(new MySqlParameter("@Description", dto.Description));
        }
        if (dto.Price.HasValue)
        {
            sets.Add("Price = @Price");
            parameters.Add(new MySqlParameter("@Price", dto.Price.Value));
        }
        if (dto.PhotoPath != null)
        {
            sets.Add("PhotoPath = @PhotoPath");
            parameters.Add(new MySqlParameter("@PhotoPath", dto.PhotoPath));
        }
        if (dto.Status != null)
        {
            sets.Add("Status = @Status");
            parameters.Add(new MySqlParameter("@Status", dto.Status));
        }

        if (sets.Count == 0)
            return false;

        sets.Add("UpdatedAt = @UpdatedAt");
        parameters.Add(new MySqlParameter("@UpdatedAt", DateTime.UtcNow));

        var sql = $"UPDATE Listings SET {string.Join(", ", sets)} WHERE Id = @Id AND IsDeleted = 0;";
        await using var command = new MySqlCommand(sql, connection);
        foreach (var p in parameters)
            command.Parameters.Add(p);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE Listings
            SET IsDeleted = 1, UpdatedAt = @UpdatedAt, Status = 'Sold'
            WHERE Id = @Id AND IsDeleted = 0;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> ValuationExistsAsync(Guid valuationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM ItemValuations WHERE Id = @Id LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", valuationId.ToString());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    public async Task<bool> ListingExistsForValuationAsync(Guid valuationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(*) FROM Listings WHERE ValuationId = @ValuationId AND IsDeleted = 0 LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@ValuationId", valuationId.ToString());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static ListingResponseDto MapListing(MySqlDataReader reader)
    {
        return new ListingResponseDto
        {
            Id = Guid.Parse(reader.GetString("l_Id")),
            ValuationId = Guid.Parse(reader.GetString("l_ValuationId")),
            RecyclerId = Guid.Parse(reader.GetString("l_RecyclerId")),
            Title = reader.GetString("l_Title"),
            Description = reader.IsDBNull(reader.GetOrdinal("l_Description")) ? null : reader.GetString("l_Description"),
            Price = reader.GetDecimal("l_Price"),
            PhotoPath = reader.IsDBNull(reader.GetOrdinal("l_PhotoPath")) ? null : reader.GetString("l_PhotoPath"),
            Status = reader.GetString("l_Status"),
            CreatedAt = reader.GetDateTime("l_CreatedAt"),
            UpdatedAt = reader.GetDateTime("l_UpdatedAt"),
            Valuation = new ValuationResponseDto
            {
                Id = Guid.Parse(reader.GetString("v_Id")),
                PickupItemId = Guid.Parse(reader.GetString("v_PickupItemId")),
                RecyclerId = Guid.Parse(reader.GetString("l_RecyclerId")),
                Price = reader.GetDecimal("v_Price"),
                Condition = reader.GetString("v_Condition"),
                CreatedAt = reader.GetDateTime("v_CreatedAt"),
                UpdatedAt = reader.GetDateTime("v_UpdatedAt")
            }
        };
    }

    private static ValuationResponseDto MapValuation(MySqlDataReader reader)
    {
        return new ValuationResponseDto
        {
            Id = Guid.Parse(reader.GetString("v_Id")),
            PickupItemId = Guid.Parse(reader.GetString("v_PickupItemId")),
            Price = reader.GetDecimal("v_Price"),
            Condition = reader.GetString("v_Condition"),
            CreatedAt = reader.GetDateTime("v_CreatedAt"),
            UpdatedAt = reader.GetDateTime("v_UpdatedAt"),
            ItemName = reader.GetString("ItemName"),
            Quantity = reader.GetInt32(reader.GetOrdinal("Quantity"))
        };
    }
}
