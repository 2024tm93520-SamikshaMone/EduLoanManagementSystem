using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.AdminDashboard;

public record GetAdminDashboardQuery
    : IRequest<AdminDashboardDto>;

public class GetAdminDashboardQueryHandler
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    private readonly IAppDbContext _db;

    public GetAdminDashboardQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardDto> Handle(
        GetAdminDashboardQuery request,
        CancellationToken ct)
    {
        // ----------------------------------------------------
        // EMPLOYEE SUMMARY
        // ----------------------------------------------------

        var totalEmployees = await _db.Users
            .CountAsync(
                u => u.Role.ToString() == "Employee" && u.IsActive,
                ct);


        // ----------------------------------------------------
        // LOAN APPLICATION DATA
        // ----------------------------------------------------

        var applications = await _db.LoanApplications
            .Select(a => new
            {
                a.Id,
                a.ApplicationNumber,
                a.EmployeeId,
                a.CollegeId,
                a.CourseId,
                a.RequestedAmount,
                Status = a.Status.ToString(),
                a.CreatedAt
            })
            .ToListAsync(ct);


        // ----------------------------------------------------
        // STATUS COUNTS
        // ----------------------------------------------------

        var statusCounts = applications
            .GroupBy(a => a.Status)
            .ToDictionary(
                g => g.Key,
                g => g.Count());


        var pendingApplications = applications.Count(a =>
            a.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
            a.Status.Equals("UnderReview", StringComparison.OrdinalIgnoreCase) ||
            a.Status.Equals("Under Review", StringComparison.OrdinalIgnoreCase));


        var approvedApplications = applications.Count(a =>
            a.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase));


        var pendingFinanceConfirmation = applications.Count(a =>
            a.Status.Equals("PendingFinance", StringComparison.OrdinalIgnoreCase) ||
            a.Status.Equals("Pending Finance", StringComparison.OrdinalIgnoreCase));


        // ----------------------------------------------------
        // STATUS OVERVIEW
        // ----------------------------------------------------

        var totalApplications = applications.Count;

        var statusOverview = statusCounts
            .OrderByDescending(x => x.Value)
            .Select(x => new AdminApplicationStatusDto(
                x.Key,
                x.Value,
                totalApplications == 0
                    ? 0
                    : (int)Math.Round(
                        x.Value * 100.0 / totalApplications)))
            .ToList();


        // ----------------------------------------------------
        // RECENT APPLICATIONS
        // ----------------------------------------------------

        var recentApplicationIds = applications
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToList();


        var employeeIds = recentApplicationIds
            .Select(a => a.EmployeeId)
            .Distinct()
            .ToList();

        var collegeIds = recentApplicationIds
            .Select(a => a.CollegeId)
            .Distinct()
            .ToList();

        var courseIds = recentApplicationIds
            .Select(a => a.CourseId)
            .Distinct()
            .ToList();


        var employees = await _db.Users
            .Where(u => employeeIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.EmployeeCode
            })
            .ToDictionaryAsync(
                x => x.Id,
                ct);


        var colleges = await _db.Colleges
            .Where(c => collegeIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name
            })
            .ToDictionaryAsync(
                x => x.Id,
                ct);


        var courses = await _db.Courses
            .Where(c => courseIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name
            })
            .ToDictionaryAsync(
                x => x.Id,
                ct);


        var recentApplications = recentApplicationIds
            .Select(a =>
            {
                employees.TryGetValue(
                    a.EmployeeId,
                    out var employee);

                colleges.TryGetValue(
                    a.CollegeId,
                    out var college);

                courses.TryGetValue(
                    a.CourseId,
                    out var course);

                return new AdminRecentApplicationDto(
                    a.Id,
                    a.ApplicationNumber,
                    employee?.FullName ?? "",
                    employee?.EmployeeCode ?? "",
                    college?.Name ?? "",
                    course?.Name ?? "",
                    a.RequestedAmount,
                    a.Status,
                    a.CreatedAt
                );
            })
            .ToList();


        // ----------------------------------------------------
        // FINAL RESPONSE
        // ----------------------------------------------------

        return new AdminDashboardDto(
            new AdminDashboardSummaryDto(
                totalEmployees,
                pendingApplications,
                approvedApplications,
                pendingFinanceConfirmation
            ),
            recentApplications,
            statusOverview
        );
    }
}