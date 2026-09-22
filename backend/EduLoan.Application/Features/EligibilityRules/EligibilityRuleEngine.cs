using EduLoan.Domain.Entities;

namespace EduLoan.Application.Features.EligibilityRules;

public interface IEligibilityRuleEngine
{
    List<RuleEvaluationOutcome> Evaluate(RuleEvaluationContext context, IEnumerable<EligibilityRule> activeRules);
}

public class EligibilityRuleEngine : IEligibilityRuleEngine
{
    public List<RuleEvaluationOutcome> Evaluate(RuleEvaluationContext context, IEnumerable<EligibilityRule> activeRules)
    {
        var outcomes = new List<RuleEvaluationOutcome>();

        foreach (var rule in activeRules)
        {
            var fieldValue = context.GetField(rule.ConditionField);

            if (fieldValue is null)
            {
                outcomes.Add(new RuleEvaluationOutcome(
                    rule.Id, rule.RuleName, rule.RuleCategory, rule.Severity.ToString(),
                    Passed: false, EvaluatedValue: 0,
                    FailureMessage: $"Rule references unknown field '{rule.ConditionField}' and could not be evaluated."));
                continue;
            }

            var passed = Compare(fieldValue.Value, rule.Operator, rule.ConditionValue);

            outcomes.Add(new RuleEvaluationOutcome(
                rule.Id, rule.RuleName, rule.RuleCategory, rule.Severity.ToString(),
                passed, fieldValue.Value,
                FailureMessage: passed ? null : rule.ErrorMessage));
        }

        return outcomes;
    }

    internal static bool Compare(decimal actual, string op, decimal expected) => op switch
    {
        ">=" => actual >= expected,
        "<=" => actual <= expected,
        ">" => actual > expected,
        "<" => actual < expected,
        "==" => actual == expected,
        "!=" => actual != expected,
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported operator."),
    };
}
