using System.Data;
using EcoTrack.IdentityService.Data;
using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using MySqlConnector;

namespace EcoTrack.IdentityService.Repositories;

public interface IAuditRepository
{
    Task<bool> LogActivityAsync(UserAuditLog log, CancellationToken cancellationToken = default);
    Task<AuditReportResponseDto> GetAuditReportAsync(AuditReportRequestDto request, CancellationToken cancellationToken = default);
}

public class AuditRepository : IAuditRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AuditRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> LogActivityAsync(UserAuditLog log, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO UserAuditLogs (Id, UserId, UserEmail, Action, Role, Details, IpAddress, Timestamp)
            VALUES (@Id, @UserId, @UserEmail, @Action, @Role, @Details, @IpAddress, @Timestamp);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", log.Id.ToString());
        command.Parameters.AddWithValue("@UserId", (object?)log.UserId?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("@UserEmail", log.UserEmail.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@Action", log.Action);
        command.Parameters.AddWithValue("@Role", log.Role);
        command.Parameters.AddWithValue("@Details", (object?)log.Details ?? DBNull.Value);
        command.Parameters.AddWithValue("@IpAddress", (object?)log.IpAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("@Timestamp", log.Timestamp);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<AuditReportResponseDto> GetAuditReportAsync(AuditReportRequestDto request, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            whereClauses.Add("Role = @Role");
            parameters.Add(new MySqlParameter("@Role", request.Role.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            whereClauses.Add("Action = @Action");
            parameters.Add(new MySqlParameter("@Action", request.Action.Trim()));
        }

        if (request.FromDate.HasValue)
        {
            whereClauses.Add("Timestamp >= @FromDate");
            parameters.Add(new MySqlParameter("@FromDate", request.FromDate.Value));
        }

        if (request.ToDate.HasValue)
        {
            whereClauses.Add("Timestamp <= @ToDate");
            parameters.Add(new MySqlParameter("@ToDate", request.ToDate.Value));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty;

        var querySql = $@"
            SELECT Id, UserId, UserEmail, Action, Role, Details, IpAddress, Timestamp
            FROM UserAuditLogs
            {whereSql}
            ORDER BY Timestamp DESC
            LIMIT @Limit;";

        await using var queryCommand = new MySqlCommand(querySql, connection);
        foreach (var param in parameters)
        {
            queryCommand.Parameters.Add(param);
        }
        queryCommand.Parameters.AddWithValue("@Limit", Math.Min(request.Limit, 500));

        var report = new AuditReportResponseDto();
        await using var reader = await queryCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var log = new AuditLogDto
            {
                Id = Guid.Parse(reader.GetString("Id")),
                UserId = reader.IsDBNull("UserId") ? null : Guid.Parse(reader.GetString("UserId")),
                UserEmail = reader.GetString("UserEmail"),
                Action = reader.GetString("Action"),
                Role = reader.GetString("Role"),
                Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                IpAddress = reader.IsDBNull("IpAddress") ? null : reader.GetString("IpAddress"),
                Timestamp = reader.GetDateTime("Timestamp")
            };

            report.Logs.Add(log);
            report.TotalEvents++;

            if (log.Action == "REGISTER") report.RegistrationCount++;
            else if (log.Action == "LOGIN_SUCCESS") report.SuccessfulLogins++;
            else if (log.Action == "LOGIN_FAILED") report.FailedLogins++;
        }

        return report;
    }
}
