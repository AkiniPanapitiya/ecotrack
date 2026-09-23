using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;
using EcoTrack.IdentityService.Services;
using Moq;
using Xunit;

namespace EcoTrack.IdentityService.Tests;

public class RoleServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuditRepository> _auditRepositoryMock;
    private readonly RoleService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public RoleServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditRepositoryMock = new Mock<IAuditRepository>();
        _service = new RoleService(_userRepositoryMock.Object, _auditRepositoryMock.Object);
    }

    // ── GetAllUsersAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsync_EmptyDatabase_ReturnsEmptyList()
    {
        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(null, null, null, false, _ct))
            .ReturnsAsync(Array.Empty<UserListDto>());

        var (success, statusCode, message, users) = await _service.GetAllUsersAsync(_ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Users retrieved successfully.", message);
        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithUsers_ReturnsUserList()
    {
        var users = new List<UserListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "Alice", Email = "alice@test.com", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), FullName = "Bob", Email = "bob@test.com", Role = "Recycler", IsActive = true, CreatedAt = DateTime.UtcNow },
        };

        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(null, null, null, false, _ct))
            .ReturnsAsync(users);

        var (success, statusCode, message, result) = await _service.GetAllUsersAsync(_ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal(2, result!.Count);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithRoleFilter_ReturnsFilteredUsers()
    {
        var users = new List<UserListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "Alice", Email = "alice@test.com", Role = "Recycler", IsActive = true, CreatedAt = DateTime.UtcNow },
        };

        _userRepositoryMock.Setup(r => r.GetAllUsersAsync("Recycler", null, null, false, _ct))
            .ReturnsAsync(users);

        var (success, statusCode, message, result) = await _service.GetAllUsersAsync(roleFilter: "Recycler", _ct);

        Assert.True(success);
        Assert.Single(result!);
        Assert.Equal("Recycler", result[0].Role);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithStatusFilter_ReturnsFilteredUsers()
    {
        var users = new List<UserListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "Alice", Email = "alice@test.com", Role = "User", IsActive = false, CreatedAt = DateTime.UtcNow },
        };

        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(null, false, null, false, _ct))
            .ReturnsAsync(users);

        var (success, statusCode, message, result) = await _service.GetAllUsersAsync(statusFilter: false, _ct);

        Assert.True(success);
        Assert.Single(result!);
        Assert.False(result[0].IsActive);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithSort_ReturnsSortedUsers()
    {
        var users = new List<UserListDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "B", Email = "b@test.com", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = Guid.NewGuid(), FullName = "A", Email = "a@test.com", Role = "User", IsActive = true, CreatedAt = DateTime.UtcNow.AddDays(-1) },
        };

        _userRepositoryMock.Setup(r => r.GetAllUsersAsync(null, null, "CreatedAt", true, _ct))
            .ReturnsAsync(users);

        var (success, statusCode, message, result) = await _service.GetAllUsersAsync(sortField: "CreatedAt", sortDesc: true, _ct);

        Assert.True(success);
        Assert.Equal(2, result!.Count);
    }

    // ── ChangeUserRoleAsync ─────────────────────────────────────────

    [Fact]
    public async Task ChangeUserRoleAsync_ValidChange_Returns200()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@test.com", Role = "User" };
        var adminUser = new User { Id = adminId, Email = "admin@test.com", Role = "Admin" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id) => id == userId ? user : id == adminId ? adminUser : null);
        _userRepositoryMock.Setup(r => r.UpdateRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), _ct))
            .ReturnsAsync(true);
        _auditRepositoryMock.Setup(r => r.LogActivityAsync(It.IsAny<UserAuditLog>(), _ct))
            .Returns(Task.CompletedTask);

        var dto = new ChangeRoleRequestDto { NewRole = "Recycler" };
        var (success, statusCode, message, response) = await _service.ChangeUserRoleAsync(userId, adminId.ToString(), dto, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Role updated successfully.", message);
        Assert.NotNull(response);
        Assert.Equal("Recycler", response.NewRole);
        _userRepositoryMock.Verify(r => r.UpdateRoleAsync(userId, "Recycler", _ct), Times.Once);
        _auditRepositoryMock.Verify(r => r.LogActivityAsync(It.IsAny<UserAuditLog>(), _ct), Times.Once);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_InvalidRole_Returns400()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@test.com", Role = "User" };
        var adminUser = new User { Id = adminId, Email = "admin@test.com", Role = "Admin" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id) => id == userId ? user : id == adminId ? adminUser : null);

        var dto = new ChangeRoleRequestDto { NewRole = "SuperAdmin" };
        var (success, statusCode, message, response) = await _service.ChangeUserRoleAsync(userId, adminId.ToString(), dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Contains("Invalid role", message);
        Assert.Null(response);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_UserNotFound_Returns404()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var adminUser = new User { Id = adminId, Email = "admin@test.com", Role = "Admin" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id) => id == adminId ? adminUser : null);

        var dto = new ChangeRoleRequestDto { NewRole = "Recycler" };
        var (success, statusCode, message, response) = await _service.ChangeUserRoleAsync(userId, adminId.ToString(), dto, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("not found", message);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_SameRole_Returns400()
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@test.com", Role = "Recycler" };
        var adminUser = new User { Id = adminId, Email = "admin@test.com", Role = "Admin" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id) => id == userId ? user : id == adminId ? adminUser : null);

        var dto = new ChangeRoleRequestDto { NewRole = "Recycler" };
        var (success, statusCode, message, response) = await _service.ChangeUserRoleAsync(userId, adminId.ToString(), dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Contains("already has", message);
    }

    [Fact]
    public async Task ChangeUserRoleAsync_SelfDemotionToNonAdmin_Returns403()
    {
        var userId = Guid.NewGuid();
        var adminId = userId; // same person
        var user = new User { Id = userId, Email = "admin@test.com", Role = "Admin" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), _ct))
            .ReturnsAsync(true);

        var dto = new ChangeRoleRequestDto { NewRole = "User" };
        var (success, statusCode, message, response) = await _service.ChangeUserRoleAsync(userId, adminId.ToString(), dto, _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Contains("own role", message);
        Assert.Null(response);
        _userRepositoryMock.Verify(r => r.UpdateRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), _ct), Times.Never);
    }

    [Theory]
    [InlineData("Recycler")]
    [InlineData("Admin")]
    public async Task ChangeUserRoleAsync_AllValidRoles_Accepted(string role)
    {
        var userId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "user@test.com", Role = "User" };
        var adminUser = new User { Id = adminId, Email = "admin@test.com", Role = "Admin" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id) => id == userId ? user : id == adminId ? adminUser : null);
        _userRepositoryMock.Setup(r => r.UpdateRoleAsync(userId, It.IsAny<string>(), _ct))
            .ReturnsAsync(true);
        _auditRepositoryMock.Setup(r => r.LogActivityAsync(It.IsAny<UserAuditLog>(), _ct))
            .Returns(Task.CompletedTask);

        var dto = new ChangeRoleRequestDto { NewRole = role };
        var (success, statusCode, message, response) = await _service.ChangeUserRoleAsync(userId, adminId.ToString(), dto, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal(role, response!.NewRole);
    }

    // ── GetUserByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct)).ReturnsAsync(user);

        var result = await _service.GetUserByIdAsync(userId, _ct);

        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@test.com", result.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistingUser_ReturnsNull()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct)).ReturnsAsync((User?)null);

        var result = await _service.GetUserByIdAsync(userId, _ct);

        Assert.Null(result);
    }

    // ── UpdateActiveStatusAsync ─────────────────────────────────────

    [Fact]
    public async Task UpdateActiveStatusAsync_ValidUser_ReturnsTrue()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateActiveStatusAsync(userId, false, _ct)).ReturnsAsync(true);
        _auditRepositoryMock.Setup(r => r.LogActivityAsync(It.IsAny<UserAuditLog>(), _ct)).Returns(Task.CompletedTask);

        var result = await _service.UpdateActiveStatusAsync(userId, false, _ct);

        Assert.True(result);
        _userRepositoryMock.Verify(r => r.UpdateActiveStatusAsync(userId, false, _ct), Times.Once);
        _auditRepositoryMock.Verify(r => r.LogActivityAsync(It.IsAny<UserAuditLog>(), _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateActiveStatusAsync_NonExistingUser_ReturnsFalse()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct)).ReturnsAsync((User?)null);

        var result = await _service.UpdateActiveStatusAsync(userId, false, _ct);

        Assert.False(result);
        _userRepositoryMock.Verify(r => r.UpdateActiveStatusAsync(It.IsAny<Guid>(), It.IsAny<bool>(), _ct), Times.Never);
    }

    // ── LogAuditAsync ───────────────────────────────────────────────

    [Fact]
    public async Task LogAuditAsync_CallsRepository()
    {
        var adminId = Guid.NewGuid();
        var details = "Test audit log";

        _auditRepositoryMock.Setup(r => r.LogActivityAsync(It.IsAny<UserAuditLog>(), _ct)).Returns(Task.CompletedTask);

        await _service.LogAuditAsync(adminId.ToString(), details);

        _auditRepositoryMock.Verify(r => r.LogActivityAsync(
            It.Is<UserAuditLog>(a => a.Action == "ADMIN_ACTION" && a.Details == details), _ct), Times.Once);
    }
}
