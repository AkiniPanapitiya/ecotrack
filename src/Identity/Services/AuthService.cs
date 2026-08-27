using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;

namespace EcoTrack.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> RegisterAsync(
        RegisterRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        // 1. Validation: password length
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return (false, 400, "Password must be at least 8 characters.", null);
        }

        // 2. Duplicate email check (Scenario 3: Return 409 Conflict)
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            return (false, 409, "An account with this email already exists.", null);
        }

        // 3. Create User record with hashed password (BCrypt work factor 11)
        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Role = request.Role,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Address = request.Address?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var userCreated = await _userRepository.CreateUserAsync(user, cancellationToken);
        if (!userCreated)
        {
            return (false, 500, "Failed to create user account.", null);
        }

        string? verificationStatus = null;

        // 4. If Role is Recycler, create RecyclerProfile with default "Pending" status
        if (string.Equals(request.Role, "Recycler", StringComparison.OrdinalIgnoreCase))
        {
            verificationStatus = "Pending";
            var recyclerProfile = new RecyclerProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? user.FullName : request.CompanyName.Trim(),
                BusinessRegistrationNumber = string.IsNullOrWhiteSpace(request.BusinessRegistrationNumber) ? "PENDING-REG" : request.BusinessRegistrationNumber.Trim(),
                FacilityAddress = string.IsNullOrWhiteSpace(request.FacilityAddress) ? (user.Address ?? "Pending Facility Address") : request.FacilityAddress.Trim(),
                OperationalCapacityKg = request.OperationalCapacityKg ?? 0.00m,
                VerificationStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.CreateRecyclerProfileAsync(recyclerProfile, cancellationToken);
        }

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            VerificationStatus = verificationStatus,
            Message = "Account created successfully. Please log in."
        };

        return (true, 201, "Account created successfully. Please log in.", response);
    }
}
