using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Events;
using EcoTrack.LogisticsService.Messaging;
using EcoTrack.LogisticsService.Models;
using EcoTrack.LogisticsService.Repositories;
using EcoTrack.LogisticsService.Services;
using Moq;
using Xunit;

namespace EcoTrack.LogisticsService.Tests;

// ECO-144: Unit tests - Drop-off & Dispatch Record API (ECO-26 / E4.2)
public class DropoffServiceTests
{
    private readonly Mock<IDropoffRepository> _dropoffRepoMock;
    private readonly Mock<IPickupRepository> _pickupRepoMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly DropoffService _dropoffService;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _recyclerId = Guid.NewGuid();
    private readonly Guid _pickupId = Guid.NewGuid();

    public DropoffServiceTests()
    {
        _dropoffRepoMock = new Mock<IDropoffRepository>();
        _pickupRepoMock = new Mock<IPickupRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _dropoffService = new DropoffService(_dropoffRepoMock.Object, _pickupRepoMock.Object, _eventPublisherMock.Object);
    }

    private PickupRequest BuildPickup(string status = "Scheduled") => new()
    {
        Id = _pickupId,
        UserId = _userId,
        RecyclerId = _recyclerId,
        Category = "Computing & Laptops",
        EstimatedWeightKg = 12.5m,
        PickupAddress = "No 100, Galle Road, Colombo 03",
        ContactPhone = "0771234567",
        PreferredDate = DateTime.UtcNow.Date,
        ScheduledDate = DateTime.UtcNow.Date,
        TimeSlot = "Morning (09:00 - 12:00)",
        ScheduledTimeSlot = "Morning (09:00 - 12:00)",
        Status = status,
        Items = new List<PickupItem>
        {
            new() { Id = Guid.NewGuid(), PickupRequestId = _pickupId, ItemName = "Dell Laptop", Quantity = 2 }
        }
    };

    private static CreateDropoffRecordRequestDto ValidDto() => new()
    {
        DropoffDate = DateTime.UtcNow.Date,
        DropoffTime = "14:30",
        ConditionNotes = "  Screen cracked, battery swollen  "
    };

    private void SetupPickup(PickupRequest? pickup) =>
        _pickupRepoMock.Setup(r => r.GetByIdAsync(_pickupId, It.IsAny<CancellationToken>())).ReturnsAsync(pickup);

    // ---- Scenario 1: Record a drop-off ----

    [Fact]
    public async Task RecordDropoffAsync_ValidRequest_Returns201AndMarksCollected()
    {
        SetupPickup(BuildPickup());
        _dropoffRepoMock.Setup(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (success, statusCode, message, record) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, ValidDto());

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Drop-off recorded. Pickup marked as Collected.", message);
        Assert.NotNull(record);
        Assert.Equal(_pickupId, record.PickupRequestId);
        Assert.Equal(_recyclerId, record.RecordedBy);
        Assert.Equal(DateTime.UtcNow.Date, record.DropoffDate);
        Assert.Equal("14:30", record.DropoffTime);
        Assert.Equal("Screen cracked, battery swollen", record.ConditionNotes);
        Assert.Equal("Collected", record.PickupStatus);
    }

    [Fact]
    public async Task RecordDropoffAsync_ValidRequest_SavesRecordLinkedToPickup()
    {
        SetupPickup(BuildPickup());
        DropoffRecord? saved = null;
        _dropoffRepoMock.Setup(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()))
            .Callback<DropoffRecord, CancellationToken>((r, _) => saved = r)
            .ReturnsAsync(true);

        await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, ValidDto());

        Assert.NotNull(saved);
        Assert.Equal(_pickupId, saved.PickupRequestId);
        Assert.Equal(_recyclerId, saved.RecordedBy);
        Assert.Equal(new TimeSpan(14, 30, 0), saved.DropoffTime);
        _dropoffRepoMock.Verify(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordDropoffAsync_ValidRequest_PublishesPickupCollectedEvent()
    {
        SetupPickup(BuildPickup());
        _dropoffRepoMock.Setup(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, ValidDto());

        _eventPublisherMock.Verify(p => p.PublishAsync(
            PickupCollectedEvent.Topic,
            _pickupId.ToString(),
            It.Is<PickupCollectedEvent>(e =>
                e.PickupRequestId == _pickupId.ToString() &&
                e.UserId == _userId.ToString() &&
                e.RecyclerId == _recyclerId.ToString() &&
                e.EstimatedWeightKg == 12.5m &&
                e.ItemCount == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecordDropoffAsync_NoTimeOrNotes_SavesWithNulls()
    {
        SetupPickup(BuildPickup());
        DropoffRecord? saved = null;
        _dropoffRepoMock.Setup(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()))
            .Callback<DropoffRecord, CancellationToken>((r, _) => saved = r)
            .ReturnsAsync(true);

        var dto = new CreateDropoffRecordRequestDto { DropoffDate = DateTime.UtcNow.Date, DropoffTime = " ", ConditionNotes = "   " };
        var (success, statusCode, _, record) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, dto);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Null(saved!.DropoffTime);
        Assert.Null(saved.ConditionNotes);
        Assert.Null(record!.DropoffTime);
    }

    // ---- Scenario 2: Missing drop-off details ----

    [Fact]
    public async Task RecordDropoffAsync_MissingDate_Returns400AndDoesNotSave()
    {
        var dto = new CreateDropoffRecordRequestDto { DropoffDate = null, ConditionNotes = "Fine" };

        var (success, statusCode, message, record) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, dto);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Drop-off date is required.", message);
        Assert.Null(record);
        _dropoffRepoMock.Verify(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()), Times.Never);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordDropoffAsync_DefaultDate_Returns400DateRequired()
    {
        var dto = new CreateDropoffRecordRequestDto { DropoffDate = default(DateTime) };

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, dto);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Drop-off date is required.", message);
    }

    [Fact]
    public async Task RecordDropoffAsync_FutureDate_Returns400()
    {
        var dto = ValidDto();
        dto.DropoffDate = DateTime.UtcNow.Date.AddDays(5);

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, dto);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Drop-off date cannot be in the future.", message);
        _dropoffRepoMock.Verify(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("2:30pm")]
    [InlineData("25:00")]
    [InlineData("abc")]
    public async Task RecordDropoffAsync_InvalidTime_Returns400(string time)
    {
        var dto = ValidDto();
        dto.DropoffTime = time;

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, dto);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Drop-off time must be in HH:mm format.", message);
    }

    [Fact]
    public async Task RecordDropoffAsync_NotesTooLong_Returns400()
    {
        var dto = ValidDto();
        dto.ConditionNotes = new string('x', DropoffService.MaxNotesLength + 1);

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, dto);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Condition notes must not exceed 1000 characters.", message);
    }

    // ---- Pickup state & authorization rules ----

    [Fact]
    public async Task RecordDropoffAsync_PickupNotFound_Returns404()
    {
        SetupPickup(null);

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, ValidDto());

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Pickup request not found.", message);
    }

    [Fact]
    public async Task RecordDropoffAsync_NotAssignedRecycler_Returns403()
    {
        SetupPickup(BuildPickup());

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, Guid.NewGuid(), ValidDto());

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Equal("You are not authorized to record a drop-off for this pickup.", message);
        _dropoffRepoMock.Verify(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordDropoffAsync_AlreadyCollected_Returns409()
    {
        SetupPickup(BuildPickup("Collected"));

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, ValidDto());

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("A drop-off has already been recorded for this pickup.", message);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Cancelled")]
    [InlineData("Requested")]
    public async Task RecordDropoffAsync_PickupNotScheduled_Returns400(string status)
    {
        SetupPickup(BuildPickup(status));

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, ValidDto());

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Only a scheduled pickup can be dropped off.", message);
    }

    [Fact]
    public async Task RecordDropoffAsync_RepositoryRejects_Returns409AndNoEvent()
    {
        SetupPickup(BuildPickup());
        _dropoffRepoMock.Setup(r => r.CreateDropoffAsync(It.IsAny<DropoffRecord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var (success, statusCode, message, _) = await _dropoffService.RecordDropoffAsync(_pickupId, _recyclerId, ValidDto());

        Assert.False(success);
        Assert.Equal(409, statusCode);
        Assert.Equal("This pickup is no longer awaiting drop-off.", message);
        _eventPublisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Scenario 3: View drop-off history ----

    private void SetupRecord(DropoffRecord? record) =>
        _dropoffRepoMock.Setup(r => r.GetByPickupRequestIdAsync(_pickupId, It.IsAny<CancellationToken>())).ReturnsAsync(record);

    private DropoffRecord BuildRecord() => new()
    {
        Id = Guid.NewGuid(),
        PickupRequestId = _pickupId,
        RecordedBy = _recyclerId,
        DropoffDate = new DateTime(2026, 10, 14),
        DropoffTime = new TimeSpan(9, 5, 0),
        ConditionNotes = "Good condition",
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetDropoffAsync_Owner_ReturnsDateAndNotes()
    {
        SetupPickup(BuildPickup("Collected"));
        SetupRecord(BuildRecord());

        var (success, statusCode, _, record) = await _dropoffService.GetDropoffAsync(_pickupId, _userId);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.NotNull(record);
        Assert.Equal(new DateTime(2026, 10, 14), record.DropoffDate);
        Assert.Equal("09:05", record.DropoffTime);
        Assert.Equal("Good condition", record.ConditionNotes);
        Assert.Equal("Collected", record.PickupStatus);
    }

    [Fact]
    public async Task GetDropoffAsync_AssignedRecycler_Returns200()
    {
        SetupPickup(BuildPickup("Collected"));
        SetupRecord(BuildRecord());

        var (success, statusCode, _, _) = await _dropoffService.GetDropoffAsync(_pickupId, _recyclerId);

        Assert.True(success);
        Assert.Equal(200, statusCode);
    }

    [Fact]
    public async Task GetDropoffAsync_UnrelatedUser_Returns403()
    {
        SetupPickup(BuildPickup("Collected"));
        SetupRecord(BuildRecord());

        var (success, statusCode, message, record) = await _dropoffService.GetDropoffAsync(_pickupId, Guid.NewGuid());

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Equal("You are not authorized to view this drop-off record.", message);
        Assert.Null(record);
    }

    [Fact]
    public async Task GetDropoffAsync_PickupNotFound_Returns404()
    {
        SetupPickup(null);

        var (success, statusCode, message, _) = await _dropoffService.GetDropoffAsync(_pickupId, _userId);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("Pickup request not found.", message);
    }

    [Fact]
    public async Task GetDropoffAsync_NoRecordYet_Returns404()
    {
        SetupPickup(BuildPickup("Scheduled"));
        SetupRecord(null);

        var (success, statusCode, message, _) = await _dropoffService.GetDropoffAsync(_pickupId, _userId);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Equal("No drop-off has been recorded for this pickup.", message);
    }
}
