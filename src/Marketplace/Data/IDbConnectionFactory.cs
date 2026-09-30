using MySqlConnector;

namespace EcoTrack.MarketplaceService.Data;

public interface IDbConnectionFactory
{
    Task<MySqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}
