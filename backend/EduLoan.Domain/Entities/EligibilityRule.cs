namespace EduLoan.Domain.Entities;

public enum RuleSeverity
{
    Blocking,  // fails the application, submission is prevented
    Warning,   // recorded and shown, but doesn't prevent submission
}

// Deliberately field-based (not free-form code/scripting) so rules stay data-driven
// and admin-configurable without touching code — this is the "configurable rule engine"
// referenced throughout the dissertation, as opposed to hardcoded eligibility logic.
public class EligibilityRule
{
    public int Id { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string RuleCategory { get; set; } = string.Empty; // Tenure / Salary / LoanCap
    public string ConditionField { get; set; } = string.Empty; // e.g. "TenureMonths"
    public string Operator { get; set; } = string.Empty; // >=, <=, ==, !=, >, <
    public decimal ConditionValue { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public RuleSeverity Severity { get; set; } = RuleSeverity.Blocking;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
