using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;

namespace EcoTrack.IdentityService.Services;

public interface IAuthService
{
    Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> RegisterAsync(
        RegisterRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> LoginAsync(
        LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IAuditRepository auditRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
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

        // 3. Create User record with hashed password
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

        // 5. Audit Log write
        await _auditRepository.LogActivityAsync(new UserAuditLog
        {
            UserId = user.Id,
            UserEmail = user.Email,
            Action = "REGISTER",
            Role = user.Role,
            Details = $"Registered new account as {user.Role}. Recycler status: {verificationStatus ?? "N/A"}",
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

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

    public async Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> LoginAsync(
        LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        // 1. Fetch user by email
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !user.IsActive)
        {
            // Scenario 3: Non-existing email returns 401 "Invalid credentials."
            await _auditRepository.LogActivityAsync(new UserAuditLog
            {
                UserId = null,
                UserEmail = request.Email,
                Action = "LOGIN_FAILED",
                Role = "Unknown",
                Details = "Failed login attempt: non-existing or inactive email.",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            return (false, 401, "Invalid credentials.", null);
        }

        // 2. Verify password with BCrypt
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            // Scenario 2: Incorrect password returns 401 "Invalid credentials."
            await _auditRepository.LogActivityAsync(new UserAuditLog
            {
                UserId = user.Id,
                UserEmail = user.Email,
                Action = "LOGIN_FAILED",
                Role = user.Role,
                Details = "Failed login attempt: incorrect password.",
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            return (false, 401, "Invalid credentials.", null);
        }

        // 3. Fetch recycler profile verification status if recycler
        string? verificationStatus = null;
        if (string.Equals(user.Role, "Recycler", StringComparison.OrdinalIgnoreCase))
        {
            var profile = await _userRepository.GetRecyclerProfileByUserIdAsync(user.Id, cancellationToken);
            verificationStatus = profile?.VerificationStatus ?? "Pending";
        }

        // 4. Generate JWT Token
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, verificationStatus);

        // 5. Audit Log write
        await _auditRepository.LogActivityAsync(new UserAuditLog
        {
            UserId = user.Id,
            UserEmail = user.Email,
            Action = "LOGIN_SUCCESS",
            Role = user.Role,
            Details = $"Successful login from {ipAddress ?? "local"}.",
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Token = token,
            ExpiresAt = expiresAt,
            VerificationStatus = verificationStatus,
            Message = "Login successful."
        };

        return (true, 200, "Login successful.", response);
    }
}
