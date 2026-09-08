using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Models;
using EcoTrack.LogisticsService.Repositories;
using EcoTrack.LogisticsService.Services;
using Moq;
using Xunit;

namespace EcoTrack.LogisticsService.Tests;

public class PickupServiceTests
{
    private readonly Mock<IPickupRepository> _pickupRepoMock;
    private readonly PickupService _pickupService;

    public PickupServiceTests()
    {
        _pickupRepoMock = new Mock<IPickupRepository>();
        _pickupService = new PickupService(_pickupRepoMock.Object);
    }

    [Fact]
    public async Task CreatePickupAsync_ValidRequest_Returns201AndScheduledPickup()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreatePickupRequestDto
        {
            Category = "Computing & Laptops",
            EstimatedWeightKg = 25.5m,
            PickupAddress = "No 100, Galle Road, Colombo 03",
            ContactPhone = "+94771234567",
            PreferredDate = DateTime.UtcNow.AddDays(2).Date,
            TimeSlot = "Morning (09:00 - 12:00)",
            SpecialInstructions = "Leave near front desk",
            Items = new List<CreatePickupItemDto>
            {
                new CreatePickupItemDto { ItemName = "Dell Latitude Laptop", Quantity = 2, ItemCondition = "Used", EstimatedWeightKg = 4.0m }
            }
        };

        _pickupRepoMock.Setup(r => r.CreatePickupAsync(It.IsAny<PickupRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (success, statusCode, message, data) = await _pickupService.CreatePickupRequestAsync(userId, dto);

        // Assert
        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.NotNull(data);
        Assert.Equal("Computing & Laptops", data.Category);
        Assert.Equal(25.5m, data.EstimatedWeightKg);
        Assert.Equal("Pending", data.Status);
        Assert.Equal(userId, data.UserId);

        _pickupRepoMock.Verify(r => r.CreatePickupAsync(
            It.Is<PickupRequest>(p => p.UserId == userId && p.Category == "Computing & Laptops" && p.EstimatedWeightKg == 25.5m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePickupAsync_ZeroOrNegativeWeight_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreatePickupRequestDto
        {
            Category = "Mobile & Handhelds",
            EstimatedWeightKg = 0m,
            PickupAddress = "Kandy, Sri Lanka",
            ContactPhone = "+94712345678",
            PreferredDate = DateTime.UtcNow.AddDays(1).Date,
            TimeSlot = "Afternoon (12:00 - 15:00)"
        };

        // Act
        var (success, statusCode, message, data) = await _pickupService.CreatePickupRequestAsync(userId, dto);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Estimated weight must be between 0.1 kg and 10,000 kg.", message);
        Assert.Null(data);

        _pickupRepoMock.Verify(r => r.CreatePickupAsync(It.IsAny<PickupRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePickupAsync_PastDate_Returns400BadRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new CreatePickupRequestDto
        {
            Category = "Home & Office Appliances",
            EstimatedWeightKg = 15.0m,
            PickupAddress = "Galle, Sri Lanka",
            ContactPhone = "+94712345678",
            PreferredDate = DateTime.UtcNow.AddDays(-2).Date,
            TimeSlot = "Morning (09:00 - 12:00)"
        };

        // Act
        var (success, statusCode, message, data) = await _pickupService.CreatePickupRequestAsync(userId, dto);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Preferred pickup date cannot be in the past.", message);
        Assert.Null(data);
    }

    [Fact]
    public async Task GetPickupsByUserAsync_ReturnsUserBookings()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockList = new List<PickupRequest>
        {
            new PickupRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Category = "Batteries & Power Supplies",
                EstimatedWeightKg = 50.0m,
                PickupAddress = "Industrial Zone, Moratuwa",
                ContactPhone = "+94770000000",
                PreferredDate = DateTime.UtcNow.AddDays(3).Date,
                TimeSlot = "Evening (15:00 - 18:00)",
                Status = "Scheduled"
            }
        };

        _pickupRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockList);

        // Act
        var result = (await _pickupService.GetPickupsByUserAsync(userId)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("Batteries & Power Supplies", result[0].Category);
        Assert.Equal(50.0m, result[0].EstimatedWeightKg);
        Assert.Equal("Scheduled", result[0].Status);
    }
    //Schedule Management tests
    [Fact]
    public async Task ConfirmScheduleAsync_NoConflict_UpdatesToScheduledAndReturns200()
    {
        var pickupId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var existingPickup = new PickupRequest { Id = pickupId, Status = "Pending" };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPickup);
        _pickupRepoMock.Setup(r => r.HasConflictAsync(recyclerId, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _pickupRepoMock.Setup(r => r.ConfirmScheduleAsync(pickupId, recyclerId, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new ConfirmScheduleRequestDto
        {
            RecyclerId = recyclerId,
            ScheduledDate = DateTime.UtcNow.AddDays(3).Date,
            ScheduledTimeSlot = "Morning (09:00 - 12:00)"
        };

        var (success, statusCode, message) = await _pickupService.ConfirmScheduleAsync(pickupId, dto);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Pickup scheduled successfully.", message);
    }

    [Fact]
    public async Task ConfirmScheduleAsync_ConflictingSlot_Returns409()
    {
        var pickupId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var existingPickup = new PickupRequest { Id = pickupId, Status = "Pending" };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPickup);
        _pickupRepoMock.Setup(r => r.HasConflictAsync(recyclerId, It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);   // pretend this recycler already has this exact slot taken

        var dto = new ConfirmScheduleRequestDto
        {
            RecyclerId = recyclerId,
            ScheduledDate = DateTime.UtcNow.AddDays(3).Date,
            ScheduledTimeSlot = "Morning (09:00 - 12:00)"
        };

        var (success, statusCode, message) = await _pickupService.ConfirmScheduleAsync(pickupId, dto);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("This time slot is already booked.", message);

        _pickupRepoMock.Verify(r => r.ConfirmScheduleAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);   // the actual update should never be attempted if there's a conflict
    }

    [Fact]
    public async Task ConfirmScheduleAsync_PickupNotFound_Returns404()
    {
        var pickupId = Guid.NewGuid();
        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PickupRequest?)null);

        var dto = new ConfirmScheduleRequestDto
        {
            RecyclerId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(1).Date,
            ScheduledTimeSlot = "Afternoon (12:00 - 15:00)"
        };

        var (success, statusCode, message) = await _pickupService.ConfirmScheduleAsync(pickupId, dto);

        Assert.False(success);
        Assert.Equal(404, statusCode);
    }

    [Fact]
    public async Task ConfirmScheduleAsync_PastDate_Returns400()
    {
        var pickupId = Guid.NewGuid();
        var existingPickup = new PickupRequest { Id = pickupId, Status = "Pending" };
        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPickup);

        var dto = new ConfirmScheduleRequestDto
        {
            RecyclerId = Guid.NewGuid(),
            ScheduledDate = DateTime.UtcNow.AddDays(-1).Date,   // yesterday
            ScheduledTimeSlot = "Morning (09:00 - 12:00)"
        };

        var (success, statusCode, message) = await _pickupService.ConfirmScheduleAsync(pickupId, dto);

        Assert.False(success);
        Assert.Equal(400, statusCode);
    }
    
    //ECO84- Unit tests for Cancel and Reschedule Pickup
    [Fact]
    public async Task CancelPickup_WhenRequested_SetsStatusToCancelled()
    {
        var pickupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pickup = new PickupRequest { Id = pickupId, Status = "Pending", UserId = userId };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pickup);
        _pickupRepoMock.Setup(r => r.UpdateStatusAsync(pickupId, "Cancelled", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _pickupService.CancelPickupAsync(pickupId, userId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ReschedulePickup_WhenScheduled_ResetsToRequestedWithNewDate()
    {
        var pickupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pickup = new PickupRequest { Id = pickupId, Status = "Scheduled", UserId = userId };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pickup);
        _pickupRepoMock.Setup(r => r.RescheduleAsync(pickupId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _pickupService.ReschedulePickupAsync(pickupId, DateTime.Today.AddDays(3), userId);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CancelPickup_WhenAlreadyCollected_ReturnsFailure()
    {
        var pickupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var pickup = new PickupRequest { Id = pickupId, Status = "Collected", UserId = userId };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pickup);

        var result = await _pickupService.CancelPickupAsync(pickupId, userId);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CancelPickup_WhenWrongUser_ReturnsFailure()
    {
        var pickupId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var someoneElseUserId = Guid.NewGuid();
        var pickup = new PickupRequest { Id = pickupId, Status = "Pending", UserId = ownerUserId };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pickup);

        var result = await _pickupService.CancelPickupAsync(pickupId, someoneElseUserId);

        Assert.False(result.Success);
    }
    
    [Fact]
    public async Task GetPickupStatusAsync_ExistingPickup_ReturnsStatus()
    {
        var pickupId = Guid.NewGuid();
        var pickup = new PickupRequest { Id = pickupId, Status = "Scheduled", UpdatedAt = DateTime.UtcNow };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pickup);

        var result = await _pickupService.GetPickupStatusAsync(pickupId);

        Assert.NotNull(result);
        Assert.Equal("Scheduled", result.Status);
    }

    [Fact]
    public async Task GetPickupStatusAsync_NotFound_ReturnsNull()
    {
        var pickupId = Guid.NewGuid();
        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PickupRequest?)null);

        var result = await _pickupService.GetPickupStatusAsync(pickupId);

        Assert.Null(result);
    }
    [Theory]
    [InlineData("Requested")]
    [InlineData("Scheduled")]
    [InlineData("Collected")]
    public async Task GetPickupStatusAsync_ReturnsCorrectStatus_ForEachStage(string status)
    {
        var pickupId = Guid.NewGuid();
        var pickup = new PickupRequest { Id = pickupId, Status = status, UpdatedAt = DateTime.UtcNow };

        _pickupRepoMock.Setup(r => r.GetByIdAsync(pickupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pickup);

        var result = await _pickupService.GetPickupStatusAsync(pickupId);

        Assert.NotNull(result);
        Assert.Equal(status, result.Status);
    }

    // Covers Scenario 3: no pickups yet -> empty list, not an error
    [Fact]
    public async Task GetPickupsByUserAsync_NoPickups_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();

        _pickupRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest>());

        var result = await _pickupService.GetPickupsByUserAsync(userId);

        Assert.Empty(result);
    }
}
