using EduLoan.Application.Features.EligibilityRules;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.EligibilityRules;

public class EligibilityRuleEngineTests
{
    private static RuleEvaluationContext BuildContext(
        decimal tenureMonths = 24, decimal monthlySalary = 50000, decimal requestedAmount = 400000,
        decimal requestedTenureMonths = 36, decimal totalEducationFees = 500000) => new()
    {
        TenureMonths = tenureMonths,
        MonthlySalary = monthlySalary,
        RequestedAmount = requestedAmount,
        RequestedTenureMonths = requestedTenureMonths,
        TotalEducationFees = totalEducationFees,
    };

    private static EligibilityRule BuildRule(
        string field, string op, decimal value, RuleSeverity severity = RuleSeverity.Blocking) => new()
    {
        Id = 1, RuleName = "Test Rule", RuleCategory = "Test", ConditionField = field,
        Operator = op, ConditionValue = value, ErrorMessage = "Test failure message", Severity = severity,
    };

    [Theory]
    [InlineData(24, ">=", 12, true)]
    [InlineData(6, ">=", 12, false)]
    [InlineData(12, ">=", 12, true)]  // boundary: equal counts as satisfying >=
    [InlineData(24, "<=", 12, false)]
    [InlineData(24, ">", 12, true)]
    [InlineData(12, ">", 12, false)]  // boundary: equal does NOT satisfy strict >
    [InlineData(24, "==", 24, true)]
    [InlineData(24, "!=", 12, true)]
    public void Evaluate_TenureRule_VariousOperatorsAndBoundaries(
        decimal tenure, string op, decimal threshold, bool expectedPass)
    {
        var engine = new EligibilityRuleEngine();
        var context = BuildContext(tenureMonths: tenure);
        var rule = BuildRule("TenureMonths", op, threshold);

        var result = engine.Evaluate(context, new[] { rule }).Single();

        Assert.Equal(expectedPass, result.Passed);
    }

    [Fact]
    public void Evaluate_PassingRule_HasNullFailureMessage()
    {
        var engine = new EligibilityRuleEngine();
        var context = BuildContext(monthlySalary: 50000);
        var rule = BuildRule("MonthlySalary", ">=", 20000);

        var result = engine.Evaluate(context, new[] { rule }).Single();

        Assert.True(result.Passed);
        Assert.Null(result.FailureMessage);
    }

    [Fact]
    public void Evaluate_FailingRule_ReturnsTheRulesErrorMessage()
    {
        var engine = new EligibilityRuleEngine();
        var context = BuildContext(monthlySalary: 10000);
        var rule = BuildRule("MonthlySalary", ">=", 20000);
        rule.ErrorMessage = "Salary too low.";

        var result = engine.Evaluate(context, new[] { rule }).Single();

        Assert.False(result.Passed);
        Assert.Equal("Salary too low.", result.FailureMessage);
    }

    [Fact]
    public void Evaluate_LoanToMonthlySalaryRatio_ComputedFieldWorksCorrectly()
    {
        var engine = new EligibilityRuleEngine();
        // 400000 requested / 20000 salary = ratio of 20
        var context = BuildContext(monthlySalary: 20000, requestedAmount: 400000);
        var passingRule = BuildRule("LoanToMonthlySalaryRatio", "<=", 24);
        var failingRule = BuildRule("LoanToMonthlySalaryRatio", "<=", 10);

        var results = engine.Evaluate(context, new[] { passingRule, failingRule });

        Assert.True(results[0].Passed);
        Assert.False(results[1].Passed);
        Assert.Equal(20, results[1].EvaluatedValue);
    }

    [Fact]
    public void Evaluate_UnknownConditionField_FailsSafeWithDiagnosticMessage()
    {
        var engine = new EligibilityRuleEngine();
        var context = BuildContext();
        var rule = BuildRule("SomeFieldThatDoesNotExist", ">=", 1);

        var result = engine.Evaluate(context, new[] { rule }).Single();

        Assert.False(result.Passed);
        Assert.Contains("unknown field", result.FailureMessage);
    }

    [Fact]
    public void Evaluate_MultipleRules_ReturnsOneOutcomePerRuleInOrder()
    {
        var engine = new EligibilityRuleEngine();
        var context = BuildContext(tenureMonths: 24, monthlySalary: 50000);
        var rules = new[]
        {
            BuildRule("TenureMonths", ">=", 12),
            BuildRule("MonthlySalary", ">=", 20000),
            BuildRule("MonthlySalary", ">=", 100000), // deliberately fails
        };

        var results = engine.Evaluate(context, rules);

        Assert.Equal(3, results.Count);
        Assert.True(results[0].Passed);
        Assert.True(results[1].Passed);
        Assert.False(results[2].Passed);
    }

    [Fact]
    public void Evaluate_WarningSeverityRule_StillEvaluatesNormallyJustTaggedAsWarning()
    {
        var engine = new EligibilityRuleEngine();
        var context = BuildContext(tenureMonths: 6);
        var rule = BuildRule("TenureMonths", ">=", 12, RuleSeverity.Warning);

        var result = engine.Evaluate(context, new[] { rule }).Single();

        Assert.False(result.Passed);
        Assert.Equal("Warning", result.Severity);
    }
}
