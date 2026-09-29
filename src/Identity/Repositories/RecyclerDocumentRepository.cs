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
        string? backFileName = null, string? backFilePath = null, string? backFileType = null, long backFileSize = 0,
        CancellationToken cancellationToken = default);

    // Recycler: get my latest document
    Task<RecyclerDocument?> GetLatestByRecyclerIdAsync(
        Guid recyclerId, CancellationToken cancellationToken = default);

    // Get document by Id
    Task<RecyclerDocument?> GetByIdAsync(
        Guid documentId, CancellationToken cancellationToken = default);

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
        string? backFileName = null, string? backFilePath = null, string? backFileType = null, long backFileSize = 0,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO RecyclerDocuments (Id, RecyclerId, DocumentType, FileName, FilePath, FileType, FileSize, BackFileName, BackFilePath, BackFileType, BackFileSize, Status, SubmittedAt)
            VALUES (@Id, @RecyclerId, @DocumentType, @FileName, @FilePath, @FileType, @FileSize, @BackFileName, @BackFilePath, @BackFileType, @BackFileSize, 'Pending', @SubmittedAt);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());
        command.Parameters.AddWithValue("@DocumentType", documentType.Trim());
        command.Parameters.AddWithValue("@FileName", fileName.Trim());
        command.Parameters.AddWithValue("@FilePath", filePath.Trim());
        command.Parameters.AddWithValue("@FileType", fileType.Trim());
        command.Parameters.AddWithValue("@FileSize", fileSize);
        command.Parameters.AddWithValue("@BackFileName", (object?)backFileName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@BackFilePath", (object?)backFilePath?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@BackFileType", (object?)backFileType?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@BackFileSize", backFileSize);
        command.Parameters.AddWithValue("@SubmittedAt", DateTime.UtcNow);

        await command.ExecuteNonQueryAsync(cancellationToken);

        // Fetch back the created document
        const string fetchSql = @"
            SELECT Id, RecyclerId, DocumentType, FileName, FilePath, FileType, FileSize, BackFileName, BackFilePath, BackFileType, BackFileSize, Status, SubmittedAt, ReviewedBy, ReviewedAt, ReviewNote
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
            SELECT Id, RecyclerId, DocumentType, FileName, FilePath, FileType, FileSize, BackFileName, BackFilePath, BackFileType, BackFileSize, Status, SubmittedAt, ReviewedBy, ReviewedAt, ReviewNote
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

    public async Task<RecyclerDocument?> GetByIdAsync(
        Guid documentId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, RecyclerId, DocumentType, FileName, FilePath, FileType, FileSize, BackFileName, BackFilePath, BackFileType, BackFileSize, Status, SubmittedAt, ReviewedBy, ReviewedAt, ReviewNote
            FROM RecyclerDocuments
            WHERE Id = @Id
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", documentId.ToString());

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
            SELECT rd.Id, rd.RecyclerId, rd.DocumentType, rd.FileName, rd.FilePath, rd.FileType, rd.FileSize, 
                   rd.BackFileName, rd.BackFilePath, rd.BackFileType, rd.BackFileSize,
                   rd.Status, rd.SubmittedAt, rd.ReviewedBy, rd.ReviewedAt, rd.ReviewNote,
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
                Id = (await reader.GetFieldValueAsync<Guid>(16, cancellationToken)),
                FullName = reader.GetString(17),
                Email = reader.GetString(18),
                Role = reader.GetString(19)
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
        // Only block re-upload if there's a document still under review (Pending)
        // or already approved (Verified). Rejected documents allow re-upload.
        const string sql = @"SELECT COUNT(1) FROM RecyclerDocuments
                             WHERE RecyclerId = @RecyclerId
                             AND Status IN ('Pending', 'Verified');";
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RecyclerId", recyclerId.ToString());
        var count = (long)(await command.ExecuteScalarAsync(cancellationToken)!);
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
            BackFileName = reader.IsDBNull(7) ? null : reader.GetString(7),
            BackFilePath = reader.IsDBNull(8) ? null : reader.GetString(8),
            BackFileType = reader.IsDBNull(9) ? null : reader.GetString(9),
            BackFileSize = reader.IsDBNull(10) ? 0 : reader.GetInt64(10),
            Status = reader.GetString(11),
            SubmittedAt = reader.GetDateTime(12),
            ReviewedBy = reader.IsDBNull(13) ? null : reader.GetGuid(13),
            ReviewedAt = reader.IsDBNull(14) ? null : reader.GetDateTime(14),
            ReviewNote = reader.IsDBNull(15) ? null : reader.GetString(15)
        };
    }
}
