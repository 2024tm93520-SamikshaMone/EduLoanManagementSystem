
using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.AdminApplications;

public sealed record ApplicationConsistencyFlag(
    string Code,
    string Title,
    string Severity,
    string Description);

public sealed record DuplicateApplicationReference(
    Guid ApplicationId,
    string ApplicationNumber,
    string Status);

public sealed record ApplicationConsistencySummary(
    Guid ApplicationId,
    IReadOnlyList<ApplicationConsistencyFlag> Flags,
    IReadOnlyList<DuplicateApplicationReference> PotentialDuplicates,
    string Disclaimer);

/// <summary>
/// Deterministic data-quality checks for human review.
/// These checks do not determine fraud or loan outcomes.
/// </summary>
public sealed class ApplicationConsistencyChecker
{
    public IReadOnlyList<ApplicationConsistencyFlag> Evaluate(
        LoanApplication application)
    {
        var flags = new List<ApplicationConsistencyFlag>();

        // Validate education fees.
        if (application.TotalEducationFees <= 0)
        {
            flags.Add(new ApplicationConsistencyFlag(
                "FEES_NOT_POSITIVE",
                "Education fees need verification",
                "High",
                "Total education fees must be greater than zero."));
        }

        // Validate requested loan amount.
        if (application.RequestedAmount <= 0)
        {
            flags.Add(new ApplicationConsistencyFlag(
                "REQUESTED_AMOUNT_NOT_POSITIVE",
                "Requested amount needs verification",
                "High",
                "The requested loan amount must be greater than zero."));
        }
        else if (
            application.TotalEducationFees > 0 &&
            application.RequestedAmount > application.TotalEducationFees)
        {
            flags.Add(new ApplicationConsistencyFlag(
                "AMOUNT_EXCEEDS_FEES",
                "Requested amount exceeds education fees",
                "High",
                "The requested amount is greater than the declared total education fees. Verify the application and fee structure."));
        }

        // Validate course duration.
        if (application.CourseDurationMonths <= 0)
        {
            flags.Add(new ApplicationConsistencyFlag(
                "INVALID_COURSE_DURATION",
                "Course duration needs verification",
                "Medium",
                "Course duration must be greater than zero months."));
        }

        // Validate requested loan tenure.
        if (application.RequestedTenureMonths <= 0)
        {
            flags.Add(new ApplicationConsistencyFlag(
                "INVALID_REQUESTED_TENURE",
                "Requested tenure needs verification",
                "Medium",
                "Requested loan tenure must be greater than zero months."));
        }

        // Identify required document types.
        var documentTypes = application.Documents
            .Select(d => NormalizeDocumentType(d.DocumentType))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!documentTypes.Contains("admissionletter"))
        {
            flags.Add(new ApplicationConsistencyFlag(
                "ADMISSION_LETTER_MISSING",
                "Admission letter not attached",
                "Medium",
                "No admission letter is attached to this application. Confirm the document before completing review."));
        }

        if (
            !documentTypes.Contains("feestructure") &&
            !documentTypes.Contains("universityfeestructure"))
        {
            flags.Add(new ApplicationConsistencyFlag(
                "FEE_STRUCTURE_MISSING",
                "Fee structure not attached",
                "Medium",
                "No fee structure document is attached to this application. Confirm the education fees before completing review."));
        }

        return flags;
    }

    private static string NormalizeDocumentType(string value)
    {
        return new string(
            value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
    }
}

public sealed record GetApplicationConsistencyChecksQuery(
    Guid ApplicationId)
    : IRequest<ApplicationConsistencySummary>;

public sealed class GetApplicationConsistencyChecksQueryHandler
    : IRequestHandler<
        GetApplicationConsistencyChecksQuery,
        ApplicationConsistencySummary>
{
    private readonly IAppDbContext _db;
    private readonly ApplicationConsistencyChecker _checker;

    public GetApplicationConsistencyChecksQueryHandler(
        IAppDbContext db,
        ApplicationConsistencyChecker checker)
    {
        _db = db;
        _checker = checker;
    }

    public async Task<ApplicationConsistencySummary> Handle(
        GetApplicationConsistencyChecksQuery request,
        CancellationToken ct)
    {
        // Load the requested application and its documents.
        var target = await _db.LoanApplications
            .AsNoTracking()
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(
                a => a.Id == request.ApplicationId,
                ct)
            ?? throw new NotFoundException(
                nameof(LoanApplication),
                request.ApplicationId);

        // Exclude statuses that should not count as potential duplicates.
        // Individual comparisons avoid the collection-based Contains
        // expression in the EF Core query.
        var duplicates = await _db.LoanApplications
            .AsNoTracking()
            .Where(a =>
                a.Id != target.Id &&
                a.EmployeeId == target.EmployeeId &&
                a.CollegeId == target.CollegeId &&
                a.CourseId == target.CourseId &&
                a.Status != ApplicationStatus.Draft &&
                a.Status != ApplicationStatus.Cancelled &&
                a.Status != ApplicationStatus.Rejected)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new DuplicateApplicationReference(
                a.Id,
                a.ApplicationNumber,
                a.Status.ToString()))
            .ToListAsync(ct);

        // Evaluate deterministic consistency checks.
        var flags = _checker.Evaluate(target).ToList();

        // Potential duplicates are review flags, not proof of fraud.
        if (duplicates.Count > 0)
        {
            flags.Add(new ApplicationConsistencyFlag(
                "POTENTIAL_DUPLICATE",
                "Potential duplicate application",
                "High",
                $"Found {duplicates.Count} other non-cancelled, non-rejected application(s) for the same employee, college, and course. Review the records; this is not a fraud determination."));
        }

        return new ApplicationConsistencySummary(
            target.Id,
            flags,
            duplicates,
            "These deterministic checks identify data inconsistencies for Admin review only. They do not establish fraud and do not approve or reject an application.");
    }
}
