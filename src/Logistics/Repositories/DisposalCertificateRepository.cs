using System.Data;
using EcoTrack.LogisticsService.Data;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Models;
using MySqlConnector;

namespace EcoTrack.LogisticsService.Repositories;

public class DisposalCertificateRepository : IDisposalCertificateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DisposalCertificateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(bool Success, DisposalCertificateDto? Certificate)> CreateCertificateAsync(
        Guid pickupItemId, Guid recyclerId, string recyclerName,
        string disposalMethod, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check item exists and is not already certified
            const string checkSql = @"
                SELECT pi.Id, pi.ItemName, pi.Quantity, pi.ItemCondition, pi.Status
                FROM PickupItems pi
                WHERE pi.Id = @PickupItemId;";

            await using var checkCmd = new MySqlCommand(checkSql, connection, transaction);
            checkCmd.Parameters.AddWithValue("@PickupItemId", pickupItemId.ToString());

            await using var reader = await checkCmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return (false, null); // Item not found
            }

            var existingStatus = reader.GetString(4);
            var itemName = reader.GetString(1);
            var quantity = reader.GetInt32(2);
            var itemCondition = reader.GetString(3);
            reader.Close();

            if (existingStatus == "Disposed")
            {
                return (false, null); // Already disposed
            }

            // Insert certificate
            var certId = Guid.NewGuid();
            var disposedAt = DateTime.UtcNow;

            const string insertSql = @"
                INSERT INTO DisposalCertificates (Id, PickupItemId, RecyclerId, RecyclerName, DisposalMethod, DisposedAt, CreatedAt)
                VALUES (@Id, @PickupItemId, @RecyclerId, @RecyclerName, @DisposalMethod, @DisposedAt, @CreatedAt);";

            await using var insertCmd = new MySqlCommand(insertSql, connection, transaction);
            insertCmd.Parameters.AddWithValue("@Id", certId.ToString());
            insertCmd.Parameters.AddWithValue("@PickupItemId", pickupItemId.ToString());
            insertCmd.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());
            insertCmd.Parameters.AddWithValue("@RecyclerName", recyclerName);
            insertCmd.Parameters.AddWithValue("@DisposalMethod", disposalMethod);
            insertCmd.Parameters.AddWithValue("@DisposedAt", disposedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            insertCmd.Parameters.AddWithValue("@CreatedAt", disposedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            var rows = await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            if (rows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return (false, null);
            }

            // Update item status to Disposed
            const string updateSql = @"
                UPDATE PickupItems
                SET Status = 'Disposed', UpdatedAt = @UpdatedAt
                WHERE Id = @Id;";

            await using var updateCmd = new MySqlCommand(updateSql, connection, transaction);
            updateCmd.Parameters.AddWithValue("@Id", pickupItemId.ToString());
            updateCmd.Parameters.AddWithValue("@UpdatedAt", disposedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            await updateCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return (true, new DisposalCertificateDto
            {
                Id = certId,
                PickupItemId = pickupItemId,
                RecyclerId = recyclerId,
                RecyclerName = recyclerName,
                DisposalMethod = disposalMethod,
                DisposedAt = disposedAt,
                CreatedAt = disposedAt,
                ItemName = itemName,
                Quantity = quantity,
                ItemCondition = itemCondition
            });
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<DisposalCertificateDto?> GetByPickupItemIdAsync(
        Guid pickupItemId, CancellationToken cancellationToken = default)
    {
        await using var connection = (MySqlConnection)await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT dc.Id, dc.PickupItemId, dc.RecyclerId, dc.RecyclerName,
                   dc.DisposalMethod, dc.DisposedAt, dc.CreatedAt,
                   pi.ItemName, pi.Quantity, pi.ItemCondition,
                   pr.UserId
            FROM DisposalCertificates dc
            INNER JOIN PickupItems pi ON pi.Id = dc.PickupItemId
            INNER JOIN PickupRequests pr ON pr.Id = pi.PickupRequestId
            WHERE dc.PickupItemId = @PickupItemId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@PickupItemId", pickupItemId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DisposalCertificateDto
        {
            Id = Guid.Parse(reader.GetString(0)),
            PickupItemId = Guid.Parse(reader.GetString(1)),
            RecyclerId = Guid.Parse(reader.GetString(2)),
            RecyclerName = reader.GetString(3),
            DisposalMethod = reader.GetString(4),
            DisposedAt = reader.GetDateTime(5),
            CreatedAt = reader.GetDateTime(6),
            ItemName = reader.GetString(7),
            Quantity = reader.GetInt32(8),
            ItemCondition = reader.GetString(9)
        };
    }
}
