using System.ComponentModel.DataAnnotations;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Models;
using EcoTrack.LogisticsService.Repositories;
using EcoTrack.LogisticsService.Services;
using Moq;
using Xunit;

namespace EcoTrack.LogisticsService.Tests;

public class DisposalCertificateServiceTests
{
    private readonly Mock<IDisposalCertificateRepository> _certRepoMock;
    private readonly Mock<IPickupRepository> _pickupRepoMock;
    private readonly DisposalCertificateService _service;

    public DisposalCertificateServiceTests()
    {
        _certRepoMock = new Mock<IDisposalCertificateRepository>();
        _pickupRepoMock = new Mock<IPickupRepository>();
        _service = new DisposalCertificateService(_certRepoMock.Object, _pickupRepoMock.Object);
    }

    private static PickupRequest CreatePickupWithItem(Guid pickupId, Guid userId, Guid recyclerId, Guid itemId, string itemName = "Test Item")
    {
        return new PickupRequest
        {
            Id = pickupId,
            UserId = userId,
            RecyclerId = recyclerId,
            Status = "Scheduled",
            Items = new List<PickupItem>
            {
                new PickupItem
                {
                    Id = itemId,
                    ItemName = itemName,
                    Quantity = 1,
                    ItemCondition = "Used",
                    PickupRequestId = pickupId
                }
            }
        };
    }

    // ── CreateCertificateAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateCertificate_ValidRequest_Returns201AndCertificate()
    {
        var recyclerId = Guid.NewGuid();
        var recyclerName = "John Recycler";
        var pickupItemId = Guid.NewGuid();
        var disposalMethod = "Recycling";
        var pickupId = Guid.NewGuid();

        var pickup = CreatePickupWithItem(pickupId, Guid.NewGuid(), recyclerId, pickupItemId);
        var expectedCert = new DisposalCertificateDto
        {
            Id = Guid.NewGuid(),
            PickupItemId = pickupItemId,
            RecyclerId = recyclerId,
            RecyclerName = recyclerName,
            DisposalMethod = disposalMethod,
            DisposedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ItemName = "Test Item",
            Quantity = 1,
            ItemCondition = "Used"
        };

        _pickupRepoMock.Setup(r => r.GetByRecyclerIdAsync(recyclerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest> { pickup });

        _certRepoMock.Setup(r => r.CreateCertificateAsync(
            pickupItemId, recyclerId, recyclerName, disposalMethod, It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedCert));

        var (success, statusCode, message, cert) = await _service.CreateCertificateAsync(
            recyclerId, recyclerName, pickupItemId, disposalMethod);

        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Disposal certificate created.", message);
        Assert.NotNull(cert);
        Assert.Equal(pickupItemId, cert.PickupItemId);
        Assert.Equal(disposalMethod, cert.DisposalMethod);
        Assert.Equal(recyclerName, cert.RecyclerName);
    }

    [Fact]
    public async Task CreateCertificate_EmptyDisposalMethod_Returns400AndNoCertificate()
    {
        var recyclerId = Guid.NewGuid();

        var (success, statusCode, message, cert) = await _service.CreateCertificateAsync(
            recyclerId, "Recycler", Guid.NewGuid(), "   ", CancellationToken.None);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Disposal method is required.", message);
        Assert.Null(cert);

        // Repo should never be called
        _certRepoMock.Verify(r => r.CreateCertificateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCertificate_WhitespaceOnlyDisposalMethod_Returns400()
    {
        var recyclerId = Guid.NewGuid();

        var (success, statusCode, message, cert) = await _service.CreateCertificateAsync(
            recyclerId, "Recycler", Guid.NewGuid(), "\t\n  ", CancellationToken.None);

        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Equal("Disposal method is required.", message);
        Assert.Null(cert);
    }

    [Fact]
    public async Task CreateCertificate_ItemNotFound_Returns404()
    {
        var recyclerId = Guid.NewGuid();
        var pickupItemId = Guid.NewGuid();

        _pickupRepoMock.Setup(r => r.GetByRecyclerIdAsync(recyclerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest>());

        var (success, statusCode, message, cert) = await _service.CreateCertificateAsync(
            recyclerId, "Recycler", pickupItemId, "Recycling", CancellationToken.None);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("not found", message);
        Assert.Null(cert);

        _certRepoMock.Verify(r => r.CreateCertificateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCertificate_ItemNotAssignedToRecycler_Returns404()
    {
        var recyclerId = Guid.NewGuid();
        var pickupItemId = Guid.NewGuid();

        // Recycler has pickups, but none contain this item
        var pickup = CreatePickupWithItem(
            Guid.NewGuid(), Guid.NewGuid(), recyclerId, Guid.NewGuid());

        _pickupRepoMock.Setup(r => r.GetByRecyclerIdAsync(recyclerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest> { pickup });

        var (success, statusCode, message, cert) = await _service.CreateCertificateAsync(
            recyclerId, "Recycler", pickupItemId, "Recycling", CancellationToken.None);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Null(cert);
        _certRepoMock.Verify(r => r.CreateCertificateAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateCertificate_EmptyRecyclerName_UsesRecycler()
    {
        var recyclerId = Guid.NewGuid();
        var pickupItemId = Guid.NewGuid();
        var pickupId = Guid.NewGuid();

        var pickup = CreatePickupWithItem(pickupId, Guid.NewGuid(), recyclerId, pickupItemId);
        var expectedCert = new DisposalCertificateDto
        {
            Id = Guid.NewGuid(), PickupItemId = pickupItemId,
            RecyclerId = recyclerId, RecyclerName = "Recycler",
            DisposalMethod = "Recycling", DisposedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, ItemName = "Test Item",
            Quantity = 1, ItemCondition = "Used"
        };

        _pickupRepoMock.Setup(r => r.GetByRecyclerIdAsync(recyclerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest> { pickup });

        _certRepoMock.Setup(r => r.CreateCertificateAsync(
            pickupItemId, recyclerId, "Recycler", "Recycling", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, expectedCert));

        var (success, statusCode, message, cert) = await _service.CreateCertificateAsync(
            recyclerId, "Recycler", pickupItemId, "Recycling", CancellationToken.None);

        Assert.True(success);
        Assert.Equal("Recycler", cert.RecyclerName);
        _certRepoMock.Verify(r => r.CreateCertificateAsync(
            pickupItemId, recyclerId, "Recycler", "Recycling", It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetItemWithCertificateAsync ─────────────────────────────────────

    [Fact]
    public async Task GetItemWithCertificate_OwnerCanView_CertificateReturned()
    {
        var pickupItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var pickupId = Guid.NewGuid();
        var certId = Guid.NewGuid();

        var pickup = CreatePickupWithItem(pickupId, userId, recyclerId, pickupItemId, "Laptop");
        var cert = new DisposalCertificateDto
        {
            Id = certId,
            PickupItemId = pickupItemId,
            RecyclerId = recyclerId,
            RecyclerName = "John Recycler",
            DisposalMethod = "Recycling",
            DisposedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            ItemName = "Laptop",
            Quantity = 1,
            ItemCondition = "Used"
        };

        _certRepoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        _pickupRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest> { pickup });

        var (success, statusCode, message, item) = await _service.GetItemWithCertificateAsync(
            pickupItemId, userId, CancellationToken.None);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Certification retrieved.", message);
        Assert.NotNull(item);
        Assert.Equal(pickupItemId, item.Id);
        Assert.Equal("Laptop", item.ItemName);
        Assert.Equal("Recycling", item.Certificate.DisposalMethod);
        Assert.Equal("Disposed", item.Status);
    }

    [Fact]
    public async Task GetItemWithCertificate_IssuingRecyclerCanView()
    {
        var pickupItemId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var pickupId = Guid.NewGuid();
        var certId = Guid.NewGuid();

        var pickup = CreatePickupWithItem(pickupId, otherUserId, recyclerId, pickupItemId, "Monitor");
        var cert = new DisposalCertificateDto
        {
            Id = certId, PickupItemId = pickupItemId,
            RecyclerId = recyclerId, RecyclerName = "John Recycler",
            DisposalMethod = "Shredding", DisposedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, ItemName = "Monitor",
            Quantity = 1, ItemCondition = "Used"
        };

        _certRepoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        _pickupRepoMock.Setup(r => r.GetByUserIdAsync(recyclerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest>());

        var (success, statusCode, message, item) = await _service.GetItemWithCertificateAsync(
            pickupItemId, recyclerId, CancellationToken.None);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.NotNull(item);
        Assert.Equal("Shredding", item.Certificate.DisposalMethod);
    }

    [Fact]
    public async Task GetItemWithCertificate_UnrelatedUser_Returns403()
    {
        var pickupItemId = Guid.NewGuid();
        var unrelatedUserId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var certId = Guid.NewGuid();

        var cert = new DisposalCertificateDto
        {
            Id = certId, PickupItemId = pickupItemId,
            RecyclerId = recyclerId, RecyclerName = "John Recycler",
            DisposalMethod = "Recycling", DisposedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow, ItemName = "Laptop",
            Quantity = 1, ItemCondition = "Used"
        };

        _certRepoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        _pickupRepoMock.Setup(r => r.GetByUserIdAsync(unrelatedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest>());

        var (success, statusCode, message, item) = await _service.GetItemWithCertificateAsync(
            pickupItemId, unrelatedUserId, CancellationToken.None);

        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Contains("not authorized", message);
        Assert.Null(item);
    }

    [Fact]
    public async Task GetItemWithCertificate_CertificateNotFound_Returns404()
    {
        var pickupItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _certRepoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DisposalCertificateDto?)null);

        var (success, statusCode, message, item) = await _service.GetItemWithCertificateAsync(
            pickupItemId, userId, CancellationToken.None);

        Assert.False(success);
        Assert.Equal(404, statusCode);
        Assert.Contains("No disposal certificate found", message);
        Assert.Null(item);
    }

    [Fact]
    public async Task GetItemWithCertificate_ReturnsCorrectItemDetails()
    {
        var pickupItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var pickupId = Guid.NewGuid();
        var certId = Guid.NewGuid();

        var pickup = CreatePickupWithItem(pickupId, userId, recyclerId,
            pickupItemId, "Dell Laptop");
        var cert = new DisposalCertificateDto
        {
            Id = certId, PickupItemId = pickupItemId,
            RecyclerId = recyclerId, RecyclerName = "John Recycler",
            DisposalMethod = "Refurbishment",
            DisposedAt = new DateTime(2026, 9, 26, 14, 30, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 9, 26, 14, 30, 0, DateTimeKind.Utc),
            ItemName = "Dell Laptop",
            Quantity = 2,
            ItemCondition = "Used"
        };

        _certRepoMock.Setup(r => r.GetByPickupItemIdAsync(pickupItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        _pickupRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PickupRequest> { pickup });

        var (success, statusCode, message, item) = await _service.GetItemWithCertificateAsync(
            pickupItemId, userId, CancellationToken.None);

        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal(pickupItemId, item.Id);
        Assert.Equal("Dell Laptop", item.ItemName);
        Assert.Equal(2, item.Quantity);
        Assert.Equal("Used", item.ItemCondition);
        Assert.Equal("Refurbishment", item.Certificate.DisposalMethod);
        Assert.Equal(2026, item.Certificate.DisposedAt.Year);
        Assert.Equal(9, item.Certificate.DisposedAt.Month);
        Assert.Equal(26, item.Certificate.DisposedAt.Day);
        Assert.Equal("Disposed", item.Status);
    }
}
