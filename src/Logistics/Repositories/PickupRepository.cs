using System.Data;
using EcoTrack.LogisticsService.Data;
using EcoTrack.LogisticsService.Models;
using MySqlConnector;

namespace EcoTrack.LogisticsService.Repositories;

public interface IPickupRepository
{
    Task<bool> CreatePickupAsync(PickupRequest request, CancellationToken cancellationToken = default);
    Task<PickupRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PickupRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<PickupRequest>> GetPendingPickupsAsync(CancellationToken cancellationToken = default);          // NEW
    Task<IEnumerable<PickupRequest>> GetByRecyclerIdAsync(Guid recyclerId, CancellationToken cancellationToken = default);   // NEW
    Task<bool> HasConflictAsync(Guid recyclerId, DateTime scheduledDate, string scheduledTimeSlot, CancellationToken cancellationToken = default);   // NEW
    Task<bool> ConfirmScheduleAsync(Guid id, Guid recyclerId, DateTime scheduledDate, string scheduledTimeSlot, CancellationToken cancellationToken = default);   // NEW

}

public class PickupRepository : IPickupRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PickupRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreatePickupAsync(PickupRequest request, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string sql = @"
                INSERT INTO PickupRequests (Id, UserId, Category, EstimatedWeightKg, PickupAddress, ContactPhone, PreferredDate, TimeSlot, SpecialInstructions, Status, CreatedAt, UpdatedAt)
                VALUES (@Id, @UserId, @Category, @EstimatedWeightKg, @PickupAddress, @ContactPhone, @PreferredDate, @TimeSlot, @SpecialInstructions, @Status, @CreatedAt, @UpdatedAt);";

            await using (var command = new MySqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", request.Id.ToString());
                command.Parameters.AddWithValue("@UserId", request.UserId.ToString());
                command.Parameters.AddWithValue("@Category", request.Category.Trim());
                command.Parameters.AddWithValue("@EstimatedWeightKg", request.EstimatedWeightKg);
                command.Parameters.AddWithValue("@PickupAddress", request.PickupAddress.Trim());
                command.Parameters.AddWithValue("@ContactPhone", request.ContactPhone.Trim());
                command.Parameters.AddWithValue("@PreferredDate", request.PreferredDate.ToString("yyyy-MM-dd"));
                command.Parameters.AddWithValue("@TimeSlot", request.TimeSlot.Trim());
                command.Parameters.AddWithValue("@SpecialInstructions", (object?)request.SpecialInstructions?.Trim() ?? DBNull.Value);
                command.Parameters.AddWithValue("@Status", request.Status);
                command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
                command.Parameters.AddWithValue("@UpdatedAt", request.UpdatedAt);

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            if (request.Items != null && request.Items.Count > 0)
            {
                const string itemSql = @"
                    INSERT INTO PickupItems (Id, PickupRequestId, ItemName, Quantity, ItemCondition, EstimatedWeightKg)
                    VALUES (@Id, @PickupRequestId, @ItemName, @Quantity, @ItemCondition, @EstimatedWeightKg);";

                foreach (var item in request.Items)
                {
                    await using var itemCommand = new MySqlCommand(itemSql, connection, transaction);
                    itemCommand.Parameters.AddWithValue("@Id", item.Id.ToString());
                    itemCommand.Parameters.AddWithValue("@PickupRequestId", request.Id.ToString());
                    itemCommand.Parameters.AddWithValue("@ItemName", item.ItemName.Trim());
                    itemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCommand.Parameters.AddWithValue("@ItemCondition", item.ItemCondition);
                    itemCommand.Parameters.AddWithValue("@EstimatedWeightKg", item.EstimatedWeightKg);

                    await itemCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PickupRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, UserId, RecyclerId, Category, EstimatedWeightKg, PickupAddress, ContactPhone, PreferredDate, ScheduledDate, TimeSlot, ScheduledTimeSlot, SpecialInstructions, Status, CreatedAt, UpdatedAt
            FROM PickupRequests
            WHERE Id = @Id
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapPickupRequest(reader);
        }

        return null;
    }

    public async Task<IEnumerable<PickupRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, UserId, RecyclerId, Category, EstimatedWeightKg, PickupAddress, ContactPhone, PreferredDate, ScheduledDate, TimeSlot, ScheduledTimeSlot, SpecialInstructions, Status, CreatedAt, UpdatedAt
            FROM PickupRequests
            WHERE UserId = @UserId
            ORDER BY CreatedAt DESC;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId.ToString());

        var list = new List<PickupRequest>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapPickupRequest(reader));
        }

        return list;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE PickupRequests
            SET Status = @Status, UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    private static PickupRequest MapPickupRequest(MySqlDataReader reader)
    {
        return new PickupRequest
        {
            Id = Guid.Parse(reader.GetString("Id")),
            UserId = Guid.Parse(reader.GetString("UserId")),
            RecyclerId = reader.IsDBNull("RecyclerId") ? null : Guid.Parse(reader.GetString("RecyclerId")),
            Category = reader.GetString("Category"),
            EstimatedWeightKg = reader.GetDecimal("EstimatedWeightKg"),
            PickupAddress = reader.GetString("PickupAddress"),
            ContactPhone = reader.GetString("ContactPhone"),
            PreferredDate = reader.GetDateTime("PreferredDate"),
            ScheduledDate = reader.IsDBNull("ScheduledDate") ? null : reader.GetDateTime("ScheduledDate"),
            TimeSlot = reader.GetString("TimeSlot"),
            ScheduledTimeSlot = reader.IsDBNull("ScheduledTimeSlot") ? null : reader.GetString("ScheduledTimeSlot"),
            SpecialInstructions = reader.IsDBNull("SpecialInstructions") ? null : reader.GetString("SpecialInstructions"),
            Status = reader.GetString("Status"),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            UpdatedAt = reader.GetDateTime("UpdatedAt")
        };
    }
    public async Task<IEnumerable<PickupRequest>> GetPendingPickupsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, UserId, RecyclerId, Category, EstimatedWeightKg, PickupAddress, ContactPhone,
                PreferredDate, ScheduledDate, TimeSlot, ScheduledTimeSlot, SpecialInstructions,
                Status, CreatedAt, UpdatedAt
            FROM PickupRequests
            WHERE Status = 'Pending'
            ORDER BY PreferredDate ASC;";

        await using var command = new MySqlCommand(sql, connection);
        var list = new List<PickupRequest>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapPickupRequest(reader));
        }
        return list;
    }

    public async Task<IEnumerable<PickupRequest>> GetByRecyclerIdAsync(Guid recyclerId, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, UserId, RecyclerId, Category, EstimatedWeightKg, PickupAddress, ContactPhone,
                PreferredDate, ScheduledDate, TimeSlot, ScheduledTimeSlot, SpecialInstructions,
                Status, CreatedAt, UpdatedAt
            FROM PickupRequests
            WHERE RecyclerId = @RecyclerId
            ORDER BY ScheduledDate ASC;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());

        var list = new List<PickupRequest>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(MapPickupRequest(reader));
        }
        return list;
    }

    public async Task<bool> HasConflictAsync(Guid recyclerId, DateTime scheduledDate, string scheduledTimeSlot, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT COUNT(*) FROM PickupRequests
            WHERE RecyclerId = @RecyclerId
            AND ScheduledDate = @ScheduledDate
            AND ScheduledTimeSlot = @ScheduledTimeSlot
            AND Status = 'Scheduled';";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());
        command.Parameters.AddWithValue("@ScheduledDate", scheduledDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@ScheduledTimeSlot", scheduledTimeSlot);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
        return count > 0;
    }

    public async Task<bool> ConfirmScheduleAsync(Guid id, Guid recyclerId, DateTime scheduledDate, string scheduledTimeSlot, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE PickupRequests
            SET RecyclerId = @RecyclerId, ScheduledDate = @ScheduledDate,
                ScheduledTimeSlot = @ScheduledTimeSlot, Status = 'Scheduled', UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());
        command.Parameters.AddWithValue("@ScheduledDate", scheduledDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("@ScheduledTimeSlot", scheduledTimeSlot);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
}
