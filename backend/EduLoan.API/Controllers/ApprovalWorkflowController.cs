using EduLoan.Application.Common;
using EduLoan.Application.Features.ApprovalWorkflow;
using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

// Separate controller (not folded into AdminController) so HR/Finance can be
// authorized per-action without altering AdminController's class-level Admin-only gate.
[ApiController]
[Route("api/v1/admin/applications/{applicationId:guid}")]
[Authorize]
public class ApprovalWorkflowController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ApprovalWorkflowController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    private Guid ActorId => _currentUser.UserId!.Value;

    public record CommentRequest(string? Comments);
    public record ConfirmProcessedRequest(string ConfirmationReference);

    [HttpGet("workflow-history")]
    public async Task<ActionResult<ApiResponse<List<WorkflowStepDto>>>> GetHistory(Guid applicationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWorkflowHistoryQuery(applicationId), ct);
        return Ok(ApiResponse<List<WorkflowStepDto>>.SuccessResponse(result));
    }

    [HttpPost("start-review")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> StartReview(Guid applicationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartReviewCommand(applicationId, ActorId), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Review started."));
    }

    [HttpPost("manual-review")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> EscalateToManualReview(
        Guid applicationId, [FromBody] CommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new EscalateToManualReviewCommand(applicationId, ActorId, request.Comments), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Escalated to manual review."));
    }

    [HttpPost("request-info")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> RequestInfo(
        Guid applicationId, [FromBody] CommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RequestInfoCommand(applicationId, ActorId, request.Comments ?? string.Empty), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Additional information requested."));
    }

    [HttpPost("approve")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> Approve(
        Guid applicationId, [FromBody] CommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApproveApplicationCommand(applicationId, ActorId, request.Comments), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Application approved."));
    }

    [HttpPost("reject")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> Reject(
        Guid applicationId, [FromBody] CommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RejectApplicationCommand(applicationId, ActorId, request.Comments ?? string.Empty), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Application rejected."));
    }

    [HttpPost("send-to-finance")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> SendToFinance(Guid applicationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SendToFinanceCommand(applicationId, ActorId), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Sent to finance."));
    }

    [HttpPost("confirm-processed")]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> ConfirmProcessed(
        Guid applicationId, [FromBody] ConfirmProcessedRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ConfirmProcessedCommand(applicationId, ActorId, request.ConfirmationReference), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Processing confirmed."));
    }
}
