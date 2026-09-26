using System.Data;
using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.DTOs;
using MySqlConnector;

namespace EcoTrack.MarketplaceService.Repositories;

public class ValuationRepository : IValuationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ValuationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ValuationResponseDto?> GetByPickupItemIdAsync(Guid pickupItemId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, PickupItemId, RecyclerId, Price, `Condition`, CreatedAt, UpdatedAt
            FROM ItemValuations
            WHERE PickupItemId = @PickupItemId
            LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PickupItemId", pickupItemId.ToString());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapValuation(reader);
        return null;
    }

    public async Task<ValuationResponseDto?> CreateAsync(ValuationResponseDto valuation, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO ItemValuations (Id, PickupItemId, RecyclerId, Price, `Condition`, CreatedAt, UpdatedAt)
            VALUES (@Id, @PickupItemId, @RecyclerId, @Price, @Condition, @CreatedAt, @UpdatedAt);";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", valuation.Id.ToString());
        command.Parameters.AddWithValue("@PickupItemId", valuation.PickupItemId.ToString());
        command.Parameters.AddWithValue("@RecyclerId", valuation.RecyclerId.ToString());
        command.Parameters.AddWithValue("@Price", valuation.Price);
        command.Parameters.AddWithValue("@Condition", valuation.Condition);
        command.Parameters.AddWithValue("@CreatedAt", valuation.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", valuation.UpdatedAt);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0 ? valuation : null;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateValuationRequestDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE ItemValuations
            SET Price = @Price, `Condition` = @Condition, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        command.Parameters.AddWithValue("@Price", dto.Price);
        command.Parameters.AddWithValue("@Condition", dto.Condition);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> PickupItemExistsAsync(Guid pickupItemId, CancellationToken cancellationToken = default)
    {
        // PickupItems is in Logistics DB — if both DBs are on the same MySQL server,
        // this cross-DB query works. Otherwise validate in app code.
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT COUNT(*) FROM ecotrack_logistics_db.PickupItems WHERE Id = @Id LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", pickupItemId.ToString());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static ValuationResponseDto MapValuation(MySqlDataReader reader)
    {
        var hasItemName = false;
        var hasQuantity = false;
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.GetName(i).Equals("ItemName", StringComparison.OrdinalIgnoreCase)) hasItemName = true;
            if (reader.GetName(i).Equals("Quantity", StringComparison.OrdinalIgnoreCase)) hasQuantity = true;
        }

        return new ValuationResponseDto
        {
            Id = Guid.Parse(reader.GetString("Id")),
            PickupItemId = Guid.Parse(reader.GetString("PickupItemId")),
            RecyclerId = Guid.Parse(reader.GetString("RecyclerId")),
            Price = reader.GetDecimal("Price"),
            Condition = reader.GetString("Condition"),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            UpdatedAt = reader.GetDateTime("UpdatedAt"),
            ItemName = hasItemName && !reader.IsDBNull(reader.GetOrdinal("ItemName")) ? reader.GetString("ItemName") : string.Empty,
            Quantity = hasQuantity && !reader.IsDBNull(reader.GetOrdinal("Quantity")) ? reader.GetInt32(reader.GetOrdinal("Quantity")) : 0
        };
    }

    public async Task<List<ValuationResponseDto>> GetByRecyclerIdAsync(string recyclerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT v.Id, v.PickupItemId, v.RecyclerId, v.Price, v.`Condition`, v.CreatedAt, v.UpdatedAt,
                   pi.ItemName, pi.Quantity
            FROM ItemValuations v
            LEFT JOIN ecotrack_logistics_db.PickupItems pi ON pi.Id = v.PickupItemId
            WHERE v.RecyclerId = @RecyclerId
            ORDER BY v.CreatedAt DESC;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RecyclerId", recyclerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<ValuationResponseDto>();
        while (await reader.ReadAsync(cancellationToken))
            results.Add(MapValuation(reader));
        return results;
    }
}
