using System.Data;
using EcoTrack.IdentityService.Data;
using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using MySqlConnector;

namespace EcoTrack.IdentityService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> CreateRecyclerProfileAsync(RecyclerProfile profile, CancellationToken cancellationToken = default);
    Task<RecyclerProfile?> GetRecyclerProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateRecyclerProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, FullName, Email, PasswordHash, Role, PhoneNumber, Address, IsActive, CreatedAt, UpdatedAt
            FROM Users
            WHERE Email = @Email
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Email", email.Trim().ToLowerInvariant());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, FullName, Email, PasswordHash, Role, PhoneNumber, Address, IsActive, CreatedAt, UpdatedAt
            FROM Users
            WHERE Id = @Id
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapUser(reader);
        }

        return null;
    }

    public async Task<bool> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, PhoneNumber, Address, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Id, @FullName, @Email, @PasswordHash, @Role, @PhoneNumber, @Address, @IsActive, @CreatedAt, @UpdatedAt);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", user.Id.ToString());
        command.Parameters.AddWithValue("@FullName", user.FullName.Trim());
        command.Parameters.AddWithValue("@Email", user.Email.Trim().ToLowerInvariant());
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@Role", user.Role);
        command.Parameters.AddWithValue("@PhoneNumber", (object?)user.PhoneNumber ?? DBNull.Value);
        command.Parameters.AddWithValue("@Address", (object?)user.Address ?? DBNull.Value);
        command.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", user.UpdatedAt);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> CreateRecyclerProfileAsync(RecyclerProfile profile, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            INSERT INTO RecyclerProfiles (Id, UserId, CompanyName, BusinessRegistrationNumber, FacilityAddress, OperationalCapacityKg, VerificationStatus, CreatedAt, UpdatedAt)
            VALUES (@Id, @UserId, @CompanyName, @BusinessRegistrationNumber, @FacilityAddress, @OperationalCapacityKg, @VerificationStatus, @CreatedAt, @UpdatedAt);";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", profile.Id.ToString());
        command.Parameters.AddWithValue("@UserId", profile.UserId.ToString());
        command.Parameters.AddWithValue("@CompanyName", profile.CompanyName.Trim());
        command.Parameters.AddWithValue("@BusinessRegistrationNumber", profile.BusinessRegistrationNumber.Trim());
        command.Parameters.AddWithValue("@FacilityAddress", profile.FacilityAddress.Trim());
        command.Parameters.AddWithValue("@OperationalCapacityKg", profile.OperationalCapacityKg);
        command.Parameters.AddWithValue("@VerificationStatus", profile.VerificationStatus);
        command.Parameters.AddWithValue("@CreatedAt", profile.CreatedAt);
        command.Parameters.AddWithValue("@UpdatedAt", profile.UpdatedAt);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<RecyclerProfile?> GetRecyclerProfileByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT Id, UserId, CompanyName, BusinessRegistrationNumber, FacilityAddress, OperationalCapacityKg, VerificationStatus, CreatedAt, UpdatedAt
            FROM RecyclerProfiles
            WHERE UserId = @UserId
            LIMIT 1;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId.ToString());

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return new RecyclerProfile
            {
                Id = Guid.Parse(reader.GetString("Id")),
                UserId = Guid.Parse(reader.GetString("UserId")),
                CompanyName = reader.GetString("CompanyName"),
                BusinessRegistrationNumber = reader.GetString("BusinessRegistrationNumber"),
                FacilityAddress = reader.GetString("FacilityAddress"),
                OperationalCapacityKg = reader.GetDecimal("OperationalCapacityKg"),
                VerificationStatus = reader.GetString("VerificationStatus"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                UpdatedAt = reader.GetDateTime("UpdatedAt")
            };
        }

        return null;
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE Users
            SET FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                Address = @Address,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", userId.ToString());
        command.Parameters.AddWithValue("@FullName", dto.FullName.Trim());
        command.Parameters.AddWithValue("@PhoneNumber", (object?)dto.PhoneNumber?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@Address", (object?)dto.Address?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateRecyclerProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            UPDATE RecyclerProfiles
            SET CompanyName = COALESCE(@CompanyName, CompanyName),
                FacilityAddress = COALESCE(@FacilityAddress, FacilityAddress),
                OperationalCapacityKg = COALESCE(@OperationalCapacityKg, OperationalCapacityKg),
                UpdatedAt = @UpdatedAt
            WHERE UserId = @UserId;";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId.ToString());
        command.Parameters.AddWithValue("@CompanyName", (object?)dto.CompanyName?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@FacilityAddress", (object?)dto.FacilityAddress?.Trim() ?? DBNull.Value);
        command.Parameters.AddWithValue("@OperationalCapacityKg", (object?)dto.OperationalCapacityKg ?? DBNull.Value);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
        return rowsAffected > 0;
    }

    private static User MapUser(MySqlDataReader reader)
    {
        return new User
        {
            Id = Guid.Parse(reader.GetString("Id")),
            FullName = reader.GetString("FullName"),
            Email = reader.GetString("Email"),
            PasswordHash = reader.GetString("PasswordHash"),
            Role = reader.GetString("Role"),
            PhoneNumber = reader.IsDBNull("PhoneNumber") ? null : reader.GetString("PhoneNumber"),
            Address = reader.IsDBNull("Address") ? null : reader.GetString("Address"),
            IsActive = reader.GetBoolean("IsActive"),
            CreatedAt = reader.GetDateTime("CreatedAt"),
            UpdatedAt = reader.GetDateTime("UpdatedAt")
        };
    }
}
