using EduLoan.Application.Features.AdminApplications;
using EduLoan.Domain.Entities;
using EduLoan.Application.Tests.Features.MasterData;
using Xunit;

namespace EduLoan.Application.Tests.Features.Admin;

public class GetSimilarApplicationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsHistoricalNeighborsAndSavedEligibilityResults()
    {
        using var db = TestDbContextFactory.Create();
        var college = new College { Name = "Example College", City = "Pune" };
        var course = new Course { Name = "M.Tech", Level = "PG", StandardDurationMonths = 24 };
        db.Colleges.Add(college);
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var employee = new User
        {
            Id = Guid.NewGuid(), EmployeeCode = "EMP-KNN", FullName = "KNN Test Employee",
            Email = "knn-test@example.com", PasswordHash = "not-used", Role = UserRole.Employee,
            DateOfJoining = new DateOnly(2020, 1, 1), MonthlySalary = 50000
        };
        db.Users.Add(employee);
        await db.SaveChangesAsync();

        var target = NewApplication("CURRENT", ApplicationStatus.Submitted, college, course, employee.Id);
        var approved1 = NewApplication("APPROVED-1", ApplicationStatus.Approved, college, course, employee.Id);
        var approved2 = NewApplication("APPROVED-2", ApplicationStatus.Approved, college, course, employee.Id, fees: 420000);
        var rejected = NewApplication("REJECTED-1", ApplicationStatus.Rejected, college, course, employee.Id, fees: 700000, amount: 600000);
        db.LoanApplications.AddRange(target, approved1, approved2, rejected);
        db.RuleEvaluationResults.Add(new RuleEvaluationResult
        {
            Id = Guid.NewGuid(),
            ApplicationId = target.Id,
            RuleId = 1,
            RuleName = "Minimum Tenure",
            RuleCategory = "Tenure",
            Severity = RuleSeverity.Blocking,
            Passed = true,
            EvaluatedValue = "24",
            EvaluatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new GetSimilarApplicationsQueryHandler(db, new SimilarApplicationKnn());
        var result = await handler.Handle(new GetSimilarApplicationsQuery(target.Id), CancellationToken.None);

        Assert.True(result.HasSufficientHistory);
        Assert.Equal(3, result.HistoricalCaseCount);
        Assert.Equal(3, result.Neighbors.Count);
        var rule = Assert.Single(result.EligibilityEvaluations);
        Assert.Equal("Minimum Tenure", rule.RuleName);
        Assert.True(rule.Passed);
    }

    [Fact]
    public async Task Handle_UnknownApplicationThrowsNotFound()
    {
        using var db = TestDbContextFactory.Create();
        var handler = new GetSimilarApplicationsQueryHandler(db, new SimilarApplicationKnn());

        await Assert.ThrowsAsync<EduLoan.Application.Common.NotFoundException>(
            () => handler.Handle(new GetSimilarApplicationsQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private static LoanApplication NewApplication(
        string number, ApplicationStatus status, College college, Course course, Guid employeeId,
        decimal fees = 400000, decimal amount = 300000) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationNumber = number,
        Status = status,
        CollegeId = college.Id,
        College = college,
        CourseId = course.Id,
        Course = course,
        Specialization = "Software Engineering",
        CourseDurationMonths = 24,
        TotalEducationFees = fees,
        RequestedAmount = amount,
        RequestedTenureMonths = 36,
        EducationPurpose = "Higher education",
        EmployeeId = employeeId,
    };
}
