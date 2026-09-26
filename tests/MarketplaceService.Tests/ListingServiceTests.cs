using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;
using EcoTrack.MarketplaceService.Services;
using Moq;
using Xunit;

namespace EcoTrack.MarketplaceService.Tests;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _repoMock;
    private readonly ListingService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public ListingServiceTests()
    {
        _repoMock = new Mock<IListingRepository>();
        _service = new ListingService(_repoMock.Object);
    }

    private static CreateListingRequestDto ValidCreateDto(Guid? valuationId = null) => new()
    {
        ValuationId = valuationId ?? Guid.NewGuid(),
        Title = "Refurbished ThinkPad",
        Description = "Core i7, 16GB RAM, 512GB SSD",
        Price = 45000.00m,
        PhotoPath = "/uploads/thinkpad.jpg"
    };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateListing_EmptyTitle_Returns400(string? title)
    {
        var dto = new CreateListingRequestDto
        {
            ValuationId = Guid.NewGuid(),
            Title = title!,
            Price = 15000m
        };

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            dto.ValuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Title is required.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public async Task CreateListing_ZeroOrNegativePrice_Returns400(decimal price)
    {
        var dto = new CreateListingRequestDto
        {
            ValuationId = Guid.NewGuid(),
            Title = "Valid Title",
            Price = price
        };

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            dto.ValuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price must be greater than 0.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_ValuationDoesNotExist_Returns404()
    {
        var dto = ValidCreateDto();

        _repoMock.Setup(r => r.ValuationExistsAsync(dto.ValuationId, _ct))
            .ReturnsAsync(false);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            dto.ValuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("No valuation found for the specified item.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_ListingAlreadyExistsForValuation_Returns409()
    {
        var dto = ValidCreateDto();

        _repoMock.Setup(r => r.ValuationExistsAsync(dto.ValuationId, _ct))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(dto.ValuationId, _ct))
            .ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            dto.ValuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("A listing already exists for this valuation. Use PUT to update it.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_ValuationNotFoundOnGet_Returns404()
    {
        var dto = ValidCreateDto();

        _repoMock.Setup(r => r.ValuationExistsAsync(dto.ValuationId, _ct))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(dto.ValuationId, _ct))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.GetValuationByIdAsync(dto.ValuationId, _ct))
            .ReturnsAsync((ValuationResponseDto?)null);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            dto.ValuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Valuation not found.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_RepositoryFailure_Returns500()
    {
        var dto = ValidCreateDto();
        var valuation = new ValuationResponseDto { Id = dto.ValuationId, Price = 40000m };

        _repoMock.Setup(r => r.ValuationExistsAsync(dto.ValuationId, _ct))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(dto.ValuationId, _ct))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.GetValuationByIdAsync(dto.ValuationId, _ct))
            .ReturnsAsync(valuation);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct))
            .ReturnsAsync(false);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            dto.ValuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Failed to create listing.", message);
        Assert.Null(listing);
    }

    [Fact]
    public async Task CreateListing_Success_Returns201()
    {
        var dto = ValidCreateDto();
        var recyclerId = Guid.NewGuid().ToString();
        var valuation = new ValuationResponseDto { Id = dto.ValuationId, Price = 40000m };

        _repoMock.Setup(r => r.ValuationExistsAsync(dto.ValuationId, _ct))
            .ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(dto.ValuationId, _ct))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.GetValuationByIdAsync(dto.ValuationId, _ct))
            .ReturnsAsync(valuation);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct))
            .ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            dto.ValuationId, dto, recyclerId, _ct);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Listing created successfully.", message);
        Assert.NotNull(listing);
        Assert.Equal(dto.Title, listing.Title);
        Assert.Equal(dto.Price, listing.Price);
        Assert.Equal("Available", listing.Status);
        Assert.Equal(Guid.Parse(recyclerId), listing.RecyclerId);
        Assert.Equal(dto.ValuationId, listing.ValuationId);
    }

    [Fact]
    public async Task UpdateListing_ListingNotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, _ct))
            .ReturnsAsync((ListingResponseDto?)null);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            id, new UpdateListingRequestDto { Title = "New Title" }, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Listing not found.", message);
        Assert.Null(listing);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateListing_EmptyTitle_Returns400(string title)
    {
        var id = Guid.NewGuid();
        var existing = new ListingResponseDto { Id = id, Title = "Original" };
        _repoMock.Setup(r => r.GetByIdAsync(id, _ct))
            .ReturnsAsync(existing);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            id, new UpdateListingRequestDto { Title = title }, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Title is required.", message);
        Assert.Null(listing);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task UpdateListing_ZeroOrNegativePrice_Returns400(decimal price)
    {
        var id = Guid.NewGuid();
        var existing = new ListingResponseDto { Id = id, Title = "Original", Price = 100m };
        _repoMock.Setup(r => r.GetByIdAsync(id, _ct))
            .ReturnsAsync(existing);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            id, new UpdateListingRequestDto { Price = price }, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price must be greater than 0.", message);
        Assert.Null(listing);
    }

    [Fact]
    public async Task UpdateListing_RepositoryFailure_Returns500()
    {
        var id = Guid.NewGuid();
        var existing = new ListingResponseDto { Id = id, Title = "Original", Price = 100m };
        _repoMock.Setup(r => r.GetByIdAsync(id, _ct))
            .ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(id, It.IsAny<UpdateListingRequestDto>(), _ct))
            .ReturnsAsync(false);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            id, new UpdateListingRequestDto { Title = "Updated" }, _ct);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Equal("Failed to update listing.", message);
        Assert.Null(listing);
    }

    [Fact]
    public async Task UpdateListing_Success_Returns200()
    {
        var id = Guid.NewGuid();
        var existing = new ListingResponseDto { Id = id, Title = "Original", Price = 100m };
        var updated = new ListingResponseDto { Id = id, Title = "Updated Title", Price = 150m };

        _repoMock.SetupSequence(r => r.GetByIdAsync(id, _ct))
            .ReturnsAsync(existing)
            .ReturnsAsync(updated);

        _repoMock.Setup(r => r.UpdateAsync(id, It.IsAny<UpdateListingRequestDto>(), _ct))
            .ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            id, new UpdateListingRequestDto { Title = "Updated Title", Price = 150m }, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Listing updated successfully.", message);
        Assert.NotNull(listing);
        Assert.Equal("Updated Title", listing.Title);
        Assert.Equal(150m, listing.Price);
    }

    [Fact]
    public async Task GetListingById_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(id, _ct))
            .ReturnsAsync((ListingResponseDto?)null);

        var (success, statusCode, message, listing) = await _service.GetListingByIdAsync(id, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Listing not found.", message);
        Assert.Null(listing);
    }

    [Fact]
    public async Task GetListingById_Success_Returns200()
    {
        var id = Guid.NewGuid();
        var expected = new ListingResponseDto { Id = id, Title = "ThinkPad" };
        _repoMock.Setup(r => r.GetByIdAsync(id, _ct))
            .ReturnsAsync(expected);

        var (success, statusCode, message, listing) = await _service.GetListingByIdAsync(id, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Listing retrieved.", message);
        Assert.Equal(expected, listing);
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-5, 100, 1, 50)]
    public async Task BrowseListings_ClampsPageAndPageSize(int inputPage, int inputPageSize, int expectedPage, int expectedPageSize)
    {
        _repoMock.Setup(r => r.BrowseAsync(It.IsAny<string?>(), expectedPage, expectedPageSize, _ct))
            .ReturnsAsync(new List<ListingResponseDto>());

        var (success, statusCode, message, listings, totalCount, returnedPage, returnedPageSize) =
            await _service.BrowseListingsAsync(null, inputPage, inputPageSize, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal(expectedPage, returnedPage);
        Assert.Equal(expectedPageSize, returnedPageSize);
    }

    [Fact]
    public async Task DeleteListing_NotFound_Returns404()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.DeleteAsync(id, _ct))
            .ReturnsAsync(false);

        var (success, statusCode, message) = await _service.DeleteListingAsync(id, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Listing not found.", message);
    }

    [Fact]
    public async Task DeleteListing_Success_Returns200()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.DeleteAsync(id, _ct))
            .ReturnsAsync(true);

        var (success, statusCode, message) = await _service.DeleteListingAsync(id, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Listing removed successfully.", message);
    }
}
