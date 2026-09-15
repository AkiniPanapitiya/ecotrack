using EcoTrack.IdentityService.Data;
using MySqlConnector;

namespace EcoTrack.IdentityService.Repositories;

public interface ITokenBlacklistRepository
{
    Task<bool> AddAsync(string jti, Guid userId, DateTime expiresAt, CancellationToken cancellationToken = default);
    Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default);
}

public class TokenBlacklistRepository : ITokenBlacklistRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TokenBlacklistRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> AddAsync(string jti, Guid userId, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO BlacklistedTokens (Id, Jti, UserId, ExpiresAt, CreatedAt)
            VALUES (@Id, @Jti, @UserId, @ExpiresAt, @CreatedAt);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@Jti", jti);
        command.Parameters.AddWithValue("@UserId", userId.ToString());
        command.Parameters.AddWithValue("@ExpiresAt", expiresAt);
        command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> IsBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT 1 FROM BlacklistedTokens WHERE Jti = @Jti LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Jti", jti);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result != null;
    }
}