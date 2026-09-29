using EcoTrack.MarketplaceService.DTOs;
using EcoTrack.MarketplaceService.Repositories;
using EcoTrack.MarketplaceService.Services;
using Moq;
using Xunit;

namespace EcoTrack.MarketplaceService.Tests;

public class ValuationServiceTests
{
    private readonly Mock<IValuationRepository> _repoMock;
    private readonly ValuationService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public ValuationServiceTests()
    {
        _repoMock = new Mock<IValuationRepository>();
        _service = new ValuationService(_repoMock.Object);
    }

    private static CreateValuationRequestDto ValidDto(decimal price = 5000m, string condition = "Good") =>
        new() { PickupItemId = Guid.NewGuid(), Price = price, Condition = condition };

    private static UpdateValuationRequestDto UpdateDto(decimal price = 7500m, string condition = "Fair") =>
        new() { Price = price, Condition = condition };

    private static ValuationResponseDto MakeValuation(Guid pickupItemId, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            PickupItemId = pickupItemId,
            RecyclerId = Guid.NewGuid(),
            Price = 5000m,
            Condition = "Good",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

    private static void SetupOwnershipTrue(Mock<IValuationRepository> repo, Guid pickupItemId)
    {
        repo.Setup(r => r.IsPickupItemOwnedByRecyclerAsync(pickupItemId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private static void SetupOwnershipFalse(Mock<IValuationRepository> repo, Guid pickupItemId)
    {
        repo.Setup(r => r.IsPickupItemOwnedByRecyclerAsync(pickupItemId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    // ── CreateValuationAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateValuation_ValidRequest_Returns201()
    {
        var dto = ValidDto();
        var pickupItemId = dto.PickupItemId;
        var recyclerId = Guid.NewGuid().ToString();

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync((ValuationResponseDto?)null);
        SetupOwnershipTrue(_repoMock, pickupItemId);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct))
            .ReturnsAsync((ValuationResponseDto v, CancellationToken _) => v);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, recyclerId, _ct);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Valuation created successfully.", message);
        Assert.NotNull(valuation);
        Assert.Equal(pickupItemId, valuation.PickupItemId);
        Assert.Equal(dto.Price, valuation.Price);
        Assert.Equal(dto.Condition, valuation.Condition);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Once);
    }

    [Fact]
    public async Task CreateValuation_MissingPrice_Returns400AndSavesNothing()
    {
        var dto = ValidDto(price: 0m);
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price is required.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_NegativePrice_Returns400AndSavesNothing()
    {
        var dto = ValidDto(price: -100m);
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price must be greater than 0.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Theory]
    [InlineData("Excellent")]
    [InlineData("Damaged")]
    [InlineData("")]
    [InlineData("good")] // case-sensitive
    public async Task CreateValuation_InvalidCondition_Returns400AndSavesNothing(string condition)
    {
        var dto = ValidDto(condition: condition);
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Condition must be Good, Fair, or Poor.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_ItemNotFound_Returns404()
    {
        var dto = ValidDto();
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(false);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("not found", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_DuplicateValuation_Returns409()
    {
        var dto = ValidDto();
        var pickupItemId = dto.PickupItemId;
        var existing = MakeValuation(pickupItemId);

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(existing);
        SetupOwnershipTrue(_repoMock, pickupItemId);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Contains("already been valued", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_RepoFailure_Returns500()
    {
        var dto = ValidDto();
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync((ValuationResponseDto?)null);
        SetupOwnershipTrue(_repoMock, pickupItemId);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct)).ReturnsAsync((ValuationResponseDto?)null);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Contains("Failed to create", message);
        Assert.Null(valuation);
    }

    // TC-28-05: Price boundary validation

    [Fact]
    public async Task CreateValuation_ExcessivePrice_Returns400()
    {
        var dto = ValidDto(price: 99999999999999m); // far above 999,999,999.99 max
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price exceeds the maximum allowed value.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_ExcessiveDecimalPlaces_Returns400()
    {
        var dto = ValidDto(price: 1000.12345m); // 5 decimal places
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price can have at most 2 decimal places.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_MaxAllowedPrice_Returns201()
    {
        var dto = ValidDto(price: 999999999.99m); // exactly at the boundary
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync((ValuationResponseDto?)null);
        SetupOwnershipTrue(_repoMock, pickupItemId);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct))
            .ReturnsAsync((ValuationResponseDto v, CancellationToken _) => v);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.NotNull(valuation);
    }

    [Fact]
    public async Task CreateValuation_TwoDecimalPlaces_Returns201()
    {
        var dto = ValidDto(price: 1000.99m); // exactly 2 decimal places
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync((ValuationResponseDto?)null);
        SetupOwnershipTrue(_repoMock, pickupItemId);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct))
            .ReturnsAsync((ValuationResponseDto v, CancellationToken _) => v);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.True(success);
        Assert.Equal(201, statusCode);
    }

    // TC-28-06: Ownership check

    [Fact]
    public async Task CreateValuation_NotOwner_Returns403()
    {
        var dto = ValidDto();
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        SetupOwnershipFalse(_repoMock, pickupItemId);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Equal("You can only value items from your own pickup requests.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_NotOwner_DoesNotCreateValuation()
    {
        var dto = ValidDto();
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        SetupOwnershipFalse(_repoMock, pickupItemId);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task CreateValuation_Owner_Returns201()
    {
        var dto = ValidDto();
        var pickupItemId = dto.PickupItemId;

        _repoMock.Setup(r => r.PickupItemExistsAsync(pickupItemId, _ct)).ReturnsAsync(true);
        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync((ValuationResponseDto?)null);
        SetupOwnershipTrue(_repoMock, pickupItemId);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct))
            .ReturnsAsync((ValuationResponseDto v, CancellationToken _) => v);

        var (success, statusCode, message, valuation) = await _service.CreateValuationAsync(
            pickupItemId, dto, Guid.NewGuid().ToString(), _ct);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<ValuationResponseDto>(), _ct), Times.Once);
    }

    // ── UpdateValuationAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateValuation_ValidRequest_Returns200AndUpdatesPrice()
    {
        var pickupItemId = Guid.NewGuid();
        var existing = MakeValuation(pickupItemId, id: Guid.NewGuid());
        var dto = UpdateDto(price: 7500m, condition: "Fair");

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(existing.Id, dto, _ct)).ReturnsAsync(true);

        var (success, statusCode, message, valuation) = await _service.UpdateValuationAsync(
            pickupItemId, dto, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Valuation updated successfully.", message);
        Assert.NotNull(valuation);
        Assert.Equal(7500m, valuation.Price);
        Assert.Equal("Fair", valuation.Condition);
        _repoMock.Verify(r => r.UpdateAsync(existing.Id, dto, _ct), Times.Once);
    }

    [Fact]
    public async Task UpdateValuation_MissingPrice_Returns400()
    {
        var pickupItemId = Guid.NewGuid();
        var existing = MakeValuation(pickupItemId);
        var dto = new UpdateValuationRequestDto { Price = 0m, Condition = "Good" };

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(existing);

        var (success, statusCode, message, valuation) = await _service.UpdateValuationAsync(
            pickupItemId, dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price is required.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateValuationRequestDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateValuation_InvalidCondition_Returns400()
    {
        var pickupItemId = Guid.NewGuid();
        var existing = MakeValuation(pickupItemId);
        var dto = new UpdateValuationRequestDto { Price = 5000m, Condition = "Broken" };

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(existing);

        var (success, statusCode, message, valuation) = await _service.UpdateValuationAsync(
            pickupItemId, dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Condition must be Good, Fair, or Poor.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateValuationRequestDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateValuation_NotFound_Returns404()
    {
        var pickupItemId = Guid.NewGuid();
        var dto = UpdateDto(price: 5000m, condition: "Good");

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync((ValuationResponseDto?)null);

        var (success, statusCode, message, valuation) = await _service.UpdateValuationAsync(
            pickupItemId, dto, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("No valuation found", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateValuationRequestDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateValuation_RepoFailure_Returns500()
    {
        var pickupItemId = Guid.NewGuid();
        var existing = MakeValuation(pickupItemId, id: Guid.NewGuid());
        var dto = UpdateDto(price: 5000m, condition: "Good");

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(existing.Id, dto, _ct)).ReturnsAsync(false);

        var (success, statusCode, message, valuation) = await _service.UpdateValuationAsync(
            pickupItemId, dto, _ct);

        Assert.False(success);
        Assert.Equal(500, statusCode);
        Assert.Contains("Failed to update", message);
        Assert.Null(valuation);
    }

    // TC-28-05 (update): price boundary validation on update

    [Fact]
    public async Task UpdateValuation_ExcessivePrice_Returns400()
    {
        var pickupItemId = Guid.NewGuid();
        var existing = MakeValuation(pickupItemId, id: Guid.NewGuid());
        var dto = new UpdateValuationRequestDto { Price = 99999999999999m, Condition = "Good" };

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(existing);

        var (success, statusCode, message, valuation) = await _service.UpdateValuationAsync(
            pickupItemId, dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price exceeds the maximum allowed value.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateValuationRequestDto>(), _ct), Times.Never);
    }

    [Fact]
    public async Task UpdateValuation_ExcessiveDecimalPlaces_Returns400()
    {
        var pickupItemId = Guid.NewGuid();
        var existing = MakeValuation(pickupItemId, id: Guid.NewGuid());
        var dto = new UpdateValuationRequestDto { Price = 1000.12345m, Condition = "Good" };

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(existing);

        var (success, statusCode, message, valuation) = await _service.UpdateValuationAsync(
            pickupItemId, dto, _ct);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Price can have at most 2 decimal places.", message);
        Assert.Null(valuation);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<UpdateValuationRequestDto>(), _ct), Times.Never);
    }

    // ── GetValuationByPickupItemAsync ─────────────────────────────────

    [Fact]
    public async Task GetValuation_Existing_Returns200()
    {
        var pickupItemId = Guid.NewGuid();
        var valuation = MakeValuation(pickupItemId);

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync(valuation);

        var (success, statusCode, message, result) = await _service.GetValuationByPickupItemAsync(
            pickupItemId, _ct);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Valuation retrieved.", message);
        Assert.NotNull(result);
        Assert.Equal(pickupItemId, result.PickupItemId);
    }

    [Fact]
    public async Task GetValuation_NotFound_Returns404()
    {
        var pickupItemId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, _ct)).ReturnsAsync((ValuationResponseDto?)null);

        var (success, statusCode, message, result) = await _service.GetValuationByPickupItemAsync(
            pickupItemId, _ct);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("No valuation found", message);
        Assert.Null(result);
    }
}
