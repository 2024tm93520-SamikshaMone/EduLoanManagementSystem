using MediatR;

namespace EduLoan.Application.Features.AdminApplications;

public record GetAdminLoanApplicationByIdQuery(Guid Id)
    : IRequest<AdminLoanApplicationDetailDto>;