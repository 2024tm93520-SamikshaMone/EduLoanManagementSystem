namespace EduLoan.Domain.Entities;

public enum WorkflowAction
{
    StartedReview,
    EscalatedToManualReview,
    RequestedInfo,
    Approved,
    Rejected,
    SentToFinance,
    ConfirmedProcessed,
    Resubmitted,
}

// One row per action taken on an application as it moves through review — this IS the
// timeline/history shown on the admin detail page, and the audit trail objective from
// the dissertation plan (who did what, when, and why).
public class ApprovalWorkflowStep
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication? Application { get; set; }

    public Guid ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;

    public WorkflowAction Action { get; set; }
    public string? Comments { get; set; }

    public DateTime ActionedAt { get; set; } = DateTime.UtcNow;
}
