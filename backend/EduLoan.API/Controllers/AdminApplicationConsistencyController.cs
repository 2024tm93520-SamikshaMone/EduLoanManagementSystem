using EduLoan.Application.Common;
using EduLoan.Application.Features.AdminApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/admin/applications")]
[Authorize(Roles = "Admin")]
public sealed class AdminApplicationConsistencyController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminApplicationConsistencyController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{applicationId:guid}/consistency-checks")]
    public async Task<ActionResult<ApiResponse<ApplicationConsistencySummary>>> GetChecks(
        Guid applicationId, CancellationToken ct)
    {
        var summary = await _mediator.Send(new GetApplicationConsistencyChecksQuery(applicationId), ct);
        return Ok(ApiResponse<ApplicationConsistencySummary>.SuccessResponse(summary));
    }
}
