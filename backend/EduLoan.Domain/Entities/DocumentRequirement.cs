namespace EduLoan.Domain.Entities;

/// <summary>Configurable document checklist entry; rules are stored in SQL, not React.</summary>
public class DocumentRequirement
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public bool IsRequired { get; set; } = true;
    public decimal? MinimumLoanAmount { get; set; }
    public decimal? MaximumLoanAmount { get; set; }
    public string? EmploymentType { get; set; }
    public string? EducationLevel { get; set; }
    public bool RequiresCollateral { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
