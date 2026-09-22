using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.AdminApplications;

public class GetAdminEmployeesQueryHandler
    : IRequestHandler<
        GetAdminEmployeesQuery,
        List<AdminEmployeeDto>>
{
    private readonly IAppDbContext _context;

    public GetAdminEmployeesQueryHandler(
        IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AdminEmployeeDto>> Handle(
        GetAdminEmployeesQuery request,
        CancellationToken ct)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(x =>
                x.Role == UserRole.Employee &&
                x.IsActive)
            .OrderBy(x => x.EmployeeCode)
            .Select(x => new AdminEmployeeDto(
                x.Id,
                x.EmployeeCode,
                x.FullName,
                x.Email,
                x.Department != null
                    ? x.Department.Name
                    : null,
                x.Designation,
                x.DateOfJoining,
                x.MonthlySalary,
                x.PhoneNumber,
                x.IsActive
            ))
            .ToListAsync(ct);
    }
}