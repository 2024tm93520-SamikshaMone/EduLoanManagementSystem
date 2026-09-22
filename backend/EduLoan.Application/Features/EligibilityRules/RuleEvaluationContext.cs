namespace EduLoan.Application.Features.EligibilityRules;

// The set of fields a rule's ConditionField can reference. Deliberately a plain
// dictionary-like object rather than reflecting over the User/LoanApplication entities
// directly — keeps the engine decoupled from domain entity shape and makes it obvious
// exactly which fields are rule-evaluable (and easy to extend later).
public class RuleEvaluationContext
{
    public required decimal TenureMonths { get; init; }
    public required decimal MonthlySalary { get; init; }
    public required decimal RequestedAmount { get; init; }
    public required decimal RequestedTenureMonths { get; init; }
    public required decimal TotalEducationFees { get; init; }

    // Computed convenience field: how many multiples of monthly salary the requested
    // loan represents — a common real-world "deduction capacity" style check.
    public decimal LoanToMonthlySalaryRatio => MonthlySalary == 0 ? decimal.MaxValue : RequestedAmount / MonthlySalary;

    public decimal? GetField(string fieldName) => fieldName switch
    {
        nameof(TenureMonths) => TenureMonths,
        nameof(MonthlySalary) => MonthlySalary,
        nameof(RequestedAmount) => RequestedAmount,
        nameof(RequestedTenureMonths) => RequestedTenureMonths,
        nameof(TotalEducationFees) => TotalEducationFees,
        nameof(LoanToMonthlySalaryRatio) => LoanToMonthlySalaryRatio,
        _ => null,
    };

    public static readonly string[] KnownFields =
    {
        nameof(TenureMonths), nameof(MonthlySalary), nameof(RequestedAmount),
        nameof(RequestedTenureMonths), nameof(TotalEducationFees), nameof(LoanToMonthlySalaryRatio),
    };
}

public record RuleEvaluationOutcome(
    int RuleId, string RuleName, string RuleCategory, string Severity,
    bool Passed, decimal EvaluatedValue, string? FailureMessage
);
