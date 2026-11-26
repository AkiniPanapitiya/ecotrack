using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;
using EcoTrack.IdentityService.Services;
using Moq;
using Xunit;

namespace EcoTrack.IdentityService.Tests;

public class ProfileServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuditRepository> _auditRepositoryMock;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditRepositoryMock = new Mock<IAuditRepository>();

        _profileService = new ProfileService(
            _userRepositoryMock.Object,
            _auditRepositoryMock.Object);
    }

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsUserProfileDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "Akini Panapitiya",
            Email = "akini@ecotrack.lk",
            Role = "User",
            PhoneNumber = "+94771234567",
            Address = "Colombo, Sri Lanka",
            IsActive = true
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var profile = await _profileService.GetProfileAsync(userId);

        // Assert
        Assert.NotNull(profile);
        Assert.Equal(userId, profile.Id);
        Assert.Equal("Akini Panapitiya", profile.FullName);
        Assert.Equal("akini@ecotrack.lk", profile.Email);
        Assert.Equal("+94771234567", profile.PhoneNumber);
    }

    [Fact]
    public async Task UpdateProfileAsync_ValidUpdate_UpdatesRecordAndAudits()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FullName = "Akini Old",
            Email = "akini@ecotrack.lk",
            Role = "User"
        };

        var updateDto = new UpdateProfileDto
        {
            FullName = "Akini Updated",
            PhoneNumber = "+94779998888",
            Address = "Kandy, Sri Lanka"
        };

        var updatedUser = new User
        {
            Id = userId,
            FullName = updateDto.FullName,
            Email = "akini@ecotrack.lk",
            Role = "User",
            PhoneNumber = updateDto.PhoneNumber,
            Address = updateDto.Address
        };

        _userRepositoryMock.SetupSequence(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user)
            .ReturnsAsync(updatedUser);

        _userRepositoryMock.Setup(r => r.UpdateProfileAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _auditRepositoryMock.Setup(a => a.LogActivityAsync(It.IsAny<UserAuditLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (success, statusCode, message, profile) = await _profileService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Profile updated successfully.", message);
        Assert.NotNull(profile);
        Assert.Equal("Akini Updated", profile.FullName);
        Assert.Equal("+94779998888", profile.PhoneNumber);

        _auditRepositoryMock.Verify(a => a.LogActivityAsync(
            It.Is<UserAuditLog>(l => l.Action == "PROFILE_UPDATE" && l.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_EmptyName_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateProfileDto
        {
            FullName = "", // Missing required field
            PhoneNumber = "+94779998888"
        };

        // Act
        var (success, statusCode, message, profile) = await _profileService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Name is required.", message);
        Assert.Null(profile);

        _userRepositoryMock.Verify(r => r.UpdateProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Advanced SE3112 Edge Cases & Parameterized Theories ──────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateProfileAsync_InvalidFullNameInputs_Returns400BadRequest(string? invalidName)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateDto = new UpdateProfileDto
        {
            FullName = invalidName!,
            PhoneNumber = "+94771234567"
        };

        // Act
        var (success, statusCode, message, profile) = await _profileService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Name is required.", message);
        Assert.Null(profile);

        _userRepositoryMock.Verify(r => r.UpdateProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetProfileAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _profileService.GetProfileAsync(nonExistentId);

        // Assert
        Assert.Null(result);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_RecyclerUser_IncludesRecyclerProfileDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recyclerUser = new User
        {
            Id = userId,
            FullName = "Eco Recycler Corp",
            Email = "recycler@ecotrack.lk",
            Role = "Recycler",
            IsActive = true
        };

        var profile = new RecyclerProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyName = "Eco Recycler Corp",
            BusinessRegistrationNumber = "BR-98765",
            FacilityAddress = "Colombo Industrial Zone",
            OperationalCapacityKg = 5000m,
            VerificationStatus = "Verified"
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recyclerUser);
        _userRepositoryMock.Setup(r => r.GetRecyclerProfileByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        // Act
        var result = await _profileService.GetProfileAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Recycler", result.Role);
        Assert.NotNull(result.RecyclerProfile);
        Assert.Equal("BR-98765", result.RecyclerProfile.BusinessRegistrationNumber);
        Assert.Equal("Verified", result.RecyclerProfile.VerificationStatus);
    }

    [Fact]
    public async Task UpdateProfileAsync_UserNotFound_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateDto = new UpdateProfileDto { FullName = "Valid Name" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var (success, statusCode, message, profile) = await _profileService.UpdateProfileAsync(nonExistentId, updateDto);

        // Assert
        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("User not found.", message);
        Assert.Null(profile);

        _userRepositoryMock.Verify(r => r.UpdateProfileAsync(It.IsAny<Guid>(), It.IsAny<UpdateProfileDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAsync_DatabaseUpdateFails_Returns500InternalServerError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, FullName = "Old Name", Email = "test@ecotrack.lk", Role = "User" };
        var updateDto = new UpdateProfileDto { FullName = "New Name" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateProfileAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Simulate DB failure

        // Act
        var (success, statusCode, message, profile) = await _profileService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Failed to update profile.", message);
        Assert.Null(profile);

        _auditRepositoryMock.Verify(a => a.LogActivityAsync(It.IsAny<UserAuditLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAsync_RecyclerUser_UpdatesBothUserProfileAndRecyclerProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recyclerUser = new User
        {
            Id = userId,
            FullName = "Old Recycler",
            Email = "rec@ecotrack.lk",
            Role = "Recycler"
        };

        var updateDto = new UpdateProfileDto
        {
            FullName = "Updated Recycler",
            FacilityAddress = "New Facility 101"
        };

        var updatedUser = new User
        {
            Id = userId,
            FullName = "Updated Recycler",
            Email = "rec@ecotrack.lk",
            Role = "Recycler"
        };

        _userRepositoryMock.SetupSequence(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recyclerUser)
            .ReturnsAsync(updatedUser);

        _userRepositoryMock.Setup(r => r.UpdateProfileAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(r => r.UpdateRecyclerProfileAsync(userId, updateDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (success, statusCode, message, profile) = await _profileService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.NotNull(profile);

        _userRepositoryMock.Verify(r => r.UpdateProfileAsync(userId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateRecyclerProfileAsync(userId, updateDto, It.IsAny<CancellationToken>()), Times.Once);
    }
}
