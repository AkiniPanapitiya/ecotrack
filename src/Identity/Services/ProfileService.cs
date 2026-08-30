using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;

namespace EcoTrack.IdentityService.Services;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(bool Success, int StatusCode, string Message, UserProfileDto? Profile)> UpdateProfileAsync(
        Guid userId, UpdateProfileDto dto, string? ipAddress = null, CancellationToken cancellationToken = default);
}

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;

    public ProfileService(IUserRepository userRepository, IAuditRepository auditRepository)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
    }

    public async Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) return null;

        var profileDto = new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        if (string.Equals(user.Role, "Recycler", StringComparison.OrdinalIgnoreCase))
        {
            var recycler = await _userRepository.GetRecyclerProfileByUserIdAsync(userId, cancellationToken);
            if (recycler != null)
            {
                profileDto.RecyclerProfile = new RecyclerProfileDto
                {
                    Id = recycler.Id,
                    CompanyName = recycler.CompanyName,
                    BusinessRegistrationNumber = recycler.BusinessRegistrationNumber,
                    FacilityAddress = recycler.FacilityAddress,
                    OperationalCapacityKg = recycler.OperationalCapacityKg,
                    VerificationStatus = recycler.VerificationStatus
                };
            }
        }

        return profileDto;
    }

    public async Task<(bool Success, int StatusCode, string Message, UserProfileDto? Profile)> UpdateProfileAsync(
        Guid userId, UpdateProfileDto dto, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        // Scenario 3: Missing Required Field (Name is required)
        if (string.IsNullOrWhiteSpace(dto.FullName))
        {
            return (false, 400, "Name is required.", null);
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return (false, 404, "User not found.", null);
        }

        // Update User table
        var userUpdated = await _userRepository.UpdateProfileAsync(userId, dto, cancellationToken);
        if (!userUpdated)
        {
            return (false, 500, "Failed to update profile.", null);
        }

        // If Recycler, update RecyclerProfile table
        if (string.Equals(user.Role, "Recycler", StringComparison.OrdinalIgnoreCase))
        {
            await _userRepository.UpdateRecyclerProfileAsync(userId, dto, cancellationToken);
        }

        // Log audit event
        await _auditRepository.LogActivityAsync(new UserAuditLog
        {
            UserId = user.Id,
            UserEmail = user.Email,
            Action = "PROFILE_UPDATE",
            Role = user.Role,
            Details = $"Updated profile information (Name: {dto.FullName}, Phone: {dto.PhoneNumber}, Address: {dto.Address}).",
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        var updatedProfile = await GetProfileAsync(userId, cancellationToken);
        return (true, 200, "Profile updated successfully.", updatedProfile);
    }
}
