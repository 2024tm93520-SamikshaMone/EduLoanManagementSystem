using EduLoan.Application.Common;
using EduLoan.Application.Features.EligibilityRules;
using EduLoan.Application.Tests.Features.MasterData;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.EligibilityRules;

public class EligibilityRuleFeatureTests
{
    [Fact]
    public async Task GetEligibilityRules_ReturnsAllRulesOrderedByCategoryThenName()
    {
        using var db = TestDbContextFactory.Create();
        db.EligibilityRules.AddRange(
            new EligibilityRule { RuleName = "Z Rule", RuleCategory = "Salary", ConditionField = "MonthlySalary", Operator = ">=", ConditionValue = 1, ErrorMessage = "x" },
            new EligibilityRule { RuleName = "A Rule", RuleCategory = "Salary", ConditionField = "MonthlySalary", Operator = ">=", ConditionValue = 1, ErrorMessage = "x" });
        await db.SaveChangesAsync();

        var result = await new GetEligibilityRulesQueryHandler(db).Handle(new GetEligibilityRulesQuery(), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("A Rule", result[0].RuleName); // alphabetical within same category
    }

    [Fact]
    public async Task CreateEligibilityRule_Valid_PersistsAndReturnsDto()
    {
        using var db = TestDbContextFactory.Create();

        var result = await new CreateEligibilityRuleCommandHandler(db).Handle(
            new CreateEligibilityRuleCommand("Min Tenure", "Tenure", "TenureMonths", ">=", 12, "Too new.", "Blocking"),
            default);

        Assert.Equal("Min Tenure", result.RuleName);
        Assert.Equal("Blocking", result.Severity);
        Assert.True(result.IsActive);
        Assert.Single(db.EligibilityRules);
    }

    [Fact]
    public async Task UpdateEligibilityRule_UnknownId_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.Create();

        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateEligibilityRuleCommandHandler(db).Handle(
            new UpdateEligibilityRuleCommand(999, "X", "Y", "TenureMonths", ">=", 1, "x", "Blocking", true), default));
    }

    [Fact]
    public async Task UpdateEligibilityRule_CanDeactivateWithoutDeleting()
    {
        using var db = TestDbContextFactory.Create();
        var rule = new EligibilityRule
        {
            RuleName = "Min Tenure", RuleCategory = "Tenure", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 12, ErrorMessage = "Too new.", IsActive = true,
        };
        db.EligibilityRules.Add(rule);
        await db.SaveChangesAsync();

        var result = await new UpdateEligibilityRuleCommandHandler(db).Handle(
            new UpdateEligibilityRuleCommand(rule.Id, rule.RuleName, rule.RuleCategory, rule.ConditionField,
                rule.Operator, rule.ConditionValue, rule.ErrorMessage, "Blocking", false), default);

        Assert.False(result.IsActive);
        Assert.Single(db.EligibilityRules); // still exists, just inactive
    }

    [Fact]
    public async Task DeleteEligibilityRule_NeverEvaluated_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var rule = new EligibilityRule
        {
            RuleName = "Unused Rule", RuleCategory = "Test", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 1, ErrorMessage = "x",
        };
        db.EligibilityRules.Add(rule);
        await db.SaveChangesAsync();

        await new DeleteEligibilityRuleCommandHandler(db).Handle(new DeleteEligibilityRuleCommand(rule.Id), default);

        Assert.Empty(db.EligibilityRules);
    }

    [Fact]
    public async Task DeleteEligibilityRule_HasEvaluationHistory_ThrowsInUseException()
    {
        using var db = TestDbContextFactory.Create();
        var rule = new EligibilityRule
        {
            RuleName = "Used Rule", RuleCategory = "Test", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 1, ErrorMessage = "x",
        };
        db.EligibilityRules.Add(rule);
        await db.SaveChangesAsync();

        db.RuleEvaluationResults.Add(new RuleEvaluationResult
        {
            Id = Guid.NewGuid(), ApplicationId = Guid.NewGuid(), RuleId = rule.Id,
            RuleName = rule.RuleName, RuleCategory = rule.RuleCategory, Severity = RuleSeverity.Blocking,
            Passed = true, EvaluatedValue = "24",
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InUseException>(
            () => new DeleteEligibilityRuleCommandHandler(db).Handle(new DeleteEligibilityRuleCommand(rule.Id), default));

        Assert.Single(db.EligibilityRules); // not deleted
    }
}

public class EligibilityRuleValidatorTests
{
    [Theory]
    [InlineData("", "Tenure", "TenureMonths", ">=", "x", "Blocking", false)]     // name required
    [InlineData("Min Tenure", "Tenure", "InvalidField", ">=", "x", "Blocking", false)] // unknown field
    [InlineData("Min Tenure", "Tenure", "TenureMonths", "~=", "x", "Blocking", false)] // invalid operator
    [InlineData("Min Tenure", "Tenure", "TenureMonths", ">=", "", "Blocking", false)]  // message required
    [InlineData("Min Tenure", "Tenure", "TenureMonths", ">=", "x", "Severe", false)]   // invalid severity
    [InlineData("Min Tenure", "Tenure", "TenureMonths", ">=", "x", "Blocking", true)]  // all valid
    public void CreateEligibilityRuleCommandValidator_VariousInputs(
        string name, string category, string field, string op, string errorMessage, string severity, bool expectedValid)
    {
        var command = new CreateEligibilityRuleCommand(name, category, field, op, 12, errorMessage, severity);
        var result = new CreateEligibilityRuleCommandValidator().Validate(command);
        Assert.Equal(expectedValid, result.IsValid);
    }
}
