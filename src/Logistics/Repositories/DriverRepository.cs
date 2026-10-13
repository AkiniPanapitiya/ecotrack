using System.Data;
using EcoTrack.LogisticsService.Data;
using EcoTrack.LogisticsService.Models;
using MySqlConnector;

namespace EcoTrack.LogisticsService.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DriverRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(Driver driver, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO Drivers (Id, RecyclerId, FullName, Phone, VehicleNumber, IsActive)
            VALUES (@Id, @RecyclerId, @FullName, @Phone, @VehicleNumber, 1);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", driver.Id.ToString());
        command.Parameters.AddWithValue("@RecyclerId", driver.RecyclerId.ToString());
        command.Parameters.AddWithValue("@FullName", driver.FullName);
        command.Parameters.AddWithValue("@Phone", driver.Phone);
        command.Parameters.AddWithValue("@VehicleNumber", (object?)driver.VehicleNumber ?? DBNull.Value);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<IEnumerable<Driver>> GetActiveByRecyclerIdAsync(Guid recyclerId, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, RecyclerId, FullName, Phone, VehicleNumber, IsActive, CreatedAt
            FROM Drivers
            WHERE RecyclerId = @RecyclerId AND IsActive = 1
            ORDER BY FullName;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());

        var list = new List<Driver>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapDriver(reader));
        }
        return list;
    }

    public async Task<Driver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, RecyclerId, FullName, Phone, VehicleNumber, IsActive, CreatedAt
            FROM Drivers
            WHERE Id = @Id
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapDriver(reader) : null;
    }

    public async Task<bool> AssignToPickupAsync(Guid pickupId, Guid driverId, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE PickupRequests
            SET DriverId = @DriverId
            WHERE Id = @PickupId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@DriverId", driverId.ToString());
        command.Parameters.AddWithValue("@PickupId", pickupId.ToString());

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<Driver?> GetAssignedDriverAsync(Guid pickupId, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT d.Id, d.RecyclerId, d.FullName, d.Phone, d.VehicleNumber, d.IsActive, d.CreatedAt
            FROM PickupRequests pr
            INNER JOIN Drivers d ON d.Id = pr.DriverId
            WHERE pr.Id = @PickupId
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PickupId", pickupId.ToString());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? MapDriver(reader) : null;
    }

    private static Driver MapDriver(MySqlDataReader reader)
    {
        return new Driver
        {
            Id = Guid.Parse(reader.GetString(0)),
            RecyclerId = Guid.Parse(reader.GetString(1)),
            FullName = reader.GetString(2),
            Phone = reader.GetString(3),
            VehicleNumber = reader.IsDBNull(4) ? null : reader.GetString(4),
            IsActive = reader.GetBoolean(5),
            CreatedAt = reader.GetDateTime(6)
        };
    }
}