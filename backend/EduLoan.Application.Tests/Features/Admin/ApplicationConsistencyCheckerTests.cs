using EduLoan.Application.Features.AdminApplications;
using EduLoan.Domain.Entities;
using Xunit;

namespace EduLoan.Application.Tests.Features.Admin;

public sealed class ApplicationConsistencyCheckerTests
{
    private static LoanApplication ValidApplication() => new()
    {
        Id = Guid.NewGuid(), EmployeeId = Guid.NewGuid(), CollegeId = 1, CourseId = 1,
        ApplicationNumber = "EDU-TEST", Status = ApplicationStatus.Submitted,
        TotalEducationFees = 250000m, RequestedAmount = 200000m,
        CourseDurationMonths = 24, RequestedTenureMonths = 60,
        Documents = new List<ApplicationDocument>
        {
            new() { DocumentType = "Admission Letter" },
            new() { DocumentType = "Fee Structure" }
        }
    };

    [Fact]
    public void Evaluate_ValidApplication_ReturnsNoFlags()
    {
        var flags = new ApplicationConsistencyChecker().Evaluate(ValidApplication());
        Assert.Empty(flags);
    }

    [Fact]
    public void Evaluate_AmountExceedsFees_AddsHighSeverityFlag()
    {
        var application = ValidApplication();
        application.RequestedAmount = 300000m;

        var flags = new ApplicationConsistencyChecker().Evaluate(application);

        Assert.Contains(flags, x => x.Code == "AMOUNT_EXCEEDS_FEES" && x.Severity == "High");
    }

    [Fact]
    public void Evaluate_MissingAdmissionAndFeeDocuments_AddsDocumentFlags()
    {
        var application = ValidApplication();
        application.Documents.Clear();

        var flags = new ApplicationConsistencyChecker().Evaluate(application);

        Assert.Contains(flags, x => x.Code == "ADMISSION_LETTER_MISSING");
        Assert.Contains(flags, x => x.Code == "FEE_STRUCTURE_MISSING");
    }

    [Fact]
    public void Evaluate_InvalidAmountsAndDurations_AddsValidationFlags()
    {
        var application = ValidApplication();
        application.TotalEducationFees = 0m;
        application.RequestedAmount = 0m;
        application.CourseDurationMonths = 0;
        application.RequestedTenureMonths = 0;

        var flags = new ApplicationConsistencyChecker().Evaluate(application);

        Assert.Contains(flags, x => x.Code == "FEES_NOT_POSITIVE");
        Assert.Contains(flags, x => x.Code == "REQUESTED_AMOUNT_NOT_POSITIVE");
        Assert.Contains(flags, x => x.Code == "INVALID_COURSE_DURATION");
        Assert.Contains(flags, x => x.Code == "INVALID_REQUESTED_TENURE");
    }
}
