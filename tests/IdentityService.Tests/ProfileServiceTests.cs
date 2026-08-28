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
}
