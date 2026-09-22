using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.AdminApplications;

public class GetAdminLoanApplicationsQueryHandler
    : IRequestHandler<
        GetAdminLoanApplicationsQuery,
        List<AdminLoanApplicationDto>>
{
    private readonly IAppDbContext _context;

    public GetAdminLoanApplicationsQueryHandler(
        IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdminLoanApplicationDto>> Handle(
        GetAdminLoanApplicationsQuery request,
        CancellationToken ct)
    {
        return await _context.LoanApplications
            .AsNoTracking()
            .Include(x => x.Employee)
                .ThenInclude(x => x.Department)
            .Include(x => x.College)
            .Include(x => x.Course)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminLoanApplicationDto(
                x.Id,
                x.ApplicationNumber,
                x.Employee.EmployeeCode,
                x.Employee.FullName,
                x.Employee.Department != null
                    ? x.Employee.Department.Name
                    : "",
                x.College.Name,
                x.Course.Name,
                x.RequestedAmount,
                x.SubmittedAt,
                x.Status.ToString()
            ))
            .ToListAsync(ct);
    }
}