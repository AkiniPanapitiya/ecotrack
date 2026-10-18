using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;
using EcoTrack.IdentityService.Services;
using Moq;
using Xunit;

namespace EcoTrack.IdentityService.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ITokenBlacklistRepository> _tokenBlacklistRepositoryMock;
    private readonly Mock<IPasswordResetTokenRepository> _passwordResetTokenRepositoryMock;
    private readonly Mock<IAuditRepository> _auditRepositoryMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _tokenBlacklistRepositoryMock = new Mock<ITokenBlacklistRepository>();
        _passwordResetTokenRepositoryMock = new Mock<IPasswordResetTokenRepository>();
        _auditRepositoryMock = new Mock<IAuditRepository>();
        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _tokenBlacklistRepositoryMock.Object,
            _passwordResetTokenRepositoryMock.Object,
            _auditRepositoryMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidUserRequest_Returns201AndHashedPassword()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Akini Panapitiya",
            Email = "akini@ecotrack.lk",
            Password = "Password@123",
            Role = "User",
            PhoneNumber = "+94771234567",
            Address = "Colombo, Sri Lanka"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("hashed_secure_password_string");

        _userRepositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (success, statusCode, message, response) = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Account created successfully. Please log in.", message);
        Assert.NotNull(response);
        Assert.Equal("Akini Panapitiya", response.FullName);
        Assert.Equal("akini@ecotrack.lk", response.Email);
        Assert.Equal("User", response.Role);

        _passwordHasherMock.Verify(h => h.HashPassword("Password@123"), Times.Once);
        _userRepositoryMock.Verify(r => r.CreateUserAsync(It.Is<User>(u => u.PasswordHash == "hashed_secure_password_string"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Duplicate User",
            Email = "existing@ecotrack.lk",
            Password = "Password@123",
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Email = request.Email });

        // Act
        var (success, statusCode, message, response) = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("An account with this email already exists.", message);
        Assert.Null(response);

        _userRepositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_RecyclerRole_SetsPendingVerificationStatus()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Green Yard Recyclers",
            Email = "greenyard@ecotrack.lk",
            Password = "SecurePassword@123",
            Role = "Recycler",
            CompanyName = "Green Yard Ltd",
            BusinessRegistrationNumber = "BR-98765",
            FacilityAddress = "Industrial Zone, Kaduwela",
            OperationalCapacityKg = 5000.00m
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("hashed_pwd");

        _userRepositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock.Setup(r => r.CreateRecyclerProfileAsync(It.IsAny<RecyclerProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (success, statusCode, message, response) = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.NotNull(response);
        Assert.Equal("Pending", response.VerificationStatus);

        _userRepositoryMock.Verify(r => r.CreateRecyclerProfileAsync(
            It.Is<RecyclerProfile>(p => p.VerificationStatus == "Pending" && p.CompanyName == "Green Yard Ltd"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_Returns200AndValidJwt()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "akini@ecotrack.lk",
            Password = "Password@123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Akini Panapitiya",
            Email = "akini@ecotrack.lk",
            PasswordHash = "$2a$11$mockhash",
            Role = "User",
            IsActive = true
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(h => h.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);

        var expires = DateTime.UtcNow.AddHours(2);
        _jwtTokenServiceMock.Setup(j => j.GenerateToken(user, null))
            .Returns(("mock.jwt.token.string", expires));

        // Act
        var (success, statusCode, message, response) = await _authService.LoginAsync(request);

        // Assert
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.NotNull(response);
        Assert.Equal("mock.jwt.token.string", response.Token);
        Assert.Equal("User", response.Role);
        Assert.Equal("Login successful.", response.Message);
    }

    [Fact]
    public async Task LoginAsync_IncorrectPassword_Returns401InvalidCredentials()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "akini@ecotrack.lk",
            Password = "WrongPassword!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "akini@ecotrack.lk",
            PasswordHash = "$2a$11$mockhash",
            Role = "User",
            IsActive = true
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock.Setup(h => h.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var (success, statusCode, message, response) = await _authService.LoginAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal(401, statusCode);
        Assert.Equal("Invalid credentials.", message);
        Assert.Null(response);
    }

    [Fact]
    public async Task LoginAsync_NonExistingEmail_Returns401InvalidCredentials()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "nonexisting@ecotrack.lk",
            Password = "SomePassword@123"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var (success, statusCode, message, response) = await _authService.LoginAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal(401, statusCode);
        Assert.Equal("Invalid credentials.", message);
        Assert.Null(response);
    }


//Logout Tests
    [Fact]
public async Task LogoutAsync_ValidJti_AddsToBlacklistAndReturns200()
{
    var jti = Guid.NewGuid().ToString();
    var userId = Guid.NewGuid();
    var expiresAt = DateTime.UtcNow.AddMinutes(30);

    _tokenBlacklistRepositoryMock
        .Setup(r => r.AddAsync(jti, userId, expiresAt, It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

    var (success, statusCode, message) = await _authService.LogoutAsync(jti, userId, expiresAt);

    Assert.True(success);
    Assert.Equal(200, statusCode);
    Assert.Equal("Logged out successfully.", message);
}

    [Fact]
    public async Task LogoutAsync_RepositoryFails_Returns500()
    {
        var jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        _tokenBlacklistRepositoryMock
            .Setup(r => r.AddAsync(jti, userId, expiresAt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var (success, statusCode, message) = await _authService.LogoutAsync(jti, userId, expiresAt);

        Assert.False(success);
        Assert.Equal(500, statusCode);
    }

//Forgot Password Tests
    [Fact]
    public async Task ForgotPasswordAsync_ExistingEmail_SavesTokenAndReturnsGenericMessage()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "testecotrack@gmail.com" };
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordResetTokenRepositoryMock
            .Setup(r => r.AddAsync(user.Id, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new ForgotPasswordRequestDto { Email = user.Email };

        var (success, statusCode, message) = await _authService.ForgotPasswordAsync(request);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Check your email for reset instructions.", message);
        _passwordResetTokenRepositoryMock.Verify(
            r => r.AddAsync(user.Id, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_NonExistentEmail_ReturnsSameGenericMessage()
    {
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new ForgotPasswordRequestDto { Email = "doesnotexist@gmail.com" };

        var (success, statusCode, message) = await _authService.ForgotPasswordAsync(request);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Check your email for reset instructions.", message);
        _passwordResetTokenRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);   // no token should ever be created for an email that doesn't exist
    }

//Reset Password Tests

    [Fact]
    public async Task ResetPasswordAsync_ValidUnexpiredToken_UpdatesPasswordAndMarksTokenUsed()
    {
        var userId = Guid.NewGuid();
        _passwordResetTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((userId, DateTime.UtcNow.AddMinutes(15), false));   // not used and not expired
        _passwordHasherMock
            .Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("hashed_new_password");
        _userRepositoryMock
            .Setup(r => r.UpdatePasswordAsync(userId, "hashed_new_password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new ResetPasswordRequestDto
        {
            Token = "raw-token-value",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };

        var (success, statusCode, message) = await _authService.ResetPasswordAsync(request);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Password reset successful. Please log in.", message);
        _passwordResetTokenRepositoryMock.Verify(
            r => r.MarkAsUsedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    [Fact]
    public async Task ResetPasswordAsync_TokenNotFound_ReturnsInvalidMessage()
    {
        _passwordResetTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((Guid, DateTime, bool)?)null);

        var request = new ResetPasswordRequestDto
        {
            Token = "does-not-exist",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };

        var (success, statusCode, message) = await _authService.ResetPasswordAsync(request);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("This reset link is invalid or has expired.", message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ReturnsInvalidMessage()
    {
        var userId = Guid.NewGuid();
        _passwordResetTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((userId, DateTime.UtcNow.AddMinutes(-5), false));

        var request = new ResetPasswordRequestDto
        {
            Token = "expired-token",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };

        var (success, statusCode, message) = await _authService.ResetPasswordAsync(request);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("This reset link is invalid or has expired.", message);
    }

    [Fact]
    public async Task ResetPasswordAsync_AlreadyUsedToken_ReturnsInvalidMessage()
    {
        var userId = Guid.NewGuid();
        _passwordResetTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((userId, DateTime.UtcNow.AddMinutes(10), true));

        var request = new ResetPasswordRequestDto
        {
            Token = "already-used-token",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };

        var (success, statusCode, message) = await _authService.ResetPasswordAsync(request);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("This reset link is invalid or has expired.", message);
    }

    // ── Advanced SE3112 Parameterized Theories & Moq Verification ────

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1234567")]
    [InlineData("short")]
    [InlineData("a@B1")]
    public async Task RegisterAsync_PasswordShorterThan8Chars_Returns400BadRequest(string invalidPassword)
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Boundary Test User",
            Email = "boundary@ecotrack.lk",
            Password = invalidPassword,
            Role = "User"
        };

        // Act
        var (success, statusCode, message, response) = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Password must be at least 8 characters.", message);
        Assert.Null(response);

        // Verify behavioral isolation: no hashing or database creation attempts occurred
        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _userRepositoryMock.Verify(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_DatabaseWriteFails_Returns500InternalServerError()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Database Fail User",
            Email = "dbfail@ecotrack.lk",
            Password = "Password@123",
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("hashed_secure_password");
        _userRepositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate DB failure

        // Act
        var (success, statusCode, message, response) = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Failed to create user account.", message);
        Assert.Null(response);

        // Verify audit log was not written if user creation failed
        _auditRepositoryMock.Verify(a => a.LogActivityAsync(It.IsAny<UserAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_RecyclerWithNullOptionalFields_SetsDomainDefaultsCorrectly()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Green Recycler",
            Email = "green@ecotrack.lk",
            Password = "Password@123",
            Role = "Recycler",
            CompanyName = null,
            BusinessRegistrationNumber = null,
            FacilityAddress = null,
            OperationalCapacityKg = null
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("hashed_pwd");
        _userRepositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(r => r.CreateRecyclerProfileAsync(It.IsAny<RecyclerProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (success, statusCode, message, response) = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.NotNull(response);
        Assert.Equal("Pending", response.VerificationStatus);

        // Verify fallback defaults were applied to RecyclerProfile
        _userRepositoryMock.Verify(r => r.CreateRecyclerProfileAsync(
            It.Is<RecyclerProfile>(p =>
                p.CompanyName == "Green Recycler" &&
                p.BusinessRegistrationNumber == "PENDING-REG" &&
                p.OperationalCapacityKg == 0.00m &&
                p.VerificationStatus == "Pending"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InactiveUserAccount_LogsFailedAuditAndReturns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "deactivated@ecotrack.lk",
            Password = "Password@123"
        };

        var inactiveUser = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = "some_hash",
            Role = "User",
            IsActive = false
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveUser);

        // Act
        var (success, statusCode, message, response) = await _authService.LoginAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal(401, statusCode);
        Assert.Equal("Invalid credentials.", message);
        Assert.Null(response);

        // Verify password check was never reached
        _passwordHasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        // Verify audit log was recorded for login failure
        _auditRepositoryMock.Verify(a => a.LogActivityAsync(
            It.Is<UserAuditLog>(log => log.Action == "LOGIN_FAILED" && log.UserEmail == request.Email),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_RepositoryThrowsException_PropagatesFaultAndDoesNotGenerateToken()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "error@ecotrack.lk",
            Password = "Password@123"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Database connection timeout"));

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() => _authService.LoginAsync(request));

        // Verify token generation was never invoked
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_TokenBlacklistRepositoryFails_Returns500InternalServerError()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        _tokenBlacklistRepositoryMock.Setup(b => b.AddAsync(jti, userId, expiresAt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate repository failure

        // Act
        var (success, statusCode, message) = await _authService.LogoutAsync(jti, userId, expiresAt);

        // Assert
        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Failed to log out. Please try again.", message);
    }

    [Fact]
    public async Task ResetPasswordAsync_DatabaseUpdatePasswordFails_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _passwordResetTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((userId, DateTime.UtcNow.AddMinutes(15), false));

        _passwordHasherMock.Setup(h => h.HashPassword("NewStrongPass@123"))
            .Returns("new_hashed_pwd");

        _userRepositoryMock.Setup(r => r.UpdatePasswordAsync(userId, "new_hashed_pwd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate DB failure

        var request = new ResetPasswordRequestDto
        {
            Token = "valid-reset-token-xyz",
            NewPassword = "NewStrongPass@123",
            ConfirmPassword = "NewStrongPass@123"
        };

        // Act
        var (success, statusCode, message) = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Something went wrong. Please try again.", message);

        // Verify token was NOT marked as used if password update failed
        _passwordResetTokenRepositoryMock.Verify(r => r.MarkAsUsedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
