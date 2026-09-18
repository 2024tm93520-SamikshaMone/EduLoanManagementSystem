using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using MediatR;

namespace EduLoan.Application.Features.Employees;

public record GetEmployeeProfileQuery(Guid UserId) : IRequest<EmployeeProfileDto>;

public class GetEmployeeProfileQueryHandler : IRequestHandler<GetEmployeeProfileQuery, EmployeeProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetEmployeeProfileQueryHandler(IUserRepository userRepository, IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EmployeeProfileDto> Handle(GetEmployeeProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        var tenureMonths = CalculateTenureMonths(user.DateOfJoining, _dateTimeProvider.Today);

        return new EmployeeProfileDto(
            user.Id,
            user.EmployeeCode,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.Department?.Name,
            user.Designation,
            user.DateOfJoining,
            tenureMonths,
            user.MonthlySalary,
            user.PhoneNumber
        );
    }

    public static int CalculateTenureMonths(DateOnly joiningDate, DateOnly today)
    {
        if (joiningDate > today) return 0;

        int months = (today.Year - joiningDate.Year) * 12 + (today.Month - joiningDate.Month);
        if (today.Day < joiningDate.Day) months--;

        return Math.Max(months, 0);
    }
}
