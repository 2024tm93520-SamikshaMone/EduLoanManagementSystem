using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace EduLoan.Application.Features.Employees;

// Deliberately narrow: employees can only update their own phone number here.
// Salary, tenure, department, designation are HR-controlled and have no employee-facing
// update path in this feature — a separate HR module will own those.
public record UpdateEmployeeProfileCommand(Guid UserId, string PhoneNumber) : IRequest<EmployeeProfileDto>;

public class UpdateEmployeeProfileCommandValidator : AbstractValidator<UpdateEmployeeProfileCommand>
{
    public UpdateEmployeeProfileCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^[0-9]{10}$").WithMessage("Phone number must be exactly 10 digits.");
    }
}

public class UpdateEmployeeProfileCommandHandler : IRequestHandler<UpdateEmployeeProfileCommand, EmployeeProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateEmployeeProfileCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<EmployeeProfileDto> Handle(UpdateEmployeeProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), request.UserId);

        user.PhoneNumber = request.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var tenureMonths = GetEmployeeProfileQueryHandler.CalculateTenureMonths(user.DateOfJoining, _dateTimeProvider.Today);

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
}
