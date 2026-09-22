using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.LoanApplications;

public record GetMyApplicationsQuery(Guid EmployeeId) : IRequest<List<LoanApplicationSummaryDto>>;

public class GetMyApplicationsQueryHandler : IRequestHandler<GetMyApplicationsQuery, List<LoanApplicationSummaryDto>>
{
    private readonly IAppDbContext _db;
    public GetMyApplicationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<LoanApplicationSummaryDto>> Handle(GetMyApplicationsQuery request, CancellationToken ct)
        => (await _db.LoanApplications
                .Include(a => a.College).Include(a => a.Course)
                .Where(a => a.EmployeeId == request.EmployeeId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct))
            .Select(a => a.ToSummaryDto())
            .ToList();
}

public record GetApplicationByIdQuery(Guid ApplicationId, Guid RequestingUserId) : IRequest<LoanApplicationDetailDto>;

public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, LoanApplicationDetailDto>
{
    private readonly IAppDbContext _db;
    public GetApplicationByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<LoanApplicationDetailDto> Handle(GetApplicationByIdQuery request, CancellationToken ct)
    {
        var application = await _db.LoanApplications
            .Include(a => a.College).Include(a => a.Course).Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(LoanApplication), request.ApplicationId);

        // Ownership check here (rather than only Admin/HR bypass) because only the
        // Employee portal exists so far — Admin/HR "view any application" is part
        // of the Approval Workflow module, not this one.
        if (application.EmployeeId != request.RequestingUserId)
            throw new ForbiddenException("You can only view your own application.");

        var eligibilityResults = await _db.RuleEvaluationResults
            .Where(r => r.ApplicationId == application.Id)
            .OrderBy(r => r.RuleCategory).ThenBy(r => r.RuleName)
            .ToListAsync(ct);

        return application.ToDetailDto(eligibilityResults);
    }
}
