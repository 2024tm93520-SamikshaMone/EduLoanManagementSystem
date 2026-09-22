using EduLoan.Application.Common;
using EduLoan.Application.Features.EligibilityRules;
using EduLoan.Application.Features.LoanApplications;
using EduLoan.Application.Interfaces;
using EduLoan.Application.Tests.Features.MasterData;
using EduLoan.Domain.Entities;
using Moq;
using Xunit;

namespace EduLoan.Application.Tests.Features.LoanApplications;

public class LoanApplicationFeatureTests
{
    private static (College college, Course course) SeedCollegeAndCourse(
        EduLoan.Infrastructure.Persistence.AppDbContext db)
    {
        var college = new College { Name = "BITS Pilani", City = "Pilani", IsActive = true };
        var course = new Course { Name = "M.Tech SE", Level = "PG", StandardDurationMonths = 24, IsActive = true };
        db.Colleges.Add(college);
        db.Courses.Add(course);
        db.SaveChanges();
        return (college, course);
    }

    private static Mock<IDateTimeProvider> FixedDate() =>
        new Mock<IDateTimeProvider>().Also(m => m.Setup(d => d.Today).Returns(new DateOnly(2026, 9, 5)));

    [Fact]
    public async Task Create_ValidRequest_CreatesDraftApplication()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var employeeId = Guid.NewGuid();
        var dateTimeProvider = FixedDate();

        var handler = new CreateLoanApplicationCommandHandler(db, dateTimeProvider.Object);
        var result = await handler.Handle(new CreateLoanApplicationCommand(
            employeeId, college.Id, course.Id, "AI/ML", 24, 500000, 400000, 36, "MTech at BITS"), default);

        Assert.Equal("Draft", result.Status);
        Assert.StartsWith("EDL-2026-", result.ApplicationNumber);
        Assert.Equal(college.Name, result.CollegeName);
    }

    [Fact]
    public async Task Create_InactiveCollege_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        college.IsActive = false;
        await db.SaveChangesAsync();

        var handler = new CreateLoanApplicationCommandHandler(db, FixedDate().Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateLoanApplicationCommand(
            Guid.NewGuid(), college.Id, course.Id, "AI/ML", 24, 500000, 400000, 36, "purpose"), default));
    }

    [Theory]
    [InlineData(500000, 600000, false)] // requested > fees -> invalid
    [InlineData(500000, 400000, true)]
    [InlineData(500000, 500000, true)]  // exactly equal to fees is allowed
    public void CreateValidator_RequestedAmountVsFees(decimal fees, decimal requested, bool expectedValid)
    {
        var command = new CreateLoanApplicationCommand(
            Guid.NewGuid(), 1, 1, "AI/ML", 24, fees, requested, 36, "purpose");
        var result = new CreateLoanApplicationCommandValidator().Validate(command);
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public async Task Update_ByNonOwner_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var owner = Guid.NewGuid();
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-AAAAAA", EmployeeId = owner,
            CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
            TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36,
            EducationPurpose = "x", Status = ApplicationStatus.Draft,
        };
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new UpdateLoanApplicationCommandHandler(db);
        var someoneElse = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UpdateLoanApplicationCommand(
            application.Id, someoneElse, college.Id, course.Id, "AI", 24, 500000, 400000, 36, "x"), default));
    }

    [Fact]
    public async Task Update_NonDraftApplication_ThrowsInUseException()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var owner = Guid.NewGuid();
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-BBBBBB", EmployeeId = owner,
            CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
            TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36,
            EducationPurpose = "x", Status = ApplicationStatus.Submitted,
        };
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new UpdateLoanApplicationCommandHandler(db);

        await Assert.ThrowsAsync<InUseException>(() => handler.Handle(new UpdateLoanApplicationCommand(
            application.Id, owner, college.Id, course.Id, "AI", 24, 500000, 400000, 36, "x"), default));
    }

    [Fact]
    public async Task Submit_WithNoDocuments_ThrowsInUseException()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var owner = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = owner, EmployeeCode = "EMPX01", FullName = "Test Owner", Email = "owner1@acc.com",
            PasswordHash = "x", Role = UserRole.Employee, DateOfJoining = new DateOnly(2020, 1, 1), MonthlySalary = 50000,
        });
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-CCCCCC", EmployeeId = owner,
            CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
            TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36,
            EducationPurpose = "x", Status = ApplicationStatus.Draft,
        };
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());

        await Assert.ThrowsAsync<InUseException>(
            () => handler.Handle(new SubmitLoanApplicationCommand(application.Id, owner), default));
    }

    [Fact]
    public async Task Submit_WithAtLeastOneDocument_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var owner = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = owner, EmployeeCode = "EMPX02", FullName = "Test Owner 2", Email = "owner2@acc.com",
            PasswordHash = "x", Role = UserRole.Employee, DateOfJoining = new DateOnly(2020, 1, 1), MonthlySalary = 50000,
        });
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-DDDDDD", EmployeeId = owner,
            CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
            TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36,
            EducationPurpose = "x", Status = ApplicationStatus.Draft,
        };
        db.LoanApplications.Add(application);
        db.ApplicationDocuments.Add(new ApplicationDocument
        {
            Id = Guid.NewGuid(), ApplicationId = application.Id, DocumentType = "Admission Letter", FileName = "a.pdf",
        });
        await db.SaveChangesAsync();

        var handler = new SubmitLoanApplicationCommandHandler(db, new EligibilityRuleEngine());
        var result = await handler.Handle(new SubmitLoanApplicationCommand(application.Id, owner), default);

        Assert.Equal("Submitted", result.Status);
        Assert.NotNull(result.Documents);
    }

    [Fact]
    public async Task Cancel_DraftApplication_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var owner = Guid.NewGuid();
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-EEEEEE", EmployeeId = owner,
            CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
            TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36,
            EducationPurpose = "x", Status = ApplicationStatus.Draft,
        };
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var result = await new CancelLoanApplicationCommandHandler(db)
            .Handle(new CancelLoanApplicationCommand(application.Id, owner), default);

        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public async Task GetById_ByNonOwner_ThrowsForbiddenException()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var owner = Guid.NewGuid();
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-FFFFFF", EmployeeId = owner,
            CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
            TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36,
            EducationPurpose = "x", Status = ApplicationStatus.Draft,
        };
        db.LoanApplications.Add(application);
        await db.SaveChangesAsync();

        var handler = new GetApplicationByIdQueryHandler(db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetApplicationByIdQuery(application.Id, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task GetMyApplications_ReturnsOnlyRequestingEmployeesApplications()
    {
        using var db = TestDbContextFactory.Create();
        var (college, course) = SeedCollegeAndCourse(db);
        var employeeA = Guid.NewGuid();
        var employeeB = Guid.NewGuid();

        db.LoanApplications.AddRange(
            new LoanApplication
            {
                Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-A00001", EmployeeId = employeeA,
                CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
                TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36, EducationPurpose = "x",
            },
            new LoanApplication
            {
                Id = Guid.NewGuid(), ApplicationNumber = "EDL-2026-B00001", EmployeeId = employeeB,
                CollegeId = college.Id, CourseId = course.Id, Specialization = "AI", CourseDurationMonths = 24,
                TotalEducationFees = 500000, RequestedAmount = 400000, RequestedTenureMonths = 36, EducationPurpose = "x",
            });
        await db.SaveChangesAsync();

        var result = await new GetMyApplicationsQueryHandler(db).Handle(new GetMyApplicationsQuery(employeeA), default);

        Assert.Single(result);
        Assert.Equal("EDL-2026-A00001", result[0].ApplicationNumber);
    }
}

internal static class MockExtensions
{
    // Small fluent helper so FixedDate() can configure-and-return in one expression.
    public static T Also<T>(this T obj, Action<T> configure)
    {
        configure(obj);
        return obj;
    }
}
