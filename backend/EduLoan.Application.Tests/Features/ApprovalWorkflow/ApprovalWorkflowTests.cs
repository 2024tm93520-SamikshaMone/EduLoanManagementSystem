using EduLoan.Application.Common;
using EduLoan.Application.Features.ApprovalWorkflow;
using EduLoan.Application.Tests.Features.MasterData;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.ApprovalWorkflow;

public class ApprovalWorkflowTests
{
    private static User BuildUser(Guid id, UserRole role) => new()
    {
        Id = id,
        EmployeeCode = $"EMP{id.ToString()[..4]}",
        FullName = "Test " + role,
        Email = $"{role}@acc.com",
        PasswordHash = "x",
        Role = role,
        DateOfJoining = new DateOnly(2020, 1, 1),
    };

    private static LoanApplication BuildApplication(Guid employeeId, ApplicationStatus status) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationNumber = $"EDL-2026-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
        EmployeeId = employeeId,
        CollegeId = 1,
        CourseId = 1,
        Specialization = "AI",
        CourseDurationMonths = 24,
        TotalEducationFees = 500000,
        RequestedAmount = 400000,
        RequestedTenureMonths = 36,
        EducationPurpose = "x",
        Status = status,
    };

    [Fact]
    public async Task StartReview_FromSubmitted_MovesToUnderReview_AndLogsStep()
    {
        using var db = TestDbContextFactory.Create();
        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        var hr = BuildUser(Guid.NewGuid(), UserRole.HR);
        var application = BuildApplication(employee.Id, ApplicationStatus.Submitted);
        db.Users.AddRange(employee, hr);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var result = await new StartReviewCommandHandler(db)
            .Handle(new StartReviewCommand(application.Id, hr.Id), default);

        Assert.Equal("UnderReview", result.Status);
        Assert.Single(db.ApprovalWorkflowSteps);
        Assert.Equal(WorkflowAction.StartedReview, db.ApprovalWorkflowSteps.First().Action);
    }

    [Fact]
    public async Task StartReview_FromDraft_ThrowsInUseException()
    {
        using var db = TestDbContextFactory.Create();
        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        var hr = BuildUser(Guid.NewGuid(), UserRole.HR);
        var application = BuildApplication(employee.Id, ApplicationStatus.Draft);
        db.Users.AddRange(employee, hr);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InUseException>(() => new StartReviewCommandHandler(db)
            .Handle(new StartReviewCommand(application.Id, hr.Id), default));
    }

    [Fact]
    public async Task Approve_ByTheApplicationsOwner_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.Create();
        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        //var application = BuildApplication(employee.Id, ApplicationStatus.UnderReview);
        var application = BuildApplication(employee.Id, ApplicationStatus.Submitted);
        db.Users.Add(employee);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        // Employee somehow also holds an Admin token and tries to approve their own application.
        await Assert.ThrowsAsync<ForbiddenException>(() => new ApproveApplicationCommandHandler(db)
            .Handle(new ApproveApplicationCommand(application.Id, employee.Id, "self-approved"), default));
    }

    [Fact]
    public async Task Approve_FromUnderReview_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        var admin = BuildUser(Guid.NewGuid(), UserRole.Admin);
        var application = BuildApplication(employee.Id, ApplicationStatus.UnderReview);
        db.Users.AddRange(employee, admin);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var result = await new ApproveApplicationCommandHandler(db)
            .Handle(new ApproveApplicationCommand(application.Id, admin.Id, "Looks good"), default);

        Assert.Equal("Approved", result.Status);
    }

    [Fact]
    public void RejectValidator_RequiresComments()
    {
        var validator = new RejectApplicationCommandValidator();
        Assert.False(validator.Validate(new RejectApplicationCommand(Guid.NewGuid(), Guid.NewGuid(), "")).IsValid);
        Assert.True(validator.Validate(new RejectApplicationCommand(Guid.NewGuid(), Guid.NewGuid(), "Insufficient tenure")).IsValid);
    }

    [Fact]
    public void RequestInfoValidator_RequiresComments()
    {
        var validator = new RequestInfoCommandValidator();
        Assert.False(validator.Validate(new RequestInfoCommand(Guid.NewGuid(), Guid.NewGuid(), "")).IsValid);
        Assert.True(validator.Validate(new RequestInfoCommand(Guid.NewGuid(), Guid.NewGuid(), "Please attach fee receipt")).IsValid);
    }

    [Fact]
    public async Task Reject_FromApproved_ThrowsInUseException()
    {
        using var db = TestDbContextFactory.Create();
        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        var admin = BuildUser(Guid.NewGuid(), UserRole.Admin);
        var application = BuildApplication(employee.Id, ApplicationStatus.Approved);
        db.Users.AddRange(employee, admin);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InUseException>(() => new RejectApplicationCommandHandler(db)
            .Handle(new RejectApplicationCommand(application.Id, admin.Id, "too late"), default));
    }

    [Fact]
    public async Task SendToFinance_FromApproved_MovesToPendingFinance()
    {
        using var db = TestDbContextFactory.Create();
        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        var admin = BuildUser(Guid.NewGuid(), UserRole.Admin);
        var application = BuildApplication(employee.Id, ApplicationStatus.Approved);
        db.Users.AddRange(employee, admin);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var result = await new SendToFinanceCommandHandler(db)
            .Handle(new SendToFinanceCommand(application.Id, admin.Id), default);

        Assert.Equal("PendingFinance", result.Status);
    }

    [Fact]
    public async Task ConfirmProcessed_FromPendingFinance_MovesToProcessed_AndRecordsReference()
    {
        using var db = TestDbContextFactory.Create();
        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        var finance = BuildUser(Guid.NewGuid(), UserRole.Finance);
        var application = BuildApplication(employee.Id, ApplicationStatus.PendingFinance);
        db.Users.AddRange(employee, finance);
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var result = await new ConfirmProcessedCommandHandler(db)
            .Handle(new ConfirmProcessedCommand(application.Id, finance.Id, "TXN-9987"), default);

        Assert.Equal("Processed", result.Status);
        Assert.Contains("TXN-9987", db.ApprovalWorkflowSteps.First().Comments);
    }

    [Fact]
    public void ConfirmProcessedValidator_RequiresReference()
    {
        var validator = new ConfirmProcessedCommandValidator();
        Assert.False(validator.Validate(new ConfirmProcessedCommand(Guid.NewGuid(), Guid.NewGuid(), "")).IsValid);
        Assert.True(validator.Validate(new ConfirmProcessedCommand(Guid.NewGuid(), Guid.NewGuid(), "TXN-1")).IsValid);
    }

    [Fact]
    public async Task GetWorkflowHistory_ReturnsStepsInChronologicalOrder()
    {
        using var db = TestDbContextFactory.Create();

        var employee = BuildUser(Guid.NewGuid(), UserRole.Employee);
        var hr = BuildUser(Guid.NewGuid(), UserRole.HR);

        // StartReview is allowed only from Submitted status.
        var application = BuildApplication(
            employee.Id,
            ApplicationStatus.Submitted);

        db.Users.AddRange(employee, hr);
        db.LoanApplications.Add(application);

        await db.SaveChangesAsync();

        // Submitted -> UnderReview
        await new StartReviewCommandHandler(db)
            .Handle(
                new StartReviewCommand(application.Id, hr.Id),
                default);

        var history = await new GetWorkflowHistoryQueryHandler(db)
            .Handle(
                new GetWorkflowHistoryQuery(application.Id),
                default);

        Assert.Single(history);

        Assert.Equal(
            "StartedReview",
            history[0].Action);
    }
}
