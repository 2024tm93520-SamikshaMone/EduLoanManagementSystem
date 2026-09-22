using EduLoan.Application.Common;
using EduLoan.Application.Features.EligibilityRules;
using EduLoan.Application.Features.LoanApplications;
using EduLoan.Application.Tests.Features.MasterData;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.LoanApplications;

public class SubmitWithEligibilityRulesTests
{
    private static (College college, Course course, User employee) SeedBaseData(
        EduLoan.Infrastructure.Persistence.AppDbContext db, int tenureMonthsAgo, decimal monthlySalary)
    {
        var college = new College { Name = "BITS Pilani", City = "Pilani", IsActive = true };
        var course = new Course { Name = "M.Tech SE", Level = "PG", StandardDurationMonths = 24, IsActive = true };
        var employee = new User
        {
            Id = Guid.NewGuid(), EmployeeCode = "EMPTEST", FullName = "Test Employee", Email = "test@acc.com",
            PasswordHash = "x", Role = UserRole.Employee, MonthlySalary = monthlySalary,
            DateOfJoining = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-tenureMonthsAgo)),
        };
        db.Colleges.Add(college);
        db.Courses.Add(course);
        db.Users.Add(employee);
        db.SaveChanges();
        return (college, course, employee);
    }

    private static LoanApplication BuildDraftWithDocument(
        Guid employeeId, int collegeId, int courseId, decimal requestedAmount, decimal totalFees)
    {
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(), ApplicationNumber = $"EDL-2026-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            EmployeeId = employeeId, CollegeId = collegeId, CourseId = courseId, Specialization = "AI",
            CourseDurationMonths = 24, TotalEducationFees = totalFees, RequestedAmount = requestedAmount,
            RequestedTenureMonths = 36, EducationPurpose = "x", Status = ApplicationStatus.Draft,
        };
        application.Documents.Add(new ApplicationDocument
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, DocumentType = "Admission Letter",
            FileName = "a.pdf", StoredFileName = "stored-a.pdf",
        });
        return application;
    }

    [Fact]
    public async Task Submit_NoActiveRules_SucceedsWithNoEvaluationRows()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course, employee) = SeedBaseData(db, tenureMonthsAgo: 24, monthlySalary: 50000);
        var application = BuildDraftWithDocument(employee.Id, college.Id, course.Id, 400000, 500000);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());
        var result = await handler.Handle(new SubmitLoanApplicationCommand(application.Id, employee.Id), default);

        Assert.Equal("Submitted", result.Status);
        Assert.Empty(result.EligibilityResults);
    }

    [Fact]
    public async Task Submit_PassesAllBlockingRules_SucceedsAndRecordsPassingResults()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course, employee) = SeedBaseData(db, tenureMonthsAgo: 24, monthlySalary: 50000);
        db.EligibilityRules.Add(new EligibilityRule
        {
            RuleName = "Min Tenure", RuleCategory = "Tenure", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 12, ErrorMessage = "Too new.", Severity = RuleSeverity.Blocking, IsActive = true,
        });
        var application = BuildDraftWithDocument(employee.Id, college.Id, course.Id, 400000, 500000);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());
        var result = await handler.Handle(new SubmitLoanApplicationCommand(application.Id, employee.Id), default);

        Assert.Equal("Submitted", result.Status);
        Assert.Single(result.EligibilityResults);
        Assert.True(result.EligibilityResults[0].Passed);
    }

    [Fact]
    public async Task Submit_FailsABlockingRule_ThrowsEligibilityFailedException_AndStaysDraft()
    {
        using var db = TestDbContextFactory.Create();
        // Only 3 months tenure — fails a "must be >= 12" rule
        var (college, course, employee) = SeedBaseData(db, tenureMonthsAgo: 3, monthlySalary: 50000);
        db.EligibilityRules.Add(new EligibilityRule
        {
            RuleName = "Min Tenure", RuleCategory = "Tenure", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 12, ErrorMessage = "You need at least 12 months tenure.",
            Severity = RuleSeverity.Blocking, IsActive = true,
        });
        var application = BuildDraftWithDocument(employee.Id, college.Id, course.Id, 400000, 500000);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());

        var ex = await Assert.ThrowsAsync<EligibilityFailedException>(
            () => handler.Handle(new SubmitLoanApplicationCommand(application.Id, employee.Id), default));

        Assert.Contains("You need at least 12 months tenure.", ex.FailureMessages);

        // Application must remain a Draft — rejection should not advance its status.
        var reloaded = db.LoanApplications.First(a => a.Id == application.Id);
        Assert.Equal(ApplicationStatus.Draft, reloaded.Status);
    }

    [Fact]
    public async Task Submit_FailsABlockingRule_StillPersistsTheEvaluationRowsForAudit()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course, employee) = SeedBaseData(db, tenureMonthsAgo: 3, monthlySalary: 50000);
        db.EligibilityRules.Add(new EligibilityRule
        {
            RuleName = "Min Tenure", RuleCategory = "Tenure", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 12, ErrorMessage = "Too new.", Severity = RuleSeverity.Blocking, IsActive = true,
        });
        var application = BuildDraftWithDocument(employee.Id, college.Id, course.Id, 400000, 500000);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());
        await Assert.ThrowsAsync<EligibilityFailedException>(
            () => handler.Handle(new SubmitLoanApplicationCommand(application.Id, employee.Id), default));

        // The audit trail (what was checked, and why it failed) must survive the rejection —
        // this is the "transparent decision" evidence, not just a pass/fail gate.
        Assert.Single(db.RuleEvaluationResults);
        Assert.False(db.RuleEvaluationResults.First().Passed);
    }

    [Fact]
    public async Task Submit_FailsOnlyAWarningRule_StillSucceeds()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course, employee) = SeedBaseData(db, tenureMonthsAgo: 3, monthlySalary: 50000);
        db.EligibilityRules.Add(new EligibilityRule
        {
            RuleName = "Preferred Tenure", RuleCategory = "Tenure", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 12, ErrorMessage = "Below preferred tenure.",
            Severity = RuleSeverity.Warning, IsActive = true, // Warning, not Blocking
        });
        var application = BuildDraftWithDocument(employee.Id, college.Id, course.Id, 400000, 500000);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());
        var result = await handler.Handle(new SubmitLoanApplicationCommand(application.Id, employee.Id), default);

        Assert.Equal("Submitted", result.Status);
        Assert.Single(result.EligibilityResults);
        Assert.False(result.EligibilityResults[0].Passed); // recorded as failed...
        Assert.Equal("Warning", result.EligibilityResults[0].Severity); // ...but only a Warning, so it didn't block
    }

    [Fact]
    public async Task Submit_InactiveRule_IsIgnoredEntirely()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course, employee) = SeedBaseData(db, tenureMonthsAgo: 3, monthlySalary: 50000);
        db.EligibilityRules.Add(new EligibilityRule
        {
            RuleName = "Retired Rule", RuleCategory = "Tenure", ConditionField = "TenureMonths",
            Operator = ">=", ConditionValue = 12, ErrorMessage = "Should never fire.",
            Severity = RuleSeverity.Blocking, IsActive = false, // inactive
        });
        var application = BuildDraftWithDocument(employee.Id, college.Id, course.Id, 400000, 500000);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());
        var result = await handler.Handle(new SubmitLoanApplicationCommand(application.Id, employee.Id), default);

        Assert.Equal("Submitted", result.Status);
        Assert.Empty(result.EligibilityResults);
    }

    [Fact]
    public async Task Submit_MultipleBlockingFailures_ReturnsAllFailureMessagesAtOnce()
    {
        using var db = TestDbContextFactory.Create();
        // Fails both tenure AND salary rules simultaneously
        var (college, course, employee) = SeedBaseData(db, tenureMonthsAgo: 3, monthlySalary: 5000);
        db.EligibilityRules.AddRange(
            new EligibilityRule
            {
                RuleName = "Min Tenure", RuleCategory = "Tenure", ConditionField = "TenureMonths",
                Operator = ">=", ConditionValue = 12, ErrorMessage = "Tenure too short.",
                Severity = RuleSeverity.Blocking, IsActive = true,
            },
            new EligibilityRule
            {
                RuleName = "Min Salary", RuleCategory = "Salary", ConditionField = "MonthlySalary",
                Operator = ">=", ConditionValue = 20000, ErrorMessage = "Salary too low.",
                Severity = RuleSeverity.Blocking, IsActive = true,
            });
        var application = BuildDraftWithDocument(employee.Id, college.Id, course.Id, 400000, 500000);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());
        var ex = await Assert.ThrowsAsync<EligibilityFailedException>(
            () => handler.Handle(new SubmitLoanApplicationCommand(application.Id, employee.Id), default));

        Assert.Equal(2, ex.FailureMessages.Count);
        Assert.Contains("Tenure too short.", ex.FailureMessages);
        Assert.Contains("Salary too low.", ex.FailureMessages);
    }
}
