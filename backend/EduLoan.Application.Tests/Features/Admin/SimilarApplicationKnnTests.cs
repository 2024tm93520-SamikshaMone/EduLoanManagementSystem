using EduLoan.Application.Features.AdminApplications;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.Admin;

public class SimilarApplicationKnnTests
{
    private static LoanApplication Application(
        string number,
        ApplicationStatus status,
        int collegeId = 1,
        int courseId = 1,
        decimal fees = 400000,
        decimal amount = 300000,
        int duration = 24,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        ApplicationNumber = number,
        Status = status,
        CollegeId = collegeId,
        CourseId = courseId,
        College = new College { Id = collegeId, Name = $"College {collegeId}", City = "Pune" },
        Course = new Course { Id = courseId, Name = $"Course {courseId}", Level = "Postgraduate" },
        Specialization = "Software Engineering",
        TotalEducationFees = fees,
        RequestedAmount = amount,
        CourseDurationMonths = duration,
    };

    [Fact]
    public void Evaluate_WithFewerThanThreeFinalOutcomes_RequiresManualReview()
    {
        var target = Application("CURRENT", ApplicationStatus.Submitted);
        var history = new[]
        {
            Application("A1", ApplicationStatus.Approved),
            Application("R1", ApplicationStatus.Rejected)
        };

        var result = new SimilarApplicationKnn().Evaluate(target, history);

        Assert.False(result.HasSufficientHistory);
        Assert.Equal(2, result.HistoricalCaseCount);
        Assert.Null(result.ApprovedWeightedPercent);
        Assert.Contains("manually", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_UsesOnlyApprovedRejectedAndExcludesCurrentApplication()
    {
        var currentId = Guid.NewGuid();
        var target = Application("CURRENT", ApplicationStatus.Submitted, id: currentId);
        var history = new[]
        {
            Application("A1", ApplicationStatus.Approved),
            Application("R1", ApplicationStatus.Rejected),
            Application("A2", ApplicationStatus.Approved),
            Application("DRAFT", ApplicationStatus.Draft),
            Application("SUBMITTED", ApplicationStatus.Submitted),
            Application("SELF", ApplicationStatus.Approved, id: currentId)
        };

        var result = new SimilarApplicationKnn().Evaluate(target, history);

        Assert.Equal(3, result.HistoricalCaseCount);
        Assert.DoesNotContain(result.Neighbors, x => x.ApplicationNumber is "DRAFT" or "SUBMITTED" or "SELF");
        Assert.True(result.HasSufficientHistory);
    }

    [Fact]
    public void Evaluate_ReturnsNearestSimilarCasesFirst()
    {
        var target = Application("CURRENT", ApplicationStatus.Submitted, fees: 400000, amount: 300000, duration: 24);
        var history = new[]
        {
            Application("FAR", ApplicationStatus.Rejected, collegeId: 9, courseId: 9, fees: 900000, amount: 800000, duration: 60),
            Application("NEAR-APPROVED", ApplicationStatus.Approved, fees: 400000, amount: 300000, duration: 24),
            Application("NEAR-REJECTED", ApplicationStatus.Rejected, fees: 410000, amount: 305000, duration: 24),
            Application("MID", ApplicationStatus.Approved, collegeId: 2, fees: 550000, amount: 400000, duration: 36)
        };

        var result = new SimilarApplicationKnn().Evaluate(target, history, k: 3);

        Assert.Equal(3, result.Neighbors.Count);
        Assert.Equal("NEAR-APPROVED", result.Neighbors[0].ApplicationNumber);
        Assert.Equal(100m, result.Neighbors[0].SimilarityPercent);
    }

    [Fact]
    public void Evaluate_ReturnsBoundedOutcomePercentagesAndDecisionSupportDisclaimer()
    {
        var target = Application("CURRENT", ApplicationStatus.Submitted);
        var history = new[]
        {
            Application("A1", ApplicationStatus.Approved),
            Application("A2", ApplicationStatus.Approved, fees: 420000),
            Application("R1", ApplicationStatus.Rejected, fees: 600000, amount: 500000, duration: 36)
        };

        var result = new SimilarApplicationKnn().Evaluate(target, history);

        Assert.InRange(result.ApprovedWeightedPercent!.Value, 0m, 100m);
        Assert.InRange(result.RejectedWeightedPercent!.Value, 0m, 100m);
        Assert.Equal(100m, result.ApprovedWeightedPercent.Value + result.RejectedWeightedPercent.Value);
        Assert.Contains("context only", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_RejectsInvalidK()
    {
        var target = Application("CURRENT", ApplicationStatus.Submitted);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimilarApplicationKnn().Evaluate(target, Array.Empty<LoanApplication>(), 0));
    }
}
