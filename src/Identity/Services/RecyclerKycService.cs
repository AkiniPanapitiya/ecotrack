using EcoTrack.IdentityService.DTOs;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Repositories;

namespace EcoTrack.IdentityService.Services;

public interface IRecyclerKycService
{
    // Recycler endpoints
    Task<(bool Success, int StatusCode, string Message, UploadDocumentResponseDto? Response)>
        UploadDocumentAsync(Guid userId, UploadDocumentRequestDto dto,
            string? filePath, string? backFilePath = null, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, MyVerificationStatusDto? Response)>
        GetMyVerificationStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    // Admin endpoints
    Task<(bool Success, int StatusCode, string Message, List<PendingSubmissionDto>? Submissions)>
        GetPendingSubmissionsAsync(CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, RecyclerDocument? Document)>
        ReviewDocumentAsync(Guid documentId, AdminReviewRequestDto dto,
            Guid adminId, CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, RecyclerDocument? Document, string? TargetFilePath, string? MimeType)>
        GetDocumentFileAsync(Guid documentId, Guid requesterId, string requesterRole, string side = "front",
            CancellationToken cancellationToken = default);

    Task<(bool Success, int StatusCode, string Message, RecyclerDocument? Document)>
        GetDocumentAsync(Guid documentId, Guid requesterId, string requesterRole,
            CancellationToken cancellationToken = default);
}

public class RecyclerKycService : IRecyclerKycService
{
    private readonly IUserRepository _userRepository;
    private readonly IRecyclerDocumentRepository _documentRepository;
    private readonly IAuditRepository _auditRepository;

    // Allowed file types and max size (5 MB)
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "application/pdf" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public RecyclerKycService(
        IUserRepository userRepository,
        IRecyclerDocumentRepository documentRepository,
        IAuditRepository auditRepository)
    {
        _userRepository = userRepository;
        _documentRepository = documentRepository;
        _auditRepository = auditRepository;
    }

    // ---------------------------------------------------------------
    // POST upload document — recycler only, validates file
    // ---------------------------------------------------------------
    public async Task<(bool Success, int StatusCode, string Message, UploadDocumentResponseDto? Response)>
        UploadDocumentAsync(Guid userId, UploadDocumentRequestDto dto,
            string? filePath, string? backFilePath = null, CancellationToken cancellationToken = default)
    {
        // 1. Check user exists and is a recycler
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return (false, 404, "User not found.", null);
        }

        if (!string.Equals(user.Role, "Recycler", StringComparison.OrdinalIgnoreCase))
        {
            return (false, 403, "Only recyclers can upload KYC documents.", null);
        }

        // 2. Check if already has a document under review or approved
        //    (Rejected documents allow re-upload)
        if (await _documentRepository.HasDocumentAsync(userId, cancellationToken))
        {
            return (false, 409, "You have already submitted a KYC document. Please wait for review.", null);
        }

        // 3. Validate DocumentType
        var docType = dto.DocumentType.Trim();
        if (docType != "ID" && docType != "BusinessProof")
        {
            return (false, 400, "DocumentType must be 'ID' or 'BusinessProof'.", null);
        }

        // 4. Validate main file
        if (dto.File == null || dto.File.Length == 0)
        {
            return (false, 400, "No file uploaded.", null);
        }

        var contentType = dto.File.ContentType.ToLowerInvariant();
        if (!AllowedTypes.Contains(contentType))
        {
            return (false, 400,
                "Invalid file type. Only JPEG, PNG, and PDF are allowed.", null);
        }

        if (dto.File.Length > MaxFileSizeBytes)
        {
            return (false, 400,
                $"File too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB.", null);
        }

        // Validate back file if provided
        string? backFileName = null;
        string? backFileType = null;
        long backFileSize = 0;

        if (dto.BackFile != null && dto.BackFile.Length > 0)
        {
            var backContentType = dto.BackFile.ContentType.ToLowerInvariant();
            if (!AllowedTypes.Contains(backContentType))
            {
                return (false, 400, "Invalid back file type. Only JPEG, PNG, and PDF are allowed.", null);
            }
            if (dto.BackFile.Length > MaxFileSizeBytes)
            {
                return (false, 400, $"Back file too large. Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB.", null);
            }
            backFileName = dto.BackFile.FileName;
            backFileType = backContentType;
            backFileSize = dto.BackFile.Length;
        }

        // 5. Save document record
        var document = await _documentRepository.CreateDocumentAsync(
            userId, docType,
            dto.File.FileName, filePath ?? "", contentType, dto.File.Length,
            backFileName, backFilePath, backFileType, backFileSize,
            cancellationToken);

        // 6. Audit log
        await _auditRepository.LogActivityAsync(new UserAuditLog
        {
            UserId = userId,
            UserEmail = user.Email,
            Action = "KYC_DOCUMENT_UPLOADED",
            Role = user.Role,
            Details = $"Uploaded {docType} document: {dto.File.FileName}{(backFileName != null ? $" and back: {backFileName}" : "")} ({document.Id})",
            IpAddress = null,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        return (true, 201, "Document submitted for review.", new UploadDocumentResponseDto
        {
            DocumentId = document.Id,
            DocumentType = document.DocumentType,
            FileName = document.FileName,
            BackFileName = document.BackFileName,
            Status = document.Status,
            SubmittedAt = document.SubmittedAt,
            Message = "Document submitted for review."
        });
    }

    // ---------------------------------------------------------------
    // GET my verification status — recycler
    // ---------------------------------------------------------------
    public async Task<(bool Success, int StatusCode, string Message, MyVerificationStatusDto? Response)>
        GetMyVerificationStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return (false, 404, "User not found.", null);
        }

        var doc = await _documentRepository.GetLatestByRecyclerIdAsync(userId, cancellationToken);

        if (doc == null)
        {
            return (true, 200, "No KYC document submitted yet.", new MyVerificationStatusDto
            {
                HasSubmittedDocument = false,
                Status = "Not Submitted"
            });
        }

        return (true, 200, "Your KYC document has been submitted.", new MyVerificationStatusDto
        {
            HasSubmittedDocument = true,
            DocumentId = doc.Id,
            DocumentType = doc.DocumentType,
            FileName = doc.FileName,
            BackFileName = doc.BackFileName,
            Status = doc.Status,
            SubmittedAt = doc.SubmittedAt,
            ReviewedAt = doc.ReviewedAt,
            ReviewNote = doc.ReviewNote
        });
    }

    // ---------------------------------------------------------------
    // GET pending submissions — admin only
    // ---------------------------------------------------------------
    public async Task<(bool Success, int StatusCode, string Message, List<PendingSubmissionDto>? Submissions)>
        GetPendingSubmissionsAsync(CancellationToken cancellationToken = default)
    {
        var submissions = await _documentRepository.GetPendingSubmissionsAsync(cancellationToken);

        var result = submissions.Select(s => new PendingSubmissionDto
        {
            DocumentId = s.Doc.Id,
            RecyclerId = s.Doc.RecyclerId,
            RecyclerName = s.Recycler.FullName,
            RecyclerEmail = s.Recycler.Email,
            DocumentType = s.Doc.DocumentType,
            FileName = s.Doc.FileName,
            FileType = s.Doc.FileType,
            FileSize = s.Doc.FileSize,
            BackFileName = s.Doc.BackFileName,
            BackFileType = s.Doc.BackFileType,
            BackFileSize = s.Doc.BackFileSize,
            SubmittedAt = s.Doc.SubmittedAt
        }).ToList();

        return (true, 200, "OK.", result);
    }

    // ---------------------------------------------------------------
    // PUT verify / reject — admin only, logs audit
    // ---------------------------------------------------------------
    public async Task<(bool Success, int StatusCode, string Message, RecyclerDocument? Document)>
        ReviewDocumentAsync(Guid documentId, AdminReviewRequestDto dto,
            Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Validate status
        var newStatus = dto.Status.Trim();
        if (newStatus != "Verified" && newStatus != "Rejected")
        {
            return (false, 400, "Status must be 'Verified' or 'Rejected'.", null);
        }

        // 2. Find the document in pending list
        var pendingDocs = await _documentRepository.GetPendingSubmissionsAsync(cancellationToken);
        var target = pendingDocs.FirstOrDefault(s => s.Doc.Id == documentId);

        if (target.Doc == null)
        {
            return (false, 404, "Document not found or already reviewed.", null);
        }

        // 3. If rejected, require a review note with a re-upload reason
        if (newStatus == "Rejected" && string.IsNullOrWhiteSpace(dto.ReviewNote))
        {
            return (false, 400,
                "A reason is required when rejecting. Please provide a review note explaining why the document was rejected so the recycler knows what to re-upload.",
                null);
        }

        // 4. Update status
        var updated = await _documentRepository.UpdateStatusAsync(
            documentId, newStatus, adminId, dto.ReviewNote, cancellationToken);

        if (!updated)
        {
            return (false, 500, "Failed to update document status.", null);
        }

        // 5. Fetch the updated document
        var updatedDoc = await _documentRepository.GetLatestByRecyclerIdAsync(
            target.Doc.RecyclerId, cancellationToken);

        // 6. Audit log — admin decision
        var adminUser = await _userRepository.GetByIdAsync(adminId, cancellationToken);
        var adminEmail = adminUser?.Email ?? "unknown";

        await _auditRepository.LogActivityAsync(new UserAuditLog
        {
            UserId = adminId,
            UserEmail = adminEmail,
            Action = newStatus == "Verified" ? "KYC_VERIFIED" : "KYC_REJECTED",
            Role = "Admin",
            Details = $"Document {documentId} ({target.Doc.DocumentType}, recycler: {target.Recycler.Email}) " +
                      $"marked as {newStatus}. Note: {(dto.ReviewNote ?? "none")}",
            IpAddress = null,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        // 7. Response
        var message = newStatus == "Verified"
            ? "Document verified successfully."
            : "Document rejected. Please re-upload a valid document.";

        return (true, 200, message, updatedDoc);
    }

    // ---------------------------------------------------------------
    // GET document file — admin or owner
    // ---------------------------------------------------------------
    public async Task<(bool Success, int StatusCode, string Message, RecyclerDocument? Document)>
        GetDocumentAsync(Guid documentId, Guid requesterId, string requesterRole,
            CancellationToken cancellationToken = default)
    {
        var doc = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (doc == null)
        {
            return (false, 404, "Document not found.", null);
        }

        var isAdmin = string.Equals(requesterRole, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && doc.RecyclerId != requesterId)
        {
            return (false, 403, "You are not authorized to view this document.", null);
        }

        if (string.IsNullOrWhiteSpace(doc.FilePath) || !File.Exists(doc.FilePath))
        {
            return (false, 404, "Document file not found on server.", null);
        }

        return (true, 200, "OK.", doc);
    }

    public async Task<(bool Success, int StatusCode, string Message, RecyclerDocument? Document, string? TargetFilePath, string? MimeType)>
        GetDocumentFileAsync(Guid documentId, Guid requesterId, string requesterRole, string side = "front",
            CancellationToken cancellationToken = default)
    {
        var doc = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (doc == null)
        {
            return (false, 404, "Document not found.", null, null, null);
        }

        var isAdmin = string.Equals(requesterRole, "Admin", StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && doc.RecyclerId != requesterId)
        {
            return (false, 403, "You are not authorized to view this document.", null, null, null);
        }

        if (string.Equals(side, "back", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(doc.BackFilePath) || !File.Exists(doc.BackFilePath))
            {
                return (false, 404, "Back side document not found.", null, null, null);
            }
            return (true, 200, "OK.", doc, doc.BackFilePath, doc.BackFileType);
        }

        if (string.IsNullOrWhiteSpace(doc.FilePath) || !File.Exists(doc.FilePath))
        {
            return (false, 404, "Document file not found on server.", null, null, null);
        }

        return (true, 200, "OK.", doc, doc.FilePath, doc.FileType);
    }
}
