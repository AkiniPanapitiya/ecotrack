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
    private readonly Mock<IAuditRepository> _auditRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditRepositoryMock = new Mock<IAuditRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _auditRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object);
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

        _auditRepositoryMock.Setup(a => a.LogActivityAsync(It.IsAny<UserAuditLog>(), It.IsAny<CancellationToken>()))
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
}
