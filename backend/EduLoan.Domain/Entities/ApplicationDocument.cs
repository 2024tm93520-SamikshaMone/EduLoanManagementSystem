namespace EduLoan.Domain.Entities;

// Metadata-only for now — no actual file bytes stored yet. Real file storage
// (disk/blob) is a follow-up enhancement once the core workflow is proven out.
public enum DocumentVerificationStatus { Pending, Uploaded, UnderReview, Verified, Rejected, AdditionalDocumentRequested }

public class ApplicationDocument
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication? Application { get; set; }

    public string DocumentType { get; set; } = string.Empty; // e.g. "Admission Letter", "Fee Receipt", "ID Proof"
    public string FileName { get; set; } = string.Empty;      // original name shown to the user
    public string StoredFileName { get; set; } = string.Empty; // actual name/path on disk (GUID-based, collision-proof)
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Guid? UploadedByUserId { get; set; }
    public bool IsRequired { get; set; } = true;
    public DocumentVerificationStatus VerificationStatus { get; set; } = DocumentVerificationStatus.Uploaded;
    public Guid? VerifiedByUserId { get; set; }
    public string? VerificationComments { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
}
