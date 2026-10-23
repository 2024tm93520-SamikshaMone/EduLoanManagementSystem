namespace EduLoan.Domain.Entities;

public class FinanceHandoff
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication? Application { get; set; }
    public Guid? ReceivedByUserId { get; set; }
    public DateTime? ReceivedAtUtc { get; set; }
    public string? BankReferenceNumber { get; set; }
    public DateTime? BankSubmissionDateUtc { get; set; }
    public decimal? ProcessedAmount { get; set; }
    public string? ConfirmationReference { get; set; }
    public DateTime? ConfirmationDateUtc { get; set; }
    public string Status { get; set; } = "Pending Finance Review";
    public string? FinanceComments { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
