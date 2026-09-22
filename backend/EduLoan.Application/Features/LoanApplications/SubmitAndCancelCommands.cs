using EduLoan.Application.Common;
using EduLoan.Application.Features.EligibilityRules;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.LoanApplications;

// ---- Submit: Draft -> Submitted, gated by the eligibility rule engine ----
public record SubmitLoanApplicationCommand(Guid ApplicationId, Guid RequestingUserId) : IRequest<LoanApplicationDetailDto>;

public class SubmitLoanApplicationCommandHandler : IRequestHandler<SubmitLoanApplicationCommand, LoanApplicationDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IEligibilityRuleEngine _ruleEngine;

    public SubmitLoanApplicationCommandHandler(IAppDbContext db, IEligibilityRuleEngine ruleEngine)
    {
        _db = db;
        _ruleEngine = ruleEngine;
    }

    public async Task<LoanApplicationDetailDto> Handle(SubmitLoanApplicationCommand request, CancellationToken ct)
    {
        var application = await _db.LoanApplications
            .Include(a => a.College).Include(a => a.Course).Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(LoanApplication), request.ApplicationId);

        if (application.EmployeeId != request.RequestingUserId)
            throw new ForbiddenException("You can only submit your own application.");

        if (application.Status != ApplicationStatus.Draft)
            throw new InUseException("Only draft applications can be submitted.");

        if (application.Documents.Count == 0)
            throw new InUseException("At least one supporting document is required before submitting.");

        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == application.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(User), application.EmployeeId);

        // Tenure calculation mirrors GetEmployeeProfileQueryHandler.CalculateTenureMonths
        // (Module 2) — duplicated here rather than shared to keep the rule engine's
        // Application-layer dependencies minimal; both use the same UTC-today convention.
        var tenureMonths = CalculateTenureMonths(employee.DateOfJoining, DateOnly.FromDateTime(DateTime.UtcNow));

        var context = new RuleEvaluationContext
        {
            TenureMonths = tenureMonths,
            MonthlySalary = employee.MonthlySalary,
            RequestedAmount = application.RequestedAmount,
            RequestedTenureMonths = application.RequestedTenureMonths,
            TotalEducationFees = application.TotalEducationFees,
        };

        var activeRules = await _db.EligibilityRules.Where(r => r.IsActive).ToListAsync(ct);
        var outcomes = _ruleEngine.Evaluate(context, activeRules);

        // Every rule's outcome is recorded — passed AND failed — so the audit trail shows
        // the complete picture, not just what went wrong. This is what the application
        // detail view surfaces for transparency (dissertation objective: showing which
        // rules contributed to a decision).
        var evaluationRows = outcomes.Select(o => new RuleEvaluationResult
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            RuleId = o.RuleId,
            RuleName = o.RuleName,
            RuleCategory = o.RuleCategory,
            Severity = Enum.Parse<RuleSeverity>(o.Severity),
            Passed = o.Passed,
            EvaluatedValue = o.EvaluatedValue.ToString("0.##"),
            FailureMessage = o.FailureMessage,
        }).ToList();

        _db.AddRuleEvaluationResults(evaluationRows);

        var blockingFailures = outcomes
            .Where(o => !o.Passed && o.Severity == RuleSeverity.Blocking.ToString())
            .Select(o => o.FailureMessage!)
            .ToList();

        if (blockingFailures.Count > 0)
        {
            // Still save the evaluation rows even on rejection — the applicant (and an
            // admin later) can see exactly what failed, not just "try again".
            await _db.SaveChangesAsync(ct);
            throw new EligibilityFailedException(blockingFailures);
        }

        application.Status = ApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return application.ToDetailDto(evaluationRows);
    }

    internal static int CalculateTenureMonths(DateOnly joiningDate, DateOnly today)
    {
        if (joiningDate > today) return 0;
        int months = (today.Year - joiningDate.Year) * 12 + (today.Month - joiningDate.Month);
        if (today.Day < joiningDate.Day) months--;
        return Math.Max(months, 0);
    }
}

// ---- Cancel: Draft or Submitted -> Cancelled ----
public record CancelLoanApplicationCommand(Guid ApplicationId, Guid RequestingUserId) : IRequest<LoanApplicationDetailDto>;

public class CancelLoanApplicationCommandHandler : IRequestHandler<CancelLoanApplicationCommand, LoanApplicationDetailDto>
{
    private readonly IAppDbContext _db;
    public CancelLoanApplicationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<LoanApplicationDetailDto> Handle(CancelLoanApplicationCommand request, CancellationToken ct)
    {
        var application = await _db.LoanApplications
            .Include(a => a.College).Include(a => a.Course).Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(LoanApplication), request.ApplicationId);

        if (application.EmployeeId != request.RequestingUserId)
            throw new ForbiddenException("You can only cancel your own application.");

        if (application.Status is not (ApplicationStatus.Draft or ApplicationStatus.Submitted))
            throw new InUseException("This application can no longer be cancelled.");

        application.Status = ApplicationStatus.Cancelled;
        application.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return application.ToDetailDto();
    }
}
