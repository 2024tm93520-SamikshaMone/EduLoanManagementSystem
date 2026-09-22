using MediatR;

namespace EduLoan.Application.Features.AdminReports;

public record GetAdminReportsQuery(
    int? Year = null,
    int? DepartmentId = null,
    int? CourseId = null,
    int? CollegeId = null
) : IRequest<AdminReportsDto>;