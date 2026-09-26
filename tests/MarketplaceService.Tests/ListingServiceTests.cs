using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;
using EcoTrack.MarketplaceService.Services;
using Moq;
using Xunit;

namespace EcoTrack.MarketplaceService.Tests;

/// <summary>
/// Service-layer unit tests for ListingService.
/// Tests 6-7 from the subtask (non-recycler cannot create, only owner can update/remove)
/// are controller/integration-level concerns (role-based authorization + ownership checks)
/// and are not testable at the service layer, which has no role or ownership logic.
/// </summary>
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

    private static CreateListingRequestDto ValidCreateDto(string title = "Refurbished Laptop", decimal price = 15000m)
        => new() { ValuationId = Guid.NewGuid(), Title = title, Description = "Test desc", Price = price, PhotoPath = null };

    private static UpdateListingRequestDto ValidUpdateDto(string? title = "Updated Laptop", decimal? price = 20000m)
        => new() { Title = title, Price = price, Description = null, PhotoPath = null, Status = null };

    private static ListingResponseDto MakeListing(Guid valuationId, Guid? id = null, string status = "Available")
    {
        var listingId = id ?? Guid.NewGuid();
        var pickupItemId = Guid.NewGuid();
        return new ListingResponseDto
        {
            Id = listingId,
            ValuationId = valuationId,
            RecyclerId = Guid.NewGuid(),
            Title = "Refurbished Laptop",
            Description = "Test desc",
            Price = 15000m,
            PhotoPath = null,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Valuation = new ValuationResponseDto
            {
                Id = valuationId,
                PickupItemId = pickupItemId,
                RecyclerId = Guid.NewGuid(),
                Price = 15000m,
                Condition = "Good",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ItemName = "Test Item",
                Quantity = 1
            },
            ItemName = "Test Item",
            ItemQuantity = 1
        };
    }

    // ── CreateListingAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateListing_ValidRequest_Returns201AndAvailableStatus()
    {
        var dto = ValidCreateDto();
        var valuationId = dto.ValuationId;
        var valuation = new ValuationResponseDto
        {
            Id = valuationId,
            PickupItemId = Guid.NewGuid(),
            RecyclerId = Guid.NewGuid(),
            Price = 15000m,
            Condition = "Good",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ItemName = "Dell Laptop",
            Quantity = 1
        };
        var createdListing = MakeListing(valuationId);

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(valuationId, _ct)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetValuationByIdAsync(valuationId, _ct)).ReturnsAsync(valuation);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct))
            .ReturnsAsync((ListingResponseDto l, CancellationToken _) =>
            {
                l.Id = Guid.NewGuid(); // simulate DB assigning ID
                return true;
            });

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Listing created successfully.", message);
        Assert.NotNull(listing);
        Assert.Equal(valuationId, listing.ValuationId);
        Assert.Equal(dto.Title, listing.Title);
        Assert.Equal(dto.Price, listing.Price);
        Assert.Equal("Available", listing.Status);
        Assert.NotNull(listing.Valuation);
    }

    [Fact]
    public async Task CreateListing_MissingTitle_Returns400AndCreatesNothing()
    {
        var dto = ValidCreateDto(title: "");
        var valuationId = dto.ValuationId;

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Title is required.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_WhitespaceTitle_Returns400AndCreatesNothing()
    {
        var dto = ValidCreateDto(title: "   ");
        var valuationId = dto.ValuationId;

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Title is required.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_ZeroPrice_Returns400AndCreatesNothing()
    {
        var dto = ValidCreateDto(price: 0m);
        var valuationId = dto.ValuationId;

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price must be greater than 0.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_NegativePrice_Returns400AndCreatesNothing()
    {
        var dto = ValidCreateDto(price: -500m);
        var valuationId = dto.ValuationId;

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price must be greater than 0.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_NoValuation_Returns404AndCreatesNothing()
    {
        var dto = ValidCreateDto();
        var valuationId = dto.ValuationId;

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(false);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("No valuation found", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_DuplicateListing_Returns409AndCreatesNothing()
    {
        var dto = ValidCreateDto();
        var valuationId = dto.ValuationId;

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(valuationId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Contains("already exists", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_ValuationNotFound_Returns404()
    {
        var dto = ValidCreateDto();
        var valuationId = dto.ValuationId;

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(valuationId, _ct)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetValuationByIdAsync(valuationId, _ct)).ReturnsAsync((ValuationResponseDto?)null);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("Valuation not found", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateListing_RepoCreateFails_Returns500()
    {
        var dto = ValidCreateDto();
        var valuationId = dto.ValuationId;
        var valuation = new ValuationResponseDto
        {
            Id = valuationId,
            PickupItemId = Guid.NewGuid(),
            RecyclerId = Guid.NewGuid(),
            Price = 15000m,
            Condition = "Good",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ItemName = "Test",
            Quantity = 1
        };

        _repoMock.Setup(r => r.ValuationExistsAsync(valuationId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.ListingExistsForValuationAsync(valuationId, _ct)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetValuationByIdAsync(valuationId, _ct)).ReturnsAsync(valuation);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ListingResponseDto>(), _ct)).ReturnsAsync(false);

        var (success, statusCode, message, listing) = await _service.CreateListingAsync(
            valuationId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Contains("Failed to create", message);
        Assert.Null(listing);
    }

    // ── UpdateListingAsync ──────────────────────────────────────────────

    [Fact]
    public async Task UpdateListing_ValidRequest_Returns200AndUpdatesFields()
    {
        var listingId = Guid.NewGuid();
        var valuationId = Guid.NewGuid();
        var existing = MakeListing(valuationId, id: listingId);
        var dto = ValidUpdateDto(title: "Updated Title", price: 25000m);

        _repoMock.Setup(r => r.UpdateAsync(listingId, It.IsAny<UpdateListingRequestDto>(), _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByIdAsync(listingId, _ct))
            .ReturnsAsync((Guid id, CancellationToken ct) =>
            {
                var updated = MakeListing(valuationId, id: listingId);
                updated.Title = "Updated Title";
                updated.Price = 25000m;
                return updated;
            });

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            listingId, dto, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Listing updated successfully.", message);
        Assert.NotNull(listing);
        Assert.Equal("Updated Title", listing.Title);
        Assert.Equal(25000m, listing.Price);
        _repoMock.Verify(r => r.UpdateAsync(listingId, It.IsAny<UpdateListingRequestDto>(), _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateListing_NotFound_Returns404()
    {
        var listingId = Guid.NewGuid();
        var dto = ValidUpdateDto();

        _repoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync((ListingResponseDto?)null);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            listingId, dto, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("Listing not found", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateListingRequestDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateListing_MissingTitle_Returns400()
    {
        var listingId = Guid.NewGuid();
        var valuationId = Guid.NewGuid();
        var existing = MakeListing(valuationId, id: listingId);
        var dto = new UpdateListingRequestDto { Title = "", Price = null };

        _repoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(existing);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            listingId, dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Title is required.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateListingRequestDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateListing_ZeroPrice_Returns400()
    {
        var listingId = Guid.NewGuid();
        var valuationId = Guid.NewGuid();
        var existing = MakeListing(valuationId, id: listingId);
        var dto = new UpdateListingRequestDto { Title = null, Price = 0m };

        _repoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(existing);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            listingId, dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price must be greater than 0.", message);
        Assert.Null(listing);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateListingRequestDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateListing_RepoUpdateFails_Returns500()
    {
        var listingId = Guid.NewGuid();
        var valuationId = Guid.NewGuid();
        var existing = MakeListing(valuationId, id: listingId);
        var dto = ValidUpdateDto();

        _repoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(listingId, It.IsAny<UpdateListingRequestDto>(), _ct)).ReturnsAsync(false);

        var (success, statusCode, message, listing) = await _service.UpdateListingAsync(
            listingId, dto, _ct);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Contains("Failed to update", message);
        Assert.Null(listing);
    }

    // ── GetListingByIdAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetListing_Existing_Returns200()
    {
        var listingId = Guid.NewGuid();
        var valuationId = Guid.NewGuid();
        var listing = MakeListing(valuationId, id: listingId);

        _repoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync(listing);

        var (success, statusCode, message, result) = await _service.GetListingByIdAsync(listingId, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Listing retrieved.", message);
        Assert.NotNull(result);
        Assert.Equal(listingId, result.Id);
    }

    [Fact]
    public async Task GetListing_NotFound_Returns404()
    {
        var listingId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByIdAsync(listingId, _ct)).ReturnsAsync((ListingResponseDto?)null);

        var (success, statusCode, message, result) = await _service.GetListingByIdAsync(listingId, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("Listing not found", message);
        Assert.Null(result);
    }

    // ── BrowseListingsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task BrowseListings_ReturnsListings()
    {
        var listings = new List<ListingResponseDto>
        {
            MakeListing(Guid.NewGuid(), id: Guid.NewGuid(), "Available"),
            MakeListing(Guid.NewGuid(), id: Guid.NewGuid(), "Available")
        };

        _repoMock.Setup(r => r.BrowseAsync(null, 1, 10, _ct)).ReturnsAsync(listings);

        var (success, statusCode, message, result, totalCount, page, pageSize) = await _service.BrowseListingsAsync(
            null, 1, 10, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Listings retrieved.", message);
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2, totalCount);
        Assert.Equal(1, page);
        Assert.Equal(10, pageSize);
    }

    [Fact]
    public async Task BrowseListings_WithKeyword_PassesToRepository()
    {
        var listings = new List<ListingResponseDto>
        {
            MakeListing(Guid.NewGuid(), id: Guid.NewGuid(), "Available")
        };

        _repoMock.Setup(r => r.BrowseAsync("Laptop", 1, 10, _ct)).ReturnsAsync(listings);

        var (success, statusCode, message, result, totalCount, page, pageSize) = await _service.BrowseListingsAsync(
            "Laptop", 1, 10, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.NotNull(result);
        _repoMock.Verify(r => r.BrowseAsync("Laptop", 1, 10, _ct), Times.Once);
    }

    [Fact]
    public async Task BrowseListings_PageSizeCappedAt50()
    {
        var listings = new List<ListingResponseDto>();
        _repoMock.Setup(r => r.BrowseAsync(null, 1, 50, _ct)).ReturnsAsync(listings);

        _ = await _service.BrowseListingsAsync(null, 1, 100, _ct);

        _repoMock.Verify(r => r.BrowseAsync(null, 1, 50, _ct), Times.Once);
        _repoMock.Verify(r => r.BrowseAsync(null, 1, 100, _ct), Times.Never);
    }

    [Fact]
    public async Task BrowseListings_InvalidPageClampedTo1()
    {
        var listings = new List<ListingResponseDto>();
        _repoMock.Setup(r => r.BrowseAsync(null, 1, 10, _ct)).ReturnsAsync(listings);

        _ = await _service.BrowseListingsAsync(null, 0, 10, _ct);

        _repoMock.Verify(r => r.BrowseAsync(null, 1, 10, _ct), Times.Once);
        _repoMock.Verify(r => r.BrowseAsync(null, 0, 10, _ct), Times.Never);
    }

    // ── DeleteListingAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteListing_Existing_Returns200()
    {
        var listingId = Guid.NewGuid();

        _repoMock.Setup(r => r.DeleteAsync(listingId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message) = await _service.DeleteListingAsync(listingId, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Listing removed successfully.", message);
        _repoMock.Verify(r => r.DeleteAsync(listingId, _ct), Times.Once);
    }

    [Fact]
    public async Task DeleteListing_NotFound_Returns404()
    {
        var listingId = Guid.NewGuid();

        _repoMock.Setup(r => r.DeleteAsync(listingId, _ct)).ReturnsAsync(false);

        var (success, statusCode, message) = await _service.DeleteListingAsync(listingId, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("Listing not found", message);
        _repoMock.Verify(r => r.DeleteAsync(listingId, _ct), Times.Once);
    }
}
