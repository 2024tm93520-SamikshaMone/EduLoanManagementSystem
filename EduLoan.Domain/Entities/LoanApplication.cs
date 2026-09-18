namespace EduLoan.Domain.Entities;

public enum ApplicationStatus
{
    Draft,
    Submitted,
    Cancelled,
    // UnderReview / Approved / Rejected are reserved for the Approval Workflow module —
    // not used yet since that logic doesn't exist in the system at this point.
}

public class LoanApplication
{
    public Guid Id { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }
    public User? Employee { get; set; }

    public int CollegeId { get; set; }
    public College? College { get; set; }

    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public string Specialization { get; set; } = string.Empty;
    public int CourseDurationMonths { get; set; }
    public decimal TotalEducationFees { get; set; }
    public decimal RequestedAmount { get; set; }
    public int RequestedTenureMonths { get; set; }
    public string EducationPurpose { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<ApplicationDocument> Documents { get; set; } = new();
}
