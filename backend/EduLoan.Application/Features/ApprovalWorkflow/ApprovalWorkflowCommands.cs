using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.ApprovalWorkflow;

// Single place where every status transition is validated, applied, and audit-logged,
// so each command below is just "which states may I come from, what do I become".
internal static class WorkflowTransition
{
    public static async Task<WorkflowResultDto> ApplyAsync(
        IAppDbContext db, Guid applicationId, Guid actorUserId,
        ApplicationStatus[] allowedFrom, ApplicationStatus newStatus,
        WorkflowAction action, string? comments, CancellationToken ct)
    {
        var application = await db.LoanApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException(nameof(LoanApplication), applicationId);

        var actor = await db.Users.FirstOrDefaultAsync(u => u.Id == actorUserId, ct)
            ?? throw new NotFoundException(nameof(User), actorUserId);

        // Reviewers can't act on their own application (separation of duties).
        if (application.EmployeeId == actorUserId)
            throw new ForbiddenException("You cannot review or process your own loan application.");

        if (!allowedFrom.Contains(application.Status))
            throw new InUseException(
                $"This action isn't available while the application is '{application.Status}'.");

        application.Status = newStatus;
        application.UpdatedAt = DateTime.UtcNow;

        db.AddApprovalWorkflowStep(new ApprovalWorkflowStep
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            ActorUserId = actor.Id,
            ActorName = actor.FullName,
            ActorRole = actor.Role.ToString(),
            Action = action,
            Comments = string.IsNullOrWhiteSpace(comments) ? null : comments.Trim(),
        });

        await db.SaveChangesAsync(ct);
        return new WorkflowResultDto(application.Id, application.ApplicationNumber, application.Status.ToString());
    }
}

// ---- Submitted -> UnderReview ----
public record StartReviewCommand(Guid ApplicationId, Guid ActorUserId) : IRequest<WorkflowResultDto>;

public class StartReviewCommandHandler : IRequestHandler<StartReviewCommand, WorkflowResultDto>
{
    private readonly IAppDbContext _db;
    public StartReviewCommandHandler(IAppDbContext db) => _db = db;

    public Task<WorkflowResultDto> Handle(StartReviewCommand r, CancellationToken ct) =>
        WorkflowTransition.ApplyAsync(_db, r.ApplicationId, r.ActorUserId,
            new[] { ApplicationStatus.Submitted }, ApplicationStatus.UnderReview,
            WorkflowAction.StartedReview, null, ct);
}

// ---- UnderReview -> ManualReview ----
public record EscalateToManualReviewCommand(Guid ApplicationId, Guid ActorUserId, string? Comments)
    : IRequest<WorkflowResultDto>;

public class EscalateToManualReviewCommandHandler : IRequestHandler<EscalateToManualReviewCommand, WorkflowResultDto>
{
    private readonly IAppDbContext _db;
    public EscalateToManualReviewCommandHandler(IAppDbContext db) => _db = db;

    public Task<WorkflowResultDto> Handle(EscalateToManualReviewCommand r, CancellationToken ct) =>
        WorkflowTransition.ApplyAsync(_db, r.ApplicationId, r.ActorUserId,
            new[] { ApplicationStatus.UnderReview }, ApplicationStatus.ManualReview,
            WorkflowAction.EscalatedToManualReview, r.Comments, ct);
}

// ---- UnderReview / ManualReview -> InfoRequested (comments = what the employee must provide) ----
public record RequestInfoCommand(Guid ApplicationId, Guid ActorUserId, string Comments) : IRequest<WorkflowResultDto>;

public class RequestInfoCommandValidator : AbstractValidator<RequestInfoCommand>
{
    public RequestInfoCommandValidator()
    {
        RuleFor(x => x.Comments).NotEmpty().WithMessage("Tell the employee what information is needed.")
            .MaximumLength(500);
    }
}

public class RequestInfoCommandHandler : IRequestHandler<RequestInfoCommand, WorkflowResultDto>
{
    private readonly IAppDbContext _db;
    public RequestInfoCommandHandler(IAppDbContext db) => _db = db;

    public Task<WorkflowResultDto> Handle(RequestInfoCommand r, CancellationToken ct) =>
        WorkflowTransition.ApplyAsync(_db, r.ApplicationId, r.ActorUserId,
            new[] { ApplicationStatus.UnderReview, ApplicationStatus.ManualReview },
            ApplicationStatus.InfoRequested, WorkflowAction.RequestedInfo, r.Comments, ct);
}

// ---- UnderReview / ManualReview -> Approved ----
public record ApproveApplicationCommand(Guid ApplicationId, Guid ActorUserId, string? Comments)
    : IRequest<WorkflowResultDto>;

public class ApproveApplicationCommandHandler : IRequestHandler<ApproveApplicationCommand, WorkflowResultDto>
{
    private readonly IAppDbContext _db;
    public ApproveApplicationCommandHandler(IAppDbContext db) => _db = db;

    public Task<WorkflowResultDto> Handle(ApproveApplicationCommand r, CancellationToken ct) =>
        WorkflowTransition.ApplyAsync(_db, r.ApplicationId, r.ActorUserId,
            new[] { ApplicationStatus.UnderReview, ApplicationStatus.ManualReview },
            ApplicationStatus.Approved, WorkflowAction.Approved, r.Comments, ct);
}

// ---- UnderReview / ManualReview -> Rejected (reason mandatory) ----
public record RejectApplicationCommand(Guid ApplicationId, Guid ActorUserId, string Comments)
    : IRequest<WorkflowResultDto>;

public class RejectApplicationCommandValidator : AbstractValidator<RejectApplicationCommand>
{
    public RejectApplicationCommandValidator()
    {
        RuleFor(x => x.Comments).NotEmpty().WithMessage("A reason is required to reject an application.")
            .MaximumLength(500);
    }
}

public class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand, WorkflowResultDto>
{
    private readonly IAppDbContext _db;
    public RejectApplicationCommandHandler(IAppDbContext db) => _db = db;

    public Task<WorkflowResultDto> Handle(RejectApplicationCommand r, CancellationToken ct) =>
        WorkflowTransition.ApplyAsync(_db, r.ApplicationId, r.ActorUserId,
            new[] { ApplicationStatus.UnderReview, ApplicationStatus.ManualReview },
            ApplicationStatus.Rejected, WorkflowAction.Rejected, r.Comments, ct);
}

// ---- Approved -> PendingFinance: hands approved loan details to the finance team.
// The system only records the handoff; the actual money transfer stays outside it. ----
public record SendToFinanceCommand(Guid ApplicationId, Guid ActorUserId) : IRequest<WorkflowResultDto>;

public class SendToFinanceCommandHandler : IRequestHandler<SendToFinanceCommand, WorkflowResultDto>
{
    private readonly IAppDbContext _db;
    public SendToFinanceCommandHandler(IAppDbContext db) => _db = db;

    public Task<WorkflowResultDto> Handle(SendToFinanceCommand r, CancellationToken ct) =>
        WorkflowTransition.ApplyAsync(_db, r.ApplicationId, r.ActorUserId,
            new[] { ApplicationStatus.Approved }, ApplicationStatus.PendingFinance,
            WorkflowAction.SentToFinance, "Approved loan details sent to the finance team.", ct);
}

// ---- PendingFinance -> Processed: finance confirms the amount was processed externally ----
public record ConfirmProcessedCommand(Guid ApplicationId, Guid ActorUserId, string ConfirmationReference)
    : IRequest<WorkflowResultDto>;

public class ConfirmProcessedCommandValidator : AbstractValidator<ConfirmProcessedCommand>
{
    public ConfirmProcessedCommandValidator()
    {
        RuleFor(x => x.ConfirmationReference).NotEmpty()
            .WithMessage("A finance confirmation/reference number is required.")
            .MaximumLength(100);
    }
}

public class ConfirmProcessedCommandHandler : IRequestHandler<ConfirmProcessedCommand, WorkflowResultDto>
{
    private readonly IAppDbContext _db;
    public ConfirmProcessedCommandHandler(IAppDbContext db) => _db = db;

    public Task<WorkflowResultDto> Handle(ConfirmProcessedCommand r, CancellationToken ct) =>
        WorkflowTransition.ApplyAsync(_db, r.ApplicationId, r.ActorUserId,
            new[] { ApplicationStatus.PendingFinance }, ApplicationStatus.Processed,
            WorkflowAction.ConfirmedProcessed, $"Finance confirmation ref: {r.ConfirmationReference.Trim()}", ct);
}
