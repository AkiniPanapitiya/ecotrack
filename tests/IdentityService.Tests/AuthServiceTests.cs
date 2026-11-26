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
        _userRepositoryMock.Verify(r => r.CreateUserAsync(
            It.Is<User>(u =>
                u.PasswordHash == "hashed_secure_password_string" &&
                u.FullName == "Akini Panapitiya" &&
                u.Email == "akini@ecotrack.lk" &&
                u.Role == "User" &&
                u.PhoneNumber == "+94771234567" &&
                u.Address == "Colombo, Sri Lanka" &&
                u.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);

        _auditRepositoryMock.Verify(a => a.LogActivityAsync(
            It.Is<UserAuditLog>(l => l.Action == "REGISTER" && l.Role == "User"),
            It.IsAny<CancellationToken>()), Times.Once);
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

        _auditRepositoryMock.Verify(a => a.LogActivityAsync(
            It.Is<UserAuditLog>(l => l.Action == "LOGIN_SUCCESS" && l.Role == "User"),
            It.IsAny<CancellationToken>()), Times.Once);
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

        _auditRepositoryMock.Verify(a => a.LogActivityAsync(
            It.Is<UserAuditLog>(l => l.Action == "LOGIN_FAILED" && l.Role == "User"),
            It.IsAny<CancellationToken>()), Times.Once);
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

        _auditRepositoryMock.Verify(a => a.LogActivityAsync(
            It.Is<UserAuditLog>(l => l.Action == "LOGIN_FAILED" && l.Role == "Unknown"),
            It.IsAny<CancellationToken>()), Times.Once);
    }


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
        Assert.Equal("Failed to log out. Please try again.", message);
    }

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
            Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidUnexpiredToken_UpdatesPasswordAndMarksTokenUsed()
    {
        var userId = Guid.NewGuid();
        _passwordResetTokenRepositoryMock
            .Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((userId, DateTime.UtcNow.AddMinutes(15), false));
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

    [Fact]
    public async Task RegisterAsync_UserCreationFails_Returns500InternalServerError()
    {
        var request = new RegisterRequestDto
        {
            FullName = "Failed User",
            Email = "fail@ecotrack.lk",
            Password = "Password@123",
            Role = "User"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("hashed_pwd");
        _userRepositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var (success, statusCode, message, response) = await _authService.RegisterAsync(request);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Failed to create user account.", message);
        Assert.Null(response);
    }

    [Fact]
    public async Task RegisterAsync_RecyclerWithEmptyDetails_UsesDefaultValues()
    {
        var request = new RegisterRequestDto
        {
            FullName = "Sunil Perera",
            Email = "sunil@ecotrack.lk",
            Password = "Password@123",
            Role = "Recycler",
            CompanyName = " ",
            BusinessRegistrationNumber = "",
            FacilityAddress = null,
            OperationalCapacityKg = null,
            Address = null
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("hashed_pwd");
        _userRepositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(r => r.CreateRecyclerProfileAsync(It.IsAny<RecyclerProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (success, statusCode, _, response) = await _authService.RegisterAsync(request);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.NotNull(response);
        _userRepositoryMock.Verify(r => r.CreateRecyclerProfileAsync(
            It.Is<RecyclerProfile>(p =>
                p.CompanyName == "Sunil Perera" &&
                p.BusinessRegistrationNumber == "PENDING-REG" &&
                p.FacilityAddress == "Pending Facility Address" &&
                p.OperationalCapacityKg == 0.00m &&
                p.VerificationStatus == "Pending"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_RecyclerWithUserAddressOnly_UsesUserAddressAsFacilityAddress()
    {
        var request = new RegisterRequestDto
        {
            FullName = "Kamal Gunaratne",
            Email = "kamal@ecotrack.lk",
            Password = "Password@123",
            Role = "Recycler",
            CompanyName = "Kamal Eco",
            BusinessRegistrationNumber = "BR-100",
            FacilityAddress = null,
            Address = "12 Colombo Road, Negombo"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasherMock.Setup(h => h.HashPassword(request.Password))
            .Returns("hashed_pwd");
        _userRepositoryMock.Setup(r => r.CreateUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(r => r.CreateRecyclerProfileAsync(It.IsAny<RecyclerProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (success, _, _, _) = await _authService.RegisterAsync(request);

        Assert.True(success);
        _userRepositoryMock.Verify(r => r.CreateRecyclerProfileAsync(
            It.Is<RecyclerProfile>(p => p.FacilityAddress == "12 Colombo Road, Negombo"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_Returns401InvalidCredentials()
    {
        var request = new LoginRequestDto
        {
            Email = "inactive@ecotrack.lk",
            Password = "Password@123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@ecotrack.lk",
            PasswordHash = "hashed_pwd",
            Role = "User",
            IsActive = false
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var (success, statusCode, message, response) = await _authService.LoginAsync(request);

        Assert.False(success);
        Assert.Equal(401, statusCode);
        Assert.Equal("Invalid credentials.", message);
        Assert.Null(response);
        _passwordHasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_RecyclerRole_FetchesProfileAndPassesVerificationStatus()
    {
        var request = new LoginRequestDto
        {
            Email = "recycler@ecotrack.lk",
            Password = "Password@123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Recycler One",
            Email = "recycler@ecotrack.lk",
            PasswordHash = "hashed_pwd",
            Role = "Recycler",
            IsActive = true
        };

        var profile = new RecyclerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            VerificationStatus = "Verified"
        };

        _userRepositoryMock.Setup(r => r.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(h => h.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        _userRepositoryMock.Setup(r => r.GetRecyclerProfileByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var expires = DateTime.UtcNow.AddHours(2);
        _jwtTokenServiceMock.Setup(j => j.GenerateToken(user, "Verified"))
            .Returns(("recycler.jwt.token", expires));

        var (success, statusCode, _, response) = await _authService.LoginAsync(request);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.NotNull(response);
        Assert.Equal("recycler.jwt.token", response.Token);
        Assert.Equal("Verified", response.VerificationStatus);
        _userRepositoryMock.Verify(r => r.GetRecyclerProfileByUserIdAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _jwtTokenServiceMock.Verify(j => j.GenerateToken(user, "Verified"), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_AddTokenFails_Returns500()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "test@ecotrack.lk" };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordResetTokenRepositoryMock.Setup(r => r.AddAsync(user.Id, It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new ForgotPasswordRequestDto { Email = user.Email };

        var (success, statusCode, message) = await _authService.ForgotPasswordAsync(request);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Something went wrong. Please try again.", message);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatePasswordFails_Returns500()
    {
        var userId = Guid.NewGuid();
        _passwordResetTokenRepositoryMock.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((userId, DateTime.UtcNow.AddMinutes(15), false));
        _passwordHasherMock.Setup(h => h.HashPassword(It.IsAny<string>()))
            .Returns("new_hash");
        _userRepositoryMock.Setup(r => r.UpdatePasswordAsync(userId, "new_hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new ResetPasswordRequestDto
        {
            Token = "valid-token",
            NewPassword = "NewPass123",
            ConfirmPassword = "NewPass123"
        };

        var (success, statusCode, message) = await _authService.ResetPasswordAsync(request);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Something went wrong. Please try again.", message);
    }
}
 
