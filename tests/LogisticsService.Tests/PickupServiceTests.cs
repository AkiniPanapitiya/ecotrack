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
}
