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
}

public class UploadDocumentResponseDto
{
    public Guid DocumentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
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
    public string Status { get; set; } = string.Empty;
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
