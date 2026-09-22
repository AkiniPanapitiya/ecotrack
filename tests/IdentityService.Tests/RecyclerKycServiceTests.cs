using EcoTrack.IdentityService.Data;
using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;
using EcoTrack.IdentityService.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace EcoTrack.IdentityService.Tests;

/// <summary>
/// Service-layer unit tests for Recycler KYC Verification.
/// All repositories are mocked — no database access.
/// </summary>
public class RecyclerKycServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRecyclerDocumentRepository> _documentRepositoryMock;
    private readonly Mock<IAuditRepository> _auditRepositoryMock;
    private readonly RecyclerKycService _service;
    private readonly CancellationToken _ct = CancellationToken.None;

    public RecyclerKycServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _documentRepositoryMock = new Mock<IRecyclerDocumentRepository>();
        _auditRepositoryMock = new Mock<IAuditRepository>();
        _service = new RecyclerKycService(
            _userRepositoryMock.Object,
            _documentRepositoryMock.Object,
            _auditRepositoryMock.Object);
    }

    // Helper: create a mocked IFormFile with the given properties
    private static Mock<IFormFile> MakeFile(string fileName, string contentType, long length)
    {
        var mock = new Mock<IFormFile>();
        mock.SetupGet(f => f.FileName).Returns(fileName);
        mock.SetupGet(f => f.ContentType).Returns(contentType);
        mock.SetupGet(f => f.Length).Returns(length);
        mock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        return mock;
    }

    // ------------------------------------------------------------------
    // 1. Upload saves as Pending
    // ------------------------------------------------------------------

    [Fact]
    public async Task UploadDocumentAsync_ValidRecycler_Returns201AndStatusPending()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "recycler@test.com", Role = "Recycler" };
        var fileMock = MakeFile("id_card.pdf", "application/pdf", 1024);
        var dto = new UploadDocumentRequestDto
        {
            DocumentType = "ID",
            File = fileMock.Object
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct))
            .ReturnsAsync(user);
        _documentRepositoryMock.Setup(r => r.HasDocumentAsync(userId, _ct))
            .ReturnsAsync(false);
        _documentRepositoryMock.Setup(r => r.CreateDocumentAsync(
                userId, "ID", "id_card.pdf", "", "application/pdf", 1024, _ct))
            .ReturnsAsync(CreatePendingDocument(userId, "ID", "id_card.pdf"));

        // Act
        var (success, statusCode, message, response) = await _service.UploadDocumentAsync(
            userId, dto, null, _ct);

        // Assert
        Assert.True(success);
        Assert.Equal(201, statusCode);
        Assert.Equal("Document submitted for review.", message);
        Assert.NotNull(response);
        Assert.Equal("Pending", response.Status);
        Assert.Equal("ID", response.DocumentType);
        Assert.Equal("id_card.pdf", response.FileName);

        _documentRepositoryMock.Verify(r => r.CreateDocumentAsync(
            userId, "ID", "id_card.pdf", "", "application/pdf", 1024, _ct), Times.Once);
        _auditRepositoryMock.Verify(r => r.LogActivityAsync(It.IsAny<UserAuditLog>(), _ct), Times.Once);
    }

    // ------------------------------------------------------------------
    // 2. Invalid file type rejected
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/zip")]
    [InlineData("text/plain")]
    [InlineData("video/mp4")]
    public async Task UploadDocumentAsync_InvalidFileType_Returns400(string contentType)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "recycler@test.com", Role = "Recycler" };
        var fileMock = MakeFile("photo.gif", contentType, 500);
        var dto = new UploadDocumentRequestDto
        {
            DocumentType = "ID",
            File = fileMock.Object
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct))
            .ReturnsAsync(user);
        _documentRepositoryMock.Setup(r => r.HasDocumentAsync(userId, _ct))
            .ReturnsAsync(false);

        // Act
        var (success, statusCode, message, response) = await _service.UploadDocumentAsync(
            userId, dto, null, _ct);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Contains("Invalid file type", message);
        Assert.Null(response);
        _documentRepositoryMock.Verify(r => r.CreateDocumentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ------------------------------------------------------------------
    // 3. File size exceeded rejected
    // ------------------------------------------------------------------

    [Fact]
    public async Task UploadDocumentAsync_FileTooLarge_Returns400()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "recycler@test.com", Role = "Recycler" };
        var fileMock = MakeFile("large.pdf", "application/pdf", 6 * 1024 * 1024); // 6 MB > 5 MB limit
        var dto = new UploadDocumentRequestDto
        {
            DocumentType = "ID",
            File = fileMock.Object
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct))
            .ReturnsAsync(user);
        _documentRepositoryMock.Setup(r => r.HasDocumentAsync(userId, _ct))
            .ReturnsAsync(false);

        // Act
        var (success, statusCode, message, response) = await _service.UploadDocumentAsync(
            userId, dto, null, _ct);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Contains("File too large", message);
        Assert.Contains("5 MB", message);
        Assert.Null(response);
    }

    // ------------------------------------------------------------------
    // 4. Non-recycler blocked
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    [InlineData("Driver")]
    public async Task UploadDocumentAsync_NonRecycler_Returns403(string role)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "other@test.com", Role = role };
        var fileMock = MakeFile("id.pdf", "application/pdf", 1024);
        var dto = new UploadDocumentRequestDto
        {
            DocumentType = "ID",
            File = fileMock.Object
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct))
            .ReturnsAsync(user);

        // Act
        var (success, statusCode, message, response) = await _service.UploadDocumentAsync(
            userId, dto, null, _ct);

        // Assert
        Assert.False(success);
        Assert.Equal(403, statusCode);
        Assert.Equal("Only recyclers can upload KYC documents.", message);
        Assert.Null(response);
        _documentRepositoryMock.Verify(r => r.CreateDocumentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ------------------------------------------------------------------
    // 5. Admin verify updates status
    // ------------------------------------------------------------------

    [Fact]
    public async Task ReviewDocumentAsync_AdminVerify_Returns200AndStatusVerified()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var pendingDoc = new RecyclerDocument
        {
            Id = documentId, RecyclerId = recyclerId, DocumentType = "ID",
            FileName = "id.pdf", Status = "Pending", SubmittedAt = DateTime.UtcNow
        };
        var recycler = new User { Id = recyclerId, Email = "recycler@test.com", FullName = "Recycler User" };
        var adminUser = new User { Id = adminId, Email = "admin@test.com", Role = "Admin" };

        _documentRepositoryMock.Setup(r => r.GetPendingSubmissionsAsync(_ct))
            .ReturnsAsync(new List<(RecyclerDocument Doc, User Recycler)>
            { (pendingDoc, recycler) });
        _documentRepositoryMock.Setup(r => r.UpdateStatusAsync(
                documentId, "Verified", adminId, null, _ct))
            .ReturnsAsync(true);
        _documentRepositoryMock.Setup(r => r.GetLatestByRecyclerIdAsync(recyclerId, _ct))
            .ReturnsAsync(CreateVerifiedDocument(documentId, recyclerId, "ID", "id.pdf"));
        _userRepositoryMock.Setup(r => r.GetByIdAsync(adminId, _ct))
            .ReturnsAsync(adminUser);

        var dto = new AdminReviewRequestDto { Status = "Verified" };

        // Act
        var (success, statusCode, message, document) = await _service.ReviewDocumentAsync(
            documentId, dto, adminId, _ct);

        // Assert
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Document verified successfully.", message);
        Assert.NotNull(document);
        Assert.Equal("Verified", document.Status);

        _documentRepositoryMock.Verify(r => r.UpdateStatusAsync(
            documentId, "Verified", adminId, null, _ct), Times.Once);
        _auditRepositoryMock.Verify(r => r.LogActivityAsync(
            It.Is<UserAuditLog>(a => a.Action == "KYC_VERIFIED" &&
                                       a.UserEmail == "admin@test.com" &&
                                       a.Role == "Admin"), _ct), Times.Once);
    }

    // ------------------------------------------------------------------
    // 6. Admin reject stores review note
    // ------------------------------------------------------------------

    [Fact]
    public async Task ReviewDocumentAsync_AdminReject_StoresReviewNote()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var pendingDoc = new RecyclerDocument
        {
            Id = documentId, RecyclerId = recyclerId, DocumentType = "ID",
            FileName = "id.pdf", Status = "Pending", SubmittedAt = DateTime.UtcNow
        };
        var recycler = new User { Id = recyclerId, Email = "recycler@test.com", FullName = "Recycler User" };
        var adminUser = new User { Id = adminId, Email = "admin@test.com", Role = "Admin" };

        _documentRepositoryMock.Setup(r => r.GetPendingSubmissionsAsync(_ct))
            .ReturnsAsync(new List<(RecyclerDocument Doc, User Recycler)>
            { (pendingDoc, recycler) });
        _documentRepositoryMock.Setup(r => r.UpdateStatusAsync(
                documentId, "Rejected", adminId, "Blurry photo, please re-upload", _ct))
            .ReturnsAsync(true);
        _documentRepositoryMock.Setup(r => r.GetLatestByRecyclerIdAsync(recyclerId, _ct))
            .ReturnsAsync(CreateRejectedDocument(documentId, recyclerId, "ID", "id.pdf",
                "Blurry photo, please re-upload"));
        _userRepositoryMock.Setup(r => r.GetByIdAsync(adminId, _ct))
            .ReturnsAsync(adminUser);

        var dto = new AdminReviewRequestDto
        {
            Status = "Rejected",
            ReviewNote = "Blurry photo, please re-upload"
        };

        // Act
        var (success, statusCode, message, document) = await _service.ReviewDocumentAsync(
            documentId, dto, adminId, _ct);

        // Assert
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Document rejected. Please re-upload a valid document.", message);
        Assert.NotNull(document);
        Assert.Equal("Rejected", document.Status);
        Assert.Equal("Blurry photo, please re-upload", document.ReviewNote);

        _documentRepositoryMock.Verify(r => r.UpdateStatusAsync(
            documentId, "Rejected", adminId, "Blurry photo, please re-upload", _ct), Times.Once);
        _auditRepositoryMock.Verify(r => r.LogActivityAsync(
            It.Is<UserAuditLog>(a => a.Action == "KYC_REJECTED" &&
                                       a.Details.Contains("Blurry photo, please re-upload")), _ct), Times.Once);
    }

    // ------------------------------------------------------------------
    // 7. Reject without review note returns 400
    // ------------------------------------------------------------------

    [Fact]
    public async Task ReviewDocumentAsync_RejectWithoutNote_Returns400()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var pendingDoc = new RecyclerDocument
        {
            Id = documentId, RecyclerId = recyclerId, DocumentType = "ID",
            FileName = "id.pdf", Status = "Pending", SubmittedAt = DateTime.UtcNow
        };
        var recycler = new User { Id = recyclerId, Email = "recycler@test.com" };

        _documentRepositoryMock.Setup(r => r.GetPendingSubmissionsAsync(_ct))
            .ReturnsAsync(new List<(RecyclerDocument Doc, User Recycler)>
            { (pendingDoc, recycler) });

        var dto = new AdminReviewRequestDto { Status = "Rejected", ReviewNote = "" };

        // Act
        var (success, statusCode, message, document) = await _service.ReviewDocumentAsync(
            documentId, dto, Guid.NewGuid(), _ct);

        // Assert
        Assert.False(success);
        Assert.Equal(400, statusCode);
        Assert.Contains("reason is required", message);
        Assert.Null(document);
        _documentRepositoryMock.Verify(r => r.UpdateStatusAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ------------------------------------------------------------------
    // 8. Non-admin cannot verify/reject — service does not own this check
    //     (enforced at controller level). This test documents that the
    //     service accepts any caller ID and relies on the controller to
    //     verify the caller is an Admin before calling ReviewDocumentAsync.
    // ------------------------------------------------------------------

    [Fact]
    public async Task ReviewDocumentAsync_NonAdminCaller_StillProcesses()
    {
        // This documents the boundary: the service does NOT check caller role.
        // The controller (KycController) is responsible for rejecting non-admins
        // before reaching the service. If the lecturer adds an admin-role check
        // in the service later, this test will need to change.

        var documentId = Guid.NewGuid();
        var recyclerId = Guid.NewGuid();
        var callerId = Guid.NewGuid(); // non-admin user
        var pendingDoc = new RecyclerDocument
        {
            Id = documentId, RecyclerId = recyclerId, DocumentType = "ID",
            FileName = "id.pdf", Status = "Pending", SubmittedAt = DateTime.UtcNow
        };
        var recycler = new User { Id = recyclerId, Email = "recycler@test.com" };

        _documentRepositoryMock.Setup(r => r.GetPendingSubmissionsAsync(_ct))
            .ReturnsAsync(new List<(RecyclerDocument Doc, User Recycler)>
            { (pendingDoc, recycler) });
        _documentRepositoryMock.Setup(r => r.UpdateStatusAsync(
                documentId, "Verified", callerId, null, _ct))
            .ReturnsAsync(true);
        _documentRepositoryMock.Setup(r => r.GetLatestByRecyclerIdAsync(recyclerId, _ct))
            .ReturnsAsync(CreateVerifiedDocument(documentId, recyclerId, "ID", "id.pdf"));
        _userRepositoryMock.Setup(r => r.GetByIdAsync(callerId, _ct))
            .ReturnsAsync(new User { Id = callerId, Email = "user@test.com", Role = "User" });

        var dto = new AdminReviewRequestDto { Status = "Verified" };

        // Act — service processes even though caller is not admin
        var (success, statusCode, message, document) = await _service.ReviewDocumentAsync(
            documentId, dto, callerId, _ct);

        // Assert — service succeeds; admin gating belongs in controller
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Document verified successfully.", message);
        Assert.NotNull(document);
        Assert.Equal("Verified", document.Status);
    }

    // ------------------------------------------------------------------
    // 9. Get my status — no document
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetMyVerificationStatusAsync_NoDocument_Returns200WithEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "recycler@test.com", Role = "Recycler" };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct))
            .ReturnsAsync(user);
        _documentRepositoryMock.Setup(r => r.GetLatestByRecyclerIdAsync(userId, _ct))
            .ReturnsAsync((RecyclerDocument?)null);

        // Act
        var (success, statusCode, message, response) = await _service.GetMyVerificationStatusAsync(
            userId, _ct);

        // Assert
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("No KYC document submitted yet.", message);
        Assert.NotNull(response);
        Assert.False(response.HasSubmittedDocument);
        Assert.Null(response.DocumentId);
    }

    // ------------------------------------------------------------------
    // 10. Get my status — has document
    // ------------------------------------------------------------------

    [Fact]
    public async Task GetMyVerificationStatusAsync_HasDocument_Returns200WithStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "recycler@test.com", Role = "Recycler" };
        var doc = new RecyclerDocument
        {
            Id = Guid.NewGuid(), RecyclerId = userId, DocumentType = "ID",
            FileName = "id.pdf", Status = "Pending", SubmittedAt = DateTime.UtcNow
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, _ct))
            .ReturnsAsync(user);
        _documentRepositoryMock.Setup(r => r.GetLatestByRecyclerIdAsync(userId, _ct))
            .ReturnsAsync(doc);

        // Act
        var (success, statusCode, message, response) = await _service.GetMyVerificationStatusAsync(
            userId, _ct);

        // Assert
        Assert.True(success);
        Assert.Equal(200, statusCode);
        Assert.Equal("Your KYC document has been submitted.", message);
        Assert.NotNull(response);
        Assert.True(response.HasSubmittedDocument);
        Assert.Equal("Pending", response.Status);
        Assert.Equal("ID", response.DocumentType);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static RecyclerDocument CreatePendingDocument(Guid recyclerId, string docType, string fileName)
    {
        return new RecyclerDocument
        {
            Id = Guid.NewGuid(),
            RecyclerId = recyclerId,
            DocumentType = docType,
            FileName = fileName,
            FilePath = "",
            FileType = "application/pdf",
            FileSize = 1024,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow
        };
    }

    private static RecyclerDocument CreateVerifiedDocument(Guid docId, Guid recyclerId,
        string docType, string fileName)
    {
        return new RecyclerDocument
        {
            Id = docId,
            RecyclerId = recyclerId,
            DocumentType = docType,
            FileName = fileName,
            FilePath = "",
            FileType = "application/pdf",
            FileSize = 1024,
            Status = "Verified",
            SubmittedAt = DateTime.UtcNow,
            ReviewedAt = DateTime.UtcNow
        };
    }

    private static RecyclerDocument CreateRejectedDocument(Guid docId, Guid recyclerId,
        string docType, string fileName, string reviewNote)
    {
        return new RecyclerDocument
        {
            Id = docId,
            RecyclerId = recyclerId,
            DocumentType = docType,
            FileName = fileName,
            FilePath = "",
            FileType = "application/pdf",
            FileSize = 1024,
            Status = "Rejected",
            SubmittedAt = DateTime.UtcNow,
            ReviewedAt = DateTime.UtcNow,
            ReviewNote = reviewNote
        };
    }
}
