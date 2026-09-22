using MediatR;
using Microsoft.EntityFrameworkCore;
using EduLoan.Application.Interfaces;

namespace EduLoan.Application.Features.AdminApplications;

public class GetAdminLoanApplicationByIdQueryHandler
    : IRequestHandler<
        GetAdminLoanApplicationByIdQuery,
        AdminLoanApplicationDetailDto>
{
    private readonly IAppDbContext _context;

    public GetAdminLoanApplicationByIdQueryHandler(
        IAppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminLoanApplicationDetailDto> Handle(
        GetAdminLoanApplicationByIdQuery request,
        CancellationToken ct)
    {
        var application = await _context.LoanApplications
            .AsNoTracking()
            .Include(x => x.Employee)
                .ThenInclude(x => x.Department)
            .Include(x => x.College)
            .Include(x => x.Course)
            .FirstOrDefaultAsync(
                x => x.Id == request.Id,
                ct);

        if (application == null)
        {
            throw new KeyNotFoundException(
                $"Loan application '{request.Id}' was not found.");
        }

        return new AdminLoanApplicationDetailDto(
            application.Id,
            application.ApplicationNumber,
            application.Status.ToString(),

            application.Employee.Id,
            application.Employee.EmployeeCode,
            application.Employee.FullName,
            application.Employee.Email,
            application.Employee.Department?.Name ?? "",
            application.Employee.Designation ?? "",
            application.Employee.DateOfJoining,
            application.Employee.MonthlySalary,
            application.Employee.PhoneNumber,

            application.College.Name,
            application.College.City,
            application.Course.Name,
            application.Course.Level,
            application.Specialization,
            application.CourseDurationMonths,

            application.TotalEducationFees,
            application.RequestedAmount,
            application.RequestedTenureMonths,
            application.EducationPurpose,

            application.CreatedAt,
            application.SubmittedAt
        );
    }
}