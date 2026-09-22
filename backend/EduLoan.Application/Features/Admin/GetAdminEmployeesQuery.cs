using MediatR;

namespace EduLoan.Application.Features.AdminApplications;

public record GetAdminEmployeesQuery
    : IRequest<List<AdminEmployeeDto>>;