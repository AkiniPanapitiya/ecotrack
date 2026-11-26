using System.Data;
using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.DTOs;
using MySqlConnector;

namespace EcoTrack.MarketplaceService.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    private const string SelectColumns = @"
            SELECT p.Id AS p_Id, p.OrderId AS p_OrderId, p.BuyerId AS p_BuyerId,
                   p.RecyclerId AS p_RecyclerId, p.Amount AS p_Amount, p.Status AS p_Status,
                   p.PaidAt AS p_PaidAt, p.RefundedAt AS p_RefundedAt,
                   p.CreatedAt AS p_CreatedAt, p.UpdatedAt AS p_UpdatedAt,
                   o.Status AS o_Status, l.Title AS l_Title
            FROM PaymentRecords p
            INNER JOIN Orders o ON o.Id = p.OrderId
            LEFT JOIN Listings l ON l.Id = o.ListingId";

    public PaymentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PaymentRecordResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = SelectColumns + " WHERE p.Id = @Id LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapPayment(reader);
        return null;
    }

    public async Task<PaymentRecordResponseDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = SelectColumns + " WHERE p.OrderId = @OrderId LIMIT 1;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@OrderId", orderId.ToString());
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return MapPayment(reader);
        return null;
    }

    public async Task<List<PaymentRecordResponseDto>> GetPagedAsync(
        Guid? buyerId, Guid? recyclerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var (whereClause, parameters) = BuildFilter(buyerId, recyclerId, status);
        var sql = SelectColumns + $@"
            {whereClause}
            ORDER BY p.CreatedAt DESC
            LIMIT @Limit OFFSET @Offset;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());
        command.Parameters.AddWithValue("@Limit", pageSize);
        command.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var results = new List<PaymentRecordResponseDto>();
        while (await reader.ReadAsync(cancellationToken))
            results.Add(MapPayment(reader));
        return results;
    }

    public async Task<int> GetCountAsync(
        Guid? buyerId, Guid? recyclerId, string? status, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var (whereClause, parameters) = BuildFilter(buyerId, recyclerId, status);
        var sql = $"SELECT COUNT(*) FROM PaymentRecords p {whereClause};";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddRange(parameters.ToArray());
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<bool> MarkAsPaidAsync(Guid id, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        // Conditional update — only a Pending record can become Paid
        const string sql = @"
            UPDATE PaymentRecords
            SET Status = 'Paid', PaidAt = @Now, UpdatedBy = @UpdatedBy, UpdatedAt = @Now
            WHERE Id = @Id AND Status = 'Pending';";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());
        command.Parameters.AddWithValue("@UpdatedBy", updatedBy.ToString());
        command.Parameters.AddWithValue("@Now", DateTime.UtcNow);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> MarkAsRefundedAsync(Guid id, Guid updatedBy, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Use a transaction so the refund + order cancellation are atomic
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;

            // Conditional update — only a Paid record can be Refunded
            const string refundSql = @"
                UPDATE PaymentRecords
                SET Status = 'Refunded', RefundedAt = @Now, UpdatedBy = @UpdatedBy, UpdatedAt = @Now
                WHERE Id = @Id AND Status = 'Paid';";
            await using var refundCmd = new MySqlCommand(refundSql, connection, transaction);
            refundCmd.Parameters.AddWithValue("@Id", id.ToString());
            refundCmd.Parameters.AddWithValue("@UpdatedBy", updatedBy.ToString());
            refundCmd.Parameters.AddWithValue("@Now", now);
            var refunded = await refundCmd.ExecuteNonQueryAsync(cancellationToken);
            if (refunded == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            // Cancel the linked order
            const string cancelSql = @"
                UPDATE Orders o
                INNER JOIN PaymentRecords p ON p.OrderId = o.Id
                SET o.Status = 'Cancelled', o.UpdatedAt = @Now
                WHERE p.Id = @Id;";
            await using var cancelCmd = new MySqlCommand(cancelSql, connection, transaction);
            cancelCmd.Parameters.AddWithValue("@Id", id.ToString());
            cancelCmd.Parameters.AddWithValue("@Now", now);
            await cancelCmd.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static (string WhereClause, List<MySqlParameter> Parameters) BuildFilter(
        Guid? buyerId, Guid? recyclerId, string? status)
    {
        var conditions = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (buyerId.HasValue)
        {
            conditions.Add("p.BuyerId = @BuyerId");
            parameters.Add(new MySqlParameter("@BuyerId", buyerId.Value.ToString()));
        }
        if (recyclerId.HasValue)
        {
            conditions.Add("p.RecyclerId = @RecyclerId");
            parameters.Add(new MySqlParameter("@RecyclerId", recyclerId.Value.ToString()));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            conditions.Add("p.Status = @Status");
            parameters.Add(new MySqlParameter("@Status", status));
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        return (whereClause, parameters);
    }

    private static PaymentRecordResponseDto MapPayment(MySqlDataReader reader)
    {
        return new PaymentRecordResponseDto
        {
            Id = Guid.Parse(reader.GetString("p_Id")),
            OrderId = Guid.Parse(reader.GetString("p_OrderId")),
            BuyerId = Guid.Parse(reader.GetString("p_BuyerId")),
            RecyclerId = Guid.Parse(reader.GetString("p_RecyclerId")),
            Amount = reader.GetDecimal("p_Amount"),
            Status = reader.GetString("p_Status"),
            PaidAt = reader.IsDBNull(reader.GetOrdinal("p_PaidAt")) ? null : reader.GetDateTime("p_PaidAt"),
            RefundedAt = reader.IsDBNull(reader.GetOrdinal("p_RefundedAt")) ? null : reader.GetDateTime("p_RefundedAt"),
            CreatedAt = reader.GetDateTime("p_CreatedAt"),
            UpdatedAt = reader.GetDateTime("p_UpdatedAt"),
            OrderStatus = reader.GetString("o_Status"),
            ListingTitle = reader.IsDBNull(reader.GetOrdinal("l_Title")) ? null : reader.GetString("l_Title")
        };
    }
}
