using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;

namespace EcoTrack.IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    private readonly ITokenBlacklistRepository _tokenBlacklistRepository;

    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ITokenBlacklistRepository tokenBlacklistRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _tokenBlacklistRepository = tokenBlacklistRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
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

    public async Task<(bool Success, int StatusCode, string Message, AuthResponseDto? Response)> LoginAsync(
        LoginRequestDto request, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        // 1. Fetch user by email
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return (false, 401, "Invalid credentials.", null);
        }

        // 2. Verify BCrypt password
        var isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return (false, 401, "Invalid credentials.", null);
        }

        // 3. If Recycler, fetch profile verification status
        RecyclerProfile? recyclerProfile = null;
        if (string.Equals(user.Role, "Recycler", StringComparison.OrdinalIgnoreCase))
        {
            recyclerProfile = await _userRepository.GetRecyclerProfileByUserIdAsync(user.Id, cancellationToken);
        }

        // 4. Generate symmetric HMAC-SHA256 JWT token with role claims
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, recyclerProfile?.VerificationStatus);

        var response = new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            Token = token,
            ExpiresAt = expiresAt,
            VerificationStatus = recyclerProfile?.VerificationStatus,
            Message = "Login successful."
        };

        return (true, 200, "Login successful.", response);
    }
    public async Task<(bool Success, int StatusCode, string Message)> LogoutAsync(
        string jti, Guid userId, DateTime tokenExpiresAt, CancellationToken cancellationToken = default)
    {
        var added = await _tokenBlacklistRepository.AddAsync(jti, userId, tokenExpiresAt, cancellationToken);
        if (!added)
        {
            return (false, 500, "Failed to log out. Please try again.");
        }

        return (true, 200, "Logged out successfully.");
    }

    public async Task<(bool Success, int StatusCode, string Message)> ForgotPasswordAsync(
    ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        const string genericMessage = "Check your email for reset instructions.";

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Security: always return the same success message, whether the email
        // exists or not — never reveal which emails are registered.
        if (user == null)
        {
            return (true, 200, genericMessage);
        }

        // Generate a random, unguessable raw token (this is what goes in the email link)
        var rawToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        // Hash it before storing — same principle as ECO-63's blacklist
        var tokenHash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(rawToken));
        var tokenHashHex = Convert.ToHexString(tokenHash);

        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        var saved = await _passwordResetTokenRepository.AddAsync(user.Id, tokenHashHex, expiresAt, cancellationToken);
        if (!saved)
        {
            return (false, 500, "Something went wrong. Please try again.");
        }

        // replace with real email sending once an email service exists.
        // For now, log the link so it can be tested manually.
        var resetLink = $"http://localhost:5173/reset-password?token={rawToken}&email={user.Email}";
        Console.WriteLine($"[Password Reset] Link for {user.Email}: {resetLink}");

        return (true, 200, genericMessage);
    }

    public async Task<(bool Success, int StatusCode, string Message)> ResetPasswordAsync(
        ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        // Hash the incoming raw token the same way we hashed it when creating it
        var tokenHashBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(request.Token));
        var tokenHashHex = Convert.ToHexString(tokenHashBytes);

        var tokenRecord = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHashHex, cancellationToken);

        const string invalidMessage = "This reset link is invalid or has expired.";

        if (tokenRecord == null)
        {
            return (false, 400, invalidMessage);
        }

        if (tokenRecord.Value.IsUsed || tokenRecord.Value.ExpiresAt < DateTime.UtcNow)
        {
            return (false, 400, invalidMessage);
        }

        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        var updated = await _userRepository.UpdatePasswordAsync(tokenRecord.Value.UserId, newPasswordHash, cancellationToken);

        if (!updated)
        {
            return (false, 500, "Something went wrong. Please try again.");
        }

        await _passwordResetTokenRepository.MarkAsUsedAsync(tokenHashHex, cancellationToken);

        return (true, 200, "Password reset successful. Please log in.");
    }

}
