using System.Data;
using EcoTrack.LogisticsService.Data;
using EcoTrack.LogisticsService.Models;
using MySqlConnector;

namespace EcoTrack.LogisticsService.Repositories;

public class DropoffRepository : IDropoffRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DropoffRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateDropoffAsync(DropoffRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Flip the pickup to Collected first; the Status guard stops a double drop-off
            // if two requests race past the service-level checks.
            const string updatePickupSql = @"
                UPDATE PickupRequests
                SET Status = 'Collected', UpdatedAt = @UpdatedAt
                WHERE Id = @Id AND Status = 'Scheduled';";

            await using (var updateCmd = new MySqlCommand(updatePickupSql, connection, transaction))
            {
                updateCmd.Parameters.AddWithValue("@Id", record.PickupRequestId.ToString());
                updateCmd.Parameters.AddWithValue("@UpdatedAt", record.CreatedAt);

                var rows = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                if (rows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }
            }

            const string insertSql = @"
                INSERT INTO DropoffRecords (Id, PickupRequestId, RecordedBy, DropoffDate, DropoffTime, ConditionNotes, CreatedAt)
                VALUES (@Id, @PickupRequestId, @RecordedBy, @DropoffDate, @DropoffTime, @ConditionNotes, @CreatedAt);";

            await using (var insertCmd = new MySqlCommand(insertSql, connection, transaction))
            {
                insertCmd.Parameters.AddWithValue("@Id", record.Id.ToString());
                insertCmd.Parameters.AddWithValue("@PickupRequestId", record.PickupRequestId.ToString());
                insertCmd.Parameters.AddWithValue("@RecordedBy", record.RecordedBy.ToString());
                insertCmd.Parameters.AddWithValue("@DropoffDate", record.DropoffDate.ToString("yyyy-MM-dd"));
                insertCmd.Parameters.AddWithValue("@DropoffTime", (object?)record.DropoffTime ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@ConditionNotes", (object?)record.ConditionNotes ?? DBNull.Value);
                insertCmd.Parameters.AddWithValue("@CreatedAt", record.CreatedAt);

                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            // Item lifecycle: Pending -> Collected -> Disposed (see ECO-71 migration)
            const string updateItemsSql = @"
                UPDATE PickupItems
                SET Status = 'Collected', UpdatedAt = @UpdatedAt
                WHERE PickupRequestId = @PickupRequestId AND Status = 'Pending';";

            await using (var itemsCmd = new MySqlCommand(updateItemsSql, connection, transaction))
            {
                itemsCmd.Parameters.AddWithValue("@PickupRequestId", record.PickupRequestId.ToString());
                itemsCmd.Parameters.AddWithValue("@UpdatedAt", record.CreatedAt);

                await itemsCmd.ExecuteNonQueryAsync(cancellationToken);
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

    public async Task<DropoffRecord?> GetByPickupRequestIdAsync(Guid pickupRequestId, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, PickupRequestId, RecordedBy, DropoffDate, DropoffTime, ConditionNotes, CreatedAt
            FROM DropoffRecords
            WHERE PickupRequestId = @PickupRequestId
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PickupRequestId", pickupRequestId.ToString());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DropoffRecord
        {
            Id = Guid.Parse(reader.GetString("Id")),
            PickupRequestId = Guid.Parse(reader.GetString("PickupRequestId")),
            RecordedBy = Guid.Parse(reader.GetString("RecordedBy")),
            DropoffDate = reader.GetDateTime("DropoffDate"),
            DropoffTime = reader.IsDBNull("DropoffTime") ? null : reader.GetTimeSpan("DropoffTime"),
            ConditionNotes = reader.IsDBNull("ConditionNotes") ? null : reader.GetString("ConditionNotes"),
            CreatedAt = reader.GetDateTime("CreatedAt")
        };
    }
}
