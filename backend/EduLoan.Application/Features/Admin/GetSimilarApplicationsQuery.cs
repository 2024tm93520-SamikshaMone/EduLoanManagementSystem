using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.AdminApplications;

public sealed record GetSimilarApplicationsQuery(Guid ApplicationId)
    : IRequest<SimilarApplicationsSummary>;

public sealed class GetSimilarApplicationsQueryHandler
    : IRequestHandler<GetSimilarApplicationsQuery, SimilarApplicationsSummary>
{
    private readonly IAppDbContext _db;
    private readonly SimilarApplicationKnn _knn;

    public GetSimilarApplicationsQueryHandler(IAppDbContext db, SimilarApplicationKnn knn)
    {
        _db = db;
        _knn = knn;
    }

    public async Task<SimilarApplicationsSummary> Handle(GetSimilarApplicationsQuery request, CancellationToken ct)
    {
        var target = await _db.LoanApplications
            .AsNoTracking()
            .Include(x => x.College)
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException("LoanApplication", request.ApplicationId);

        var historical = await _db.LoanApplications
            .AsNoTracking()
            .Where(x => x.Id != target.Id &&
                        (x.Status == EduLoan.Domain.Entities.ApplicationStatus.Approved ||
                         x.Status == EduLoan.Domain.Entities.ApplicationStatus.Rejected))
            .Include(x => x.College)
            .Include(x => x.Course)
            .ToListAsync(ct);

        var evaluationRows = await _db.RuleEvaluationResults
            .AsNoTracking()
            .Where(x => x.ApplicationId == target.Id)
            .OrderByDescending(x => x.EvaluatedAt)
            .ToListAsync(ct);
        var evaluations = evaluationRows.Select(x => new EligibilityEvaluationSummary(
            x.RuleName,
            x.RuleCategory,
            x.Severity.ToString(),
            x.Passed,
            x.EvaluatedValue,
            x.FailureMessage,
            x.EvaluatedAt)).ToList();

        return _knn.Evaluate(target, historical) with { EligibilityEvaluations = evaluations };
    }
}
