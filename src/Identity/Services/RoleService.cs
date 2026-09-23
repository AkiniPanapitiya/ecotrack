using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;

namespace EcoTrack.IdentityService.Services;

public interface IRoleService
{
    Task<(bool Success, int StatusCode, string Message, List<UserListDto>? Users)> GetAllUsersAsync(
        string? roleFilter = null, bool? statusFilter = null,
        string? sortField = null, bool sortDesc = false,
        CancellationToken cancellationToken = default);
    Task<(bool Success, int StatusCode, string Message, ChangeRoleResponseDto? Response)> ChangeUserRoleAsync(
        Guid userId, string adminId, ChangeRoleRequestDto dto, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateActiveStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task LogAuditAsync(string adminId, string details);
}

public class RoleService : IRoleService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private static readonly string[] ValidRoles = { "User", "Recycler", "Admin" };

    public RoleService(IUserRepository userRepository, IAuditRepository auditRepository)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, List<UserListDto>? Users)> GetAllUsersAsync(
        string? roleFilter = null, bool? statusFilter = null,
        string? sortField = null, bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllUsersAsync(roleFilter, statusFilter, sortField, sortDesc, cancellationToken);
        return (true, 200, "Users retrieved successfully.", users);
    }

    public async Task<(bool Success, int StatusCode, string Message, ChangeRoleResponseDto? Response)> ChangeUserRoleAsync(
        Guid userId, string adminId, ChangeRoleRequestDto dto, CancellationToken cancellationToken = default)
    {
        var newRole = dto.NewRole?.Trim();
        if (string.IsNullOrWhiteSpace(newRole) || !ValidRoles.Contains(newRole))
        {
            return (false, 400, "Invalid role. Valid roles: User, Recycler, Admin.", null);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return (false, 404, "User not found.", null);
        }

        var previousRole = user.Role;
        if (previousRole == newRole)
        {
            return (false, 400, $"User already has the '{newRole}' role.", null);
        }

        // Block admin from demoting themselves — prevents lockout
        var adminUser = await _userRepository.GetByIdAsync(Guid.Parse(adminId), cancellationToken);
        if (adminUser != null && adminUser.Id == userId && newRole != "Admin")
        {
            return (false, 403, "You cannot change your own role to a non-admin role. This would lock you out of admin features.", null);
        }

        var success = await _userRepository.UpdateRoleAsync(userId, newRole, cancellationToken);
        if (!success)
        {
            return (false, 500, "Failed to update user role.", null);
        }

        var response = new ChangeRoleResponseDto
        {
            UserId = userId,
            PreviousRole = previousRole,
            NewRole = newRole,
            Message = $"Role changed from '{previousRole}' to '{newRole}' successfully."
        };

        // Audit log
        await _auditRepository.LogActivityAsync(new UserAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Parse(adminId),
            UserEmail = adminUser?.Email ?? "unknown",
            Action = "ROLE_CHANGED",
            Role = "Admin",
            Details = $"Role changed from '{previousRole}' to '{newRole}' for user {user.Email}",
            IpAddress = null,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return (true, 200, "Role updated successfully.", response);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<bool> UpdateActiveStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return false;

        var success = await _userRepository.UpdateActiveStatusAsync(userId, isActive, cancellationToken);
        if (success)
        {
            await _auditRepository.LogActivityAsync(new UserAuditLog
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                UserEmail = user.Email,
                Action = "ACCOUNT_STATUS_CHANGED",
                Role = user.Role,
                Details = $"Account {(isActive ? "activated" : "deactivated")} by admin",
                IpAddress = null,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
        }
        return success;
    }

    public async Task LogAuditAsync(string adminId, string details)
    {
        await _auditRepository.LogActivityAsync(new UserAuditLog
        {
            Id = Guid.NewGuid(),
            UserId = Guid.TryParse(adminId, out var guid) ? guid : Guid.Empty,
            UserEmail = "admin",
            Action = "ADMIN_ACTION",
            Role = "Admin",
            Details = details,
            IpAddress = null,
            Timestamp = DateTime.UtcNow
        }, CancellationToken.None);
    }
}
