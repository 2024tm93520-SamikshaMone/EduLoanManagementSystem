using EduLoan.Application.Common;
using EduLoan.Application.Features.Employees;
using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public EmployeesController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public record UpdateProfileRequest(string PhoneNumber);

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> GetMyProfile(CancellationToken ct)
    {
        var userId = _currentUser.UserId!.Value; // [Authorize] guarantees this is set
        var result = await _mediator.Send(new GetEmployeeProfileQuery(userId), ct);
        return Ok(ApiResponse<EmployeeProfileDto>.SuccessResponse(result));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<EmployeeProfileDto>>> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = _currentUser.UserId!.Value;
        var result = await _mediator.Send(new UpdateEmployeeProfileCommand(userId, request.PhoneNumber), ct);
        return Ok(ApiResponse<EmployeeProfileDto>.SuccessResponse(result, "Profile updated."));
    }
}
