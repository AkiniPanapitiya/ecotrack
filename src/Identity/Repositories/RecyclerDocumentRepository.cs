using System.Data;
using EcoTrack.IdentityService.Data;
using EcoTrack.IdentityService.Models;
using MySqlConnector;

namespace EcoTrack.IdentityService.Repositories;

public interface IRecyclerDocumentRepository
{
    // Upload
    Task<RecyclerDocument> CreateDocumentAsync(
        Guid recyclerId, string documentType,
        string fileName, string filePath, string fileType, long fileSize,
        CancellationToken cancellationToken = default);

    // Recycler: get my latest document
    Task<RecyclerDocument?> GetLatestByRecyclerIdAsync(
        Guid recyclerId, CancellationToken cancellationToken = default);

    // Admin: get all pending submissions
    Task<List<(RecyclerDocument Doc, User Recycler)>> GetPendingSubmissionsAsync(
        CancellationToken cancellationToken = default);

    // Admin: update status (verify / reject)
    Task<bool> UpdateStatusAsync(
        Guid documentId, string status,
        Guid reviewedBy, string? reviewNote,
        CancellationToken cancellationToken = default);

    // Check if a recycler already has a document
    Task<bool> HasDocumentAsync(Guid recyclerId, CancellationToken cancellationToken = default);
}

public class RecyclerDocumentRepository : IRecyclerDocumentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RecyclerDocumentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RecyclerDocument> CreateDocumentAsync(
        Guid recyclerId, string documentType,
        string fileName, string filePath, string fileType, long fileSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO RecyclerDocuments (Id, RecyclerId, DocumentType, FileName, FilePath, FileType, FileSize, Status, SubmittedAt)
            VALUES (@Id, @RecyclerId, @DocumentType, @FileName, @FilePath, @FileType, @FileSize, 'Pending', @SubmittedAt);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());
        command.Parameters.AddWithValue("@DocumentType", documentType.Trim());
        command.Parameters.AddWithValue("@FileName", fileName.Trim());
        command.Parameters.AddWithValue("@FilePath", filePath.Trim());
        command.Parameters.AddWithValue("@FileType", fileType.Trim());
        command.Parameters.AddWithValue("@FileSize", fileSize);
        command.Parameters.AddWithValue("@SubmittedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);

        // Fetch back the created document
        const string fetchSql = @"
            SELECT Id, RecyclerId, DocumentType, FileName, FilePath, FileType, FileSize, Status, SubmittedAt, ReviewedBy, ReviewedAt, ReviewNote
            FROM RecyclerDocuments
            WHERE RecyclerId = @RecyclerId
            ORDER BY SubmittedAt DESC
            LIMIT 1;";

        await using var fetchCommand = new MySqlCommand(fetchSql, connection);
        fetchCommand.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());

        await using var reader = await fetchCommand.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return MapDocument(reader);
    }

    public async Task<RecyclerDocument?> GetLatestByRecyclerIdAsync(
        Guid recyclerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, RecyclerId, DocumentType, FileName, FilePath, FileType, FileSize, Status, SubmittedAt, ReviewedBy, ReviewedAt, ReviewNote
            FROM RecyclerDocuments
            WHERE RecyclerId = @RecyclerId
            ORDER BY SubmittedAt DESC
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapDocument(reader);
        }
        return null;
    }

    public async Task<List<(RecyclerDocument Doc, User Recycler)>> GetPendingSubmissionsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<(RecyclerDocument, User)>();

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT rd.Id, rd.RecyclerId, rd.DocumentType, rd.FileName, rd.FilePath, rd.FileType, rd.FileSize, rd.Status, rd.SubmittedAt,
                   u.Id, u.FullName, u.Email, u.Role
            FROM RecyclerDocuments rd
            INNER JOIN Users u ON rd.RecyclerId = u.Id
            WHERE rd.Status = 'Pending'
            ORDER BY rd.SubmittedAt ASC;";

        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var doc = MapDocument(reader);
            var user = new User
            {
                Id = reader.GetGuid(9),
                FullName = reader.GetString(10),
                Email = reader.GetString(11)
            };
            result.Add((doc, user));
        }

        return result;
    }

    public async Task<bool> UpdateStatusAsync(
        Guid documentId, string status,
        Guid reviewedBy, string? reviewNote,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE RecyclerDocuments
            SET Status = @Status,
                ReviewedBy = @ReviewedBy,
                ReviewedAt = @ReviewedAt,
                ReviewNote = @ReviewNote
            WHERE Id = @Id;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", documentId.ToString());
        command.Parameters.AddWithValue("@Status", status.Trim());
        command.Parameters.AddWithValue("@ReviewedBy", reviewedBy.ToString());
        command.Parameters.AddWithValue("@ReviewedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@ReviewNote", (object?)reviewNote ?? DBNull.Value);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    public async Task<bool> HasDocumentAsync(Guid recyclerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = "SELECT COUNT(1) FROM RecyclerDocuments WHERE RecyclerId = @RecyclerId;";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());
        var count = (int)(await command.ExecuteScalarAsync(cancellationToken)!);
        return count > 0;
    }

    private static RecyclerDocument MapDocument(MySqlDataReader reader)
    {
        return new RecyclerDocument
        {
            Id = reader.GetGuid(0),
            RecyclerId = reader.GetGuid(1),
            DocumentType = reader.GetString(2),
            FileName = reader.GetString(3),
            FilePath = reader.GetString(4),
            FileType = reader.GetString(5),
            FileSize = reader.GetInt64(6),
            Status = reader.GetString(7),
            SubmittedAt = reader.GetDateTime(8),
            ReviewedBy = reader.IsDBNull(9) ? null : reader.GetGuid(9),
            ReviewedAt = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
            ReviewNote = reader.IsDBNull(11) ? null : reader.GetString(11)
        };
    }
}
