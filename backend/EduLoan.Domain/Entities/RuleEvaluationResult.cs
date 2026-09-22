namespace EduLoan.Domain.Entities;

// Snapshots the rule's name/category/message at evaluation time (rather than joining
// live to EligibilityRule) so the historical record stays accurate even if a rule is
// later edited or deleted — this table IS the "transparent decision" evidence the
// dissertation objective calls for (showing which rules passed/failed and why).
public class RuleEvaluationResult
{
    public Guid Id { get; set; }
    public Guid ApplicationId { get; set; }
    public LoanApplication? Application { get; set; }

    public int RuleId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string RuleCategory { get; set; } = string.Empty;
    public RuleSeverity Severity { get; set; }
    public bool Passed { get; set; }
    public string EvaluatedValue { get; set; } = string.Empty; // the actual value compared, as text
    public string? FailureMessage { get; set; } // populated only when Passed == false
    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;
}
