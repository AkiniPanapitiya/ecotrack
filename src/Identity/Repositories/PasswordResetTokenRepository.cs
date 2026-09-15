using EcoTrack.IdentityService.Data;
using MySqlConnector;

namespace EcoTrack.IdentityService.Repositories;

public interface IPasswordResetTokenRepository
{
    Task<bool> AddAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<(Guid UserId, DateTime ExpiresAt, bool IsUsed)?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> MarkAsUsedAsync(string tokenHash, CancellationToken cancellationToken = default);
}

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PasswordResetTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> AddAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO PasswordResetTokens (Id, UserId, TokenHash, ExpiresAt, IsUsed, CreatedAt)
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAt, @IsUsed, @CreatedAt);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@UserId", userId.ToString());
        command.Parameters.AddWithValue("@TokenHash", tokenHash);
        command.Parameters.AddWithValue("@ExpiresAt", expiresAt);
        command.Parameters.AddWithValue("@IsUsed", false);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }
    
    public async Task<(Guid UserId, DateTime ExpiresAt, bool IsUsed)?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT UserId, ExpiresAt, IsUsed
            FROM PasswordResetTokens
            WHERE TokenHash = @TokenHash
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TokenHash", tokenHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
    {
        return null;
    }

    return (
        Guid.Parse(reader.GetString("UserId")),
        reader.GetDateTime("ExpiresAt"),
        reader.GetBoolean("IsUsed")
    );
    }

    public async Task<bool> MarkAsUsedAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "UPDATE PasswordResetTokens SET IsUsed = TRUE WHERE TokenHash = @TokenHash;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@TokenHash", tokenHash);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }   
}