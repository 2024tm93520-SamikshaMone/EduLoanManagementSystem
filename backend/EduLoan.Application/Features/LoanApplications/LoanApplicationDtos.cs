using EduLoan.Domain.Entities;

namespace EduLoan.Application.Features.LoanApplications;

public record ApplicationDocumentDto(Guid Id, string DocumentType, string FileName, long FileSizeBytes, DateTime UploadedAt);

public record EligibilityResultDto(
    int RuleId, string RuleName, string RuleCategory, string Severity,
    bool Passed, string EvaluatedValue, string? FailureMessage
);

public record LoanApplicationSummaryDto(
    Guid Id,
    string ApplicationNumber,
    string CollegeName,
    string CourseName,
    decimal RequestedAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? SubmittedAt
);

public record LoanApplicationDetailDto(
    Guid Id,
    string ApplicationNumber,
    Guid EmployeeId,
    int CollegeId,
    string CollegeName,
    int CourseId,
    string CourseName,
    string Specialization,
    int CourseDurationMonths,
    decimal TotalEducationFees,
    decimal RequestedAmount,
    int RequestedTenureMonths,
    string EducationPurpose,
    string Status,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    List<ApplicationDocumentDto> Documents,
    List<EligibilityResultDto> EligibilityResults
);

internal static class LoanApplicationMapper
{
    public static LoanApplicationSummaryDto ToSummaryDto(this LoanApplication a) => new(
        a.Id, a.ApplicationNumber, a.College?.Name ?? "", a.Course?.Name ?? "",
        a.RequestedAmount, a.Status.ToString(), a.CreatedAt, a.SubmittedAt);

    // eligibilityResults is optional because not every call site has them loaded
    // (e.g. right after Create, before any evaluation has ever run).
    public static LoanApplicationDetailDto ToDetailDto(
        this LoanApplication a, List<RuleEvaluationResult>? eligibilityResults = null) => new(
        a.Id, a.ApplicationNumber, a.EmployeeId,
        a.CollegeId, a.College?.Name ?? "",
        a.CourseId, a.Course?.Name ?? "",
        a.Specialization, a.CourseDurationMonths, a.TotalEducationFees,
        a.RequestedAmount, a.RequestedTenureMonths, a.EducationPurpose,
        a.Status.ToString(), a.CreatedAt, a.SubmittedAt,
        a.Documents.Select(d => new ApplicationDocumentDto(d.Id, d.DocumentType, d.FileName, d.FileSizeBytes, d.UploadedAt)).ToList(),
        (eligibilityResults ?? new List<RuleEvaluationResult>())
            .Select(r => new EligibilityResultDto(
                r.RuleId, r.RuleName, r.RuleCategory, r.Severity.ToString(), r.Passed, r.EvaluatedValue, r.FailureMessage))
            .ToList()
    );
}
