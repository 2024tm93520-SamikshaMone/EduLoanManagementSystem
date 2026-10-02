using EduLoan.Application.Common;
using EduLoan.Domain.Entities;
using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.ApprovalWorkflow;

public record GetWorkflowHistoryQuery(Guid ApplicationId) : IRequest<List<WorkflowStepDto>>;

public class GetWorkflowHistoryQueryHandler : IRequestHandler<GetWorkflowHistoryQuery, List<WorkflowStepDto>>
{
    private readonly IAppDbContext _db;
    public GetWorkflowHistoryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<WorkflowStepDto>> Handle(GetWorkflowHistoryQuery request, CancellationToken ct)
    {
        var exists = await _db.LoanApplications.AnyAsync(a => a.Id == request.ApplicationId, ct);
        if (!exists) throw new NotFoundException(nameof(LoanApplication), request.ApplicationId);

        return await _db.ApprovalWorkflowSteps
            .Where(w => w.ApplicationId == request.ApplicationId)
            .OrderBy(w => w.ActionedAt)
            .Select(w => new WorkflowStepDto(w.Id, w.ActorName, w.ActorRole, w.Action.ToString(), w.Comments, w.ActionedAt))
            .ToListAsync(ct);
    }
}
