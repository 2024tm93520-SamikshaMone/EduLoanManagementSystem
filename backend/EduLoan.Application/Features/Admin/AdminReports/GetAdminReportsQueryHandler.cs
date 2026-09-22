using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.AdminReports;

public class GetAdminReportsQueryHandler
    : IRequestHandler<GetAdminReportsQuery, AdminReportsDto>
{
    private readonly IAppDbContext _context;

    public GetAdminReportsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminReportsDto> Handle(
        GetAdminReportsQuery request,
        CancellationToken ct)
    {
        var applicationsQuery = _context.LoanApplications
            .AsNoTracking()
            .AsQueryable();

        // ---------------------------------------------------------
        // Year Filter
        // ---------------------------------------------------------

        if (request.Year.HasValue)
        {
            var startDate = new DateTime(
                request.Year.Value,
                1,
                1);

            var endDate = startDate.AddYears(1);

            applicationsQuery = applicationsQuery.Where(x =>
                x.CreatedAt >= startDate &&
                x.CreatedAt < endDate);
        }

        // ---------------------------------------------------------
        // Department Filter
        // ---------------------------------------------------------

        if (request.DepartmentId.HasValue)
        {
            applicationsQuery = applicationsQuery.Where(x =>
                x.Employee.DepartmentId == request.DepartmentId.Value);
        }

        // ---------------------------------------------------------
        // Course Filter
        // ---------------------------------------------------------

        if (request.CourseId.HasValue)
        {
            applicationsQuery = applicationsQuery.Where(x =>
                x.CourseId == request.CourseId.Value);
        }

        // ---------------------------------------------------------
        // College Filter
        // ---------------------------------------------------------

        if (request.CollegeId.HasValue)
        {
            applicationsQuery = applicationsQuery.Where(x =>
                x.CollegeId == request.CollegeId.Value);
        }

        // ---------------------------------------------------------
        // Get actual application data from SQL
        // ---------------------------------------------------------

        var applications = await applicationsQuery
            .Select(x => new
            {
                x.Status,
                x.RequestedAmount,

                DepartmentId = x.Employee.DepartmentId,

                DepartmentName = x.Employee.Department != null
                    ? x.Employee.Department.Name
                    : "Unknown",

                x.CourseId,

                CourseName = x.Course != null
                    ? x.Course.Name
                    : "Unknown",

                x.CollegeId,

                CollegeName = x.College != null
                    ? x.College.Name
                    : "Unknown"
            })
            .ToListAsync(ct);

        // ---------------------------------------------------------
        // Summary
        // ---------------------------------------------------------

        var totalApplications = applications.Count;

        var approvedApplications = applications.Count(x =>
    x.Status.ToString() == "Approved");

        var rejectedApplications = applications.Count(x =>
            x.Status.ToString() == "Rejected");

        var submittedApplications = applications.Count(x =>
            x.Status.ToString() == "Submitted");

        var totalRequestedAmount = applications.Sum(x =>
            x.RequestedAmount);

        var totalEmployees = await _context.Users
            .AsNoTracking()
            .CountAsync(
                x =>
                    x.Role == UserRole.Employee &&
                    x.IsActive,
                ct);

        // ---------------------------------------------------------
        // Status Overview
        // ---------------------------------------------------------

        var statusOverview = applications
            .GroupBy(x => x.Status)
            .Select(g => new AdminReportStatusDto(
                g.Key.ToString(),
                g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        // ---------------------------------------------------------
        // Department Overview
        // ---------------------------------------------------------

        var departmentOverview = applications
            .GroupBy(x => new
            {
                x.DepartmentId,
                x.DepartmentName
            })
            .Select(g => new AdminReportDepartmentDto(
                g.Key.DepartmentId ?? 0,
                g.Key.DepartmentName,
                g.Count(),
                g.Sum(x => x.RequestedAmount)))
            .OrderByDescending(x => x.ApplicationCount)
            .ToList();

        // ---------------------------------------------------------
        // Course Overview
        // ---------------------------------------------------------

        var courseOverview = applications
            .GroupBy(x => new
            {
                x.CourseId,
                x.CourseName
            })
            .Select(g => new AdminReportCourseDto(
                g.Key.CourseId,
                g.Key.CourseName,
                g.Count(),
                g.Sum(x => x.RequestedAmount)))
            .OrderByDescending(x => x.ApplicationCount)
            .ToList();

        // ---------------------------------------------------------
        // College Overview
        // ---------------------------------------------------------

        var collegeOverview = applications
            .GroupBy(x => new
            {
                x.CollegeId,
                x.CollegeName
            })
            .Select(g => new AdminReportCollegeDto(
                g.Key.CollegeId,
                g.Key.CollegeName,
                g.Count(),
                g.Sum(x => x.RequestedAmount)))
            .OrderByDescending(x => x.ApplicationCount)
            .ToList();

        // ---------------------------------------------------------
        // Final Response
        // ---------------------------------------------------------

        return new AdminReportsDto(
            new AdminReportSummaryDto(
                totalApplications,
                approvedApplications,
                rejectedApplications,
                submittedApplications,
                totalEmployees,
                totalRequestedAmount
            ),
            statusOverview,
            departmentOverview,
            courseOverview,
            collegeOverview
        );
    }
}