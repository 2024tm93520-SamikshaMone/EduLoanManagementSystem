using EduLoan.Application.Common;
using EduLoan.Application.Features.ApprovalWorkflow;
using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

// Separate controller (not folded into AdminController) so workflow actions remain separated from the AdminController
// authorized per-action without altering AdminController's class-level Admin-only gate.
[ApiController]
[Route("api/v1/admin/applications/{applicationId:guid}")]
[Authorize]
public class ApprovalWorkflowController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IAppDbContext _db;

    public ApprovalWorkflowController(IMediator mediator, ICurrentUserService currentUser, IAppDbContext db)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _db = db;
    }

    private Guid ActorId => _currentUser.UserId!.Value;

    public record CommentRequest(string? Comments);
    public record ConfirmProcessedRequest(string ConfirmationReference);

    [HttpGet("workflow-history")]
    public async Task<ActionResult<ApiResponse<List<WorkflowStepDto>>>> GetHistory(Guid applicationId, CancellationToken ct)
    {
        var application = await _db.LoanApplications.FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application is null)
            return NotFound(ApiResponse<List<WorkflowStepDto>>.FailureResponse("Application not found."));
        if (User.IsInRole("Employee") && application.EmployeeId != ActorId)
            return Forbid();
        if (!User.IsInRole("Employee") && !User.IsInRole("Admin") && !User.IsInRole("HR") && !User.IsInRole("Finance"))
            return Forbid();

        var result = await _mediator.Send(new GetWorkflowHistoryQuery(applicationId), ct);
        return Ok(ApiResponse<List<WorkflowStepDto>>.SuccessResponse(result));
    }

    [HttpPost("start-review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> StartReview(Guid applicationId, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartReviewCommand(applicationId, ActorId), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Review started."));
    }

    [HttpPost("manual-review")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> EscalateToManualReview(
        Guid applicationId, [FromBody] CommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new EscalateToManualReviewCommand(applicationId, ActorId, request.Comments), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Escalated to manual review."));
    }

    [HttpPost("request-info")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> RequestInfo(
        Guid applicationId, [FromBody] CommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RequestInfoCommand(applicationId, ActorId, request.Comments ?? string.Empty), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Additional information requested."));
    }

    [HttpPost("approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<WorkflowResultDto>>> Approve(
        Guid applicationId, [FromBody] CommentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ApproveApplicationCommand(applicationId, ActorId, request.Comments), ct);
        return Ok(ApiResponse<WorkflowResultDto>.SuccessResponse(result, "Application approved."));
    }

    [HttpPost("reject")]
    [Authorize(Roles = "Admin")]
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
