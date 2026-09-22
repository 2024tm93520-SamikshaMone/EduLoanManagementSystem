using EduLoan.Application.Common;
using EduLoan.Application.Features.EligibilityRules;
using EduLoan.Application.Features.MasterData.Colleges;
using EduLoan.Application.Features.MasterData.Courses;
using EduLoan.Application.Features.MasterData.Departments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EduLoan.Application.Features.AdminDashboard;
using EduLoan.Application.Features.AdminApplications;
using EduLoan.Application.Features.AdminReports;


namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminController(IMediator mediator) => _mediator = mediator;

    // ---------------- Departments ----------------

    public record UpsertDepartmentRequest(string Name, bool IsActive = true);

    [HttpGet("departments")]
    public async Task<ActionResult<ApiResponse<List<DepartmentDto>>>> GetDepartments(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDepartmentsQuery(), ct);
        return Ok(ApiResponse<List<DepartmentDto>>.SuccessResponse(result));
    }

    [HttpPost("departments")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> CreateDepartment(
        [FromBody] UpsertDepartmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDepartmentCommand(request.Name), ct);
        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(result, "Department created."));
    }

    [HttpPut("departments/{id:int}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> UpdateDepartment(
        int id, [FromBody] UpsertDepartmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDepartmentCommand(id, request.Name, request.IsActive), ct);
        return Ok(ApiResponse<DepartmentDto>.SuccessResponse(result, "Department updated."));
    }

    [HttpDelete("departments/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDepartment(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDepartmentCommand(id), ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Department deleted."));
    }

    // ---------------- Colleges ----------------

    public record UpsertCollegeRequest(string Name, string City, bool IsActive = true);

    [HttpGet("colleges")]
    public async Task<ActionResult<ApiResponse<List<CollegeDto>>>> GetColleges(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCollegesQuery(), ct);
        return Ok(ApiResponse<List<CollegeDto>>.SuccessResponse(result));
    }

    [HttpPost("colleges")]
    public async Task<ActionResult<ApiResponse<CollegeDto>>> CreateCollege(
        [FromBody] UpsertCollegeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCollegeCommand(request.Name, request.City), ct);
        return Ok(ApiResponse<CollegeDto>.SuccessResponse(result, "College created."));
    }

    [HttpPut("colleges/{id:int}")]
    public async Task<ActionResult<ApiResponse<CollegeDto>>> UpdateCollege(
        int id, [FromBody] UpsertCollegeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCollegeCommand(id, request.Name, request.City, request.IsActive), ct);
        return Ok(ApiResponse<CollegeDto>.SuccessResponse(result, "College updated."));
    }

    [HttpDelete("colleges/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCollege(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCollegeCommand(id), ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "College deleted."));
    }

    // ---------------- Courses ----------------

    public record UpsertCourseRequest(string Name, string Level, int StandardDurationMonths, bool IsActive = true);

    [HttpGet("courses")]
    public async Task<ActionResult<ApiResponse<List<CourseDto>>>> GetCourses(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCoursesQuery(), ct);
        return Ok(ApiResponse<List<CourseDto>>.SuccessResponse(result));
    }

    [HttpPost("courses")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> CreateCourse(
        [FromBody] UpsertCourseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new CreateCourseCommand(request.Name, request.Level, request.StandardDurationMonths), ct);
        return Ok(ApiResponse<CourseDto>.SuccessResponse(result, "Course created."));
    }

    [HttpPut("courses/{id:int}")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> UpdateCourse(
        int id, [FromBody] UpsertCourseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new UpdateCourseCommand(id, request.Name, request.Level, request.StandardDurationMonths, request.IsActive), ct);
        return Ok(ApiResponse<CourseDto>.SuccessResponse(result, "Course updated."));
    }

    [HttpDelete("courses/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCourse(int id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteCourseCommand(id), ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { }, "Course deleted."));
    }
    // ---------------- Dashboard ----------------

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> GetDashboard(
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAdminDashboardQuery(),
            ct);

        return Ok(
            ApiResponse<AdminDashboardDto>.SuccessResponse(result)
        );
    }
    // ---------------- Loan Applications ----------------

    [HttpGet("applications")]
    public async Task<ActionResult<ApiResponse<List<AdminLoanApplicationDto>>>> GetApplications(
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAdminLoanApplicationsQuery(),
            ct);

        return Ok(
            ApiResponse<List<AdminLoanApplicationDto>>.SuccessResponse(result)
        );
    }

    [HttpGet("applications/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminLoanApplicationDetailDto>>> GetApplication(
        Guid id,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAdminLoanApplicationByIdQuery(id),
            ct);

        return Ok(
            ApiResponse<AdminLoanApplicationDetailDto>.SuccessResponse(result)
        );
    }
    // ---------------- Employees ----------------

    [HttpGet("employees")]
    public async Task<ActionResult<ApiResponse<List<AdminEmployeeDto>>>> GetEmployees(
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAdminEmployeesQuery(),
            ct);

        return Ok(
            ApiResponse<List<AdminEmployeeDto>>.SuccessResponse(result)
        );
    }

    // ---------------- Reports ----------------

    [HttpGet("reports")]
    public async Task<ActionResult<ApiResponse<AdminReportsDto>>> GetReports(
        [FromQuery] int? year,
        [FromQuery] int? departmentId,
        [FromQuery] int? courseId,
        [FromQuery] int? collegeId,
        CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAdminReportsQuery(
                year,
                departmentId,
                courseId,
                collegeId),
            ct);

        return Ok(
            ApiResponse<AdminReportsDto>.SuccessResponse(result)
        );
    }
}
