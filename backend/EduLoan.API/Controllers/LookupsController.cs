using EduLoan.Application.Common;
using EduLoan.Application.Features.Lookups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/lookups")]
[Authorize] // any authenticated role — not Admin-only, unlike AdminController's full CRUD
public class LookupsController : ControllerBase
{
    private readonly IMediator _mediator;
    public LookupsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("colleges")]
    public async Task<ActionResult<ApiResponse<List<LookupDto>>>> GetColleges(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetActiveCollegesLookupQuery(), ct);
        return Ok(ApiResponse<List<LookupDto>>.SuccessResponse(result));
    }

    [HttpGet("courses")]
    public async Task<ActionResult<ApiResponse<List<LookupDto>>>> GetCourses(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetActiveCoursesLookupQuery(), ct);
        return Ok(ApiResponse<List<LookupDto>>.SuccessResponse(result));
    }
}
