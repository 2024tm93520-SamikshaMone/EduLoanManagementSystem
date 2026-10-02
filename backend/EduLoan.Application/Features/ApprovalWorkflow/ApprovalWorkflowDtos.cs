namespace EduLoan.Application.Features.ApprovalWorkflow;

public record WorkflowResultDto(Guid ApplicationId, string ApplicationNumber, string Status);

public record WorkflowStepDto(
    Guid Id, string ActorName, string ActorRole, string Action, string? Comments, DateTime ActionedAt
);

internal static class WorkflowStepMapper
{
    public static WorkflowStepDto ToDto(this EduLoan.Domain.Entities.ApprovalWorkflowStep s) =>
        new(s.Id, s.ActorName, s.ActorRole, s.Action.ToString(), s.Comments, s.ActionedAt);
}
