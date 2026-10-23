namespace EduLoan.Domain.Entities;

/// <summary>A reusable file owned by one employee, independent of any loan application.</summary>
public class EmployeeDocument
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
}
