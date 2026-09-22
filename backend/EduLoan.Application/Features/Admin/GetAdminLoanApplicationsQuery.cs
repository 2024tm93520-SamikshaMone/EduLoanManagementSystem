using MediatR;

namespace EduLoan.Application.Features.AdminApplications;

public record GetAdminLoanApplicationsQuery
    : IRequest<List<AdminLoanApplicationDto>>;