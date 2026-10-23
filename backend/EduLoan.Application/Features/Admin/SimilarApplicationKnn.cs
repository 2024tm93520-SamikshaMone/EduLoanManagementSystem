using EduLoan.Domain.Entities;

namespace EduLoan.Application.Features.AdminApplications;

/// <summary>
/// Provides an explainable nearest-neighbour summary from historical final outcomes.
/// It never makes or applies an approval decision.
/// </summary>
public sealed class SimilarApplicationKnn
{
    public const int DefaultK = 5;
    public const int MinimumHistoricalCases = 3;

    public SimilarApplicationsSummary Evaluate(
        LoanApplication target,
        IEnumerable<LoanApplication> historicalApplications,
        int k = DefaultK)
    {
        if (k < 1) throw new ArgumentOutOfRangeException(nameof(k), "K must be at least 1.");

        var history = historicalApplications
            .Where(x => x.Id != target.Id &&
                        x.Status is ApplicationStatus.Approved or ApplicationStatus.Rejected)
            .ToList();

        var neighbors = history
            .Select(x => new
            {
                Application = x,
                Distance = Distance(target, x, history)
            })
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Application.ApplicationNumber, StringComparer.Ordinal)
            .Take(k)
            .Select(x => new SimilarApplicationCase(
                x.Application.Id,
                x.Application.ApplicationNumber,
                x.Application.Status.ToString(),
                x.Application.College?.Name ?? string.Empty,
                x.Application.Course?.Name ?? string.Empty,
                x.Application.TotalEducationFees,
                x.Application.RequestedAmount,
                x.Application.CourseDurationMonths,
                Math.Round(Math.Clamp(1m - x.Distance, 0m, 1m) * 100m, 1)))
            .ToList();

        if (history.Count < MinimumHistoricalCases)
        {
            return new SimilarApplicationsSummary(
                false,
                history.Count,
                k,
                neighbors,
                null,
                null,
                $"Only {history.Count} historical approved/rejected application(s) are available. At least {MinimumHistoricalCases} are required for a meaningful KNN summary. Review this application manually.");
        }

        var selected = neighbors;
        var totalWeight = 0m;
        var approvedWeight = 0m;
        foreach (var neighbor in selected)
        {
            // Similarity is bounded to [0,100]. The small offset keeps an exact match finite.
            var weight = 1m / (101m - neighbor.SimilarityPercent);
            totalWeight += weight;
            if (neighbor.Outcome == ApplicationStatus.Approved.ToString())
                approvedWeight += weight;
        }

        var approvedPercent = totalWeight == 0m ? 0m : approvedWeight / totalWeight * 100m;
        var rejectedPercent = 100m - approvedPercent;
        var summary = approvedPercent == rejectedPercent
            ? "Similar historical cases have a balanced mix of approved and rejected outcomes."
            : approvedPercent > rejectedPercent
                ? $"{selected.Count(x => x.Outcome == "Approved")} of {selected.Count} nearest historical cases were approved. This is context only, not an approval recommendation."
                : $"{selected.Count(x => x.Outcome == "Rejected")} of {selected.Count} nearest historical cases were rejected. This is context only, not a rejection recommendation.";

        return new SimilarApplicationsSummary(
            true,
            history.Count,
            Math.Min(k, history.Count),
            neighbors,
            Math.Round(approvedPercent, 1),
            Math.Round(rejectedPercent, 1),
            summary);
    }

    private static decimal Distance(
        LoanApplication target,
        LoanApplication candidate,
        IReadOnlyCollection<LoanApplication> history)
    {
        // Categorical and numeric features are deliberately limited to education/fee
        // characteristics. Salary, employee identity, department and other sensitive
        // employee attributes are not used by KNN.
        var collegeMismatch = target.CollegeId == candidate.CollegeId ? 0m : 1m;
        var courseMismatch = target.CourseId == candidate.CourseId ? 0m : 1m;
        var specializationMismatch = string.Equals(
            target.Specialization?.Trim(),
            candidate.Specialization?.Trim(),
            StringComparison.OrdinalIgnoreCase) ? 0m : 1m;

        var feesDistance = NormalizeDifference(target.TotalEducationFees, candidate.TotalEducationFees,
            history.Select(x => x.TotalEducationFees));
        var amountDistance = NormalizeDifference(target.RequestedAmount, candidate.RequestedAmount,
            history.Select(x => x.RequestedAmount));
        var durationDistance = NormalizeDifference(target.CourseDurationMonths, candidate.CourseDurationMonths,
            history.Select(x => (decimal)x.CourseDurationMonths));

        return collegeMismatch * 0.20m
             + courseMismatch * 0.25m
             + specializationMismatch * 0.10m
             + feesDistance * 0.20m
             + amountDistance * 0.20m
             + durationDistance * 0.05m;
    }

    private static decimal NormalizeDifference(decimal left, decimal right, IEnumerable<decimal> historicalValues)
    {
        var values = historicalValues.ToList();
        var min = values.Append(left).Append(right).Min();
        var max = values.Append(left).Append(right).Max();
        var range = max - min;
        if (range == 0m) return 0m;
        return Math.Clamp(Math.Abs(left - right) / range, 0m, 1m);
    }
}

public sealed record SimilarApplicationCase(
    Guid ApplicationId,
    string ApplicationNumber,
    string Outcome,
    string CollegeName,
    string CourseName,
    decimal TotalEducationFees,
    decimal RequestedAmount,
    int CourseDurationMonths,
    decimal SimilarityPercent);

public sealed record SimilarApplicationsSummary(
    bool HasSufficientHistory,
    int HistoricalCaseCount,
    int K,
    IReadOnlyList<SimilarApplicationCase> Neighbors,
    decimal? ApprovedWeightedPercent,
    decimal? RejectedWeightedPercent,
    string Summary)
{
    public IReadOnlyList<EligibilityEvaluationSummary> EligibilityEvaluations { get; init; }
        = Array.Empty<EligibilityEvaluationSummary>();
}

public sealed record EligibilityEvaluationSummary(
    string RuleName,
    string RuleCategory,
    string Severity,
    bool Passed,
    string EvaluatedValue,
    string? FailureMessage,
    DateTime EvaluatedAt);
