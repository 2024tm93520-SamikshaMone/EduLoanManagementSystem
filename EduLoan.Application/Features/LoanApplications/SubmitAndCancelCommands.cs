using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.LoanApplications;

// ---- Submit: Draft -> Submitted ----
public record SubmitLoanApplicationCommand(Guid ApplicationId, Guid RequestingUserId) : IRequest<LoanApplicationDetailDto>;

public class SubmitLoanApplicationCommandHandler : IRequestHandler<SubmitLoanApplicationCommand, LoanApplicationDetailDto>
{
    private readonly IAppDbContext _db;
    public SubmitLoanApplicationCommandHandler(IAppDbContext db) => _db = db;

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

        application.Status = ApplicationStatus.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return application.ToDetailDto();
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
