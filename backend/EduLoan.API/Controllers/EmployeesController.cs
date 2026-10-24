using EduLoan.Application.Common;
using EduLoan.Application.Features.Employees;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
    private readonly IAppDbContext _db;

    public EmployeesController(IMediator mediator, ICurrentUserService currentUser, IAppDbContext db)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _db = db;
    }

    public record UpdateProfileRequest(string PhoneNumber);
    public record UpdateEmploymentTypeRequest(string? EmploymentType);

    // Employment type is HR-controlled master data, not employee-editable profile data.
    // Values are copied from the authoritative HR record; this endpoint intentionally
    // does not define or default an employment-type vocabulary.
    [HttpPut("{employeeId:guid}/employment-type")]
    [Authorize(Roles = "Admin,HR")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateEmploymentType(
        Guid employeeId, [FromBody] UpdateEmploymentTypeRequest request, CancellationToken ct)
    {
        var employee = await _db.Users.FirstOrDefaultAsync(u => u.Id == employeeId, ct);
        if (employee is null)
            return NotFound(ApiResponse<object>.FailureResponse("Employee profile was not found."));

        var value = string.IsNullOrWhiteSpace(request.EmploymentType) ? null : request.EmploymentType.Trim();
        if (value?.Length > 50)
            return BadRequest(ApiResponse<object>.FailureResponse("Employment type must not exceed 50 characters."));

        employee.EmploymentType = value;
        employee.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { employee.Id, employee.EmploymentType }, "Employment type updated from the HR record."));
    }

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
