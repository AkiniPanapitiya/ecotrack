using System.Data.Common;
using MySqlConnector;

namespace EcoTrack.IdentityService.Data;

public interface IDbConnectionFactory
{
    Task<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("IdentityDb")
            ?? throw new InvalidOperationException("Connection string 'IdentityDb' not found in configuration.");
    }

    public async Task<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
