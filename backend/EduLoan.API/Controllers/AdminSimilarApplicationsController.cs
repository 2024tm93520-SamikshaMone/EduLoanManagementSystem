using EduLoan.Application.Common;
using EduLoan.Application.Features.AdminApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

/// <summary>Returns an explainable KNN summary from historical final outcomes; never decides an application.</summary>
[ApiController]
[Route("api/v1/admin/applications")]
[Authorize(Roles = "Admin")]
public sealed class AdminSimilarApplicationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminSimilarApplicationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{applicationId:guid}/similar-cases")]
    public async Task<ActionResult<ApiResponse<SimilarApplicationsSummary>>> GetSimilarCases(
        Guid applicationId,
        CancellationToken ct)
    {
        var summary = await _mediator.Send(new GetSimilarApplicationsQuery(applicationId), ct);
        return Ok(ApiResponse<SimilarApplicationsSummary>.SuccessResponse(summary));
    }
}
