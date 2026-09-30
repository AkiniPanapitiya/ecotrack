using System.ComponentModel.DataAnnotations;

namespace EcoTrack.IdentityService.DTOs;

// --- Recycler: Upload document ---

public class UploadDocumentRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string DocumentType { get; set; } = string.Empty; // "ID" or "BusinessProof"

    [Required]
    public IFormFile File { get; set; } = null!;

    public IFormFile? BackFile { get; set; }
}

public class UploadDocumentResponseDto
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? BackFileName { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime SubmittedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

// --- Recycler: Get my verification status ---

public class MyVerificationStatusDto
{
    public bool HasSubmittedDocument { get; set; }
    public Guid? DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? BackFileName { get; set; }
    public bool HasBackFile => !string.IsNullOrEmpty(BackFileName);
    public string Status { get; set; } = "Not Submitted";
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

// --- Admin: Pending submissions list ---

public class PendingSubmissionDto
{
    public Guid DocumentId { get; set; }
    public Guid RecyclerId { get; set; }
    public string RecyclerName { get; set; } = string.Empty;
    public string RecyclerEmail { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? BackFileName { get; set; }
    public string? BackFileType { get; set; }
    public long BackFileSize { get; set; }
    public bool HasBackFile => !string.IsNullOrEmpty(BackFileName);
    public DateTime SubmittedAt { get; set; }
}

public class AdminReviewRequestDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Status { get; set; } = string.Empty; // "Verified" or "Rejected"

    [StringLength(500)]
    public string? ReviewNote { get; set; }
}
