namespace EcoTrack.IdentityService.Models;

// ECO-XX: Recycler KYC Document — stores uploaded ID/business proof documents
public class RecyclerDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecyclerId { get; set; }
    public string DocumentType { get; set; } = string.Empty; // "ID" or "BusinessProof"
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // mime type e.g. "image/jpeg"
    public long FileSize { get; set; } // bytes
    public string Status { get; set; } = "Pending"; // "Pending", "Verified", "Rejected"
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}
