using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.LoanApplications;

public record UpdateLoanApplicationCommand(
    Guid ApplicationId,
    Guid RequestingUserId,
    int CollegeId,
    int CourseId,
    string Specialization,
    int CourseDurationMonths,
    decimal TotalEducationFees,
    decimal RequestedAmount,
    int RequestedTenureMonths,
    string EducationPurpose
) : IRequest<LoanApplicationDetailDto>;

public class UpdateLoanApplicationCommandValidator : AbstractValidator<UpdateLoanApplicationCommand>
{
    public UpdateLoanApplicationCommandValidator()
    {
        RuleFor(x => x.CollegeId).GreaterThan(0);
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.Specialization).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CourseDurationMonths).GreaterThan(0).LessThanOrEqualTo(120);
        RuleFor(x => x.TotalEducationFees).GreaterThan(0);
        RuleFor(x => x.RequestedAmount).GreaterThan(0).LessThanOrEqualTo(x => x.TotalEducationFees)
            .WithMessage("Requested amount cannot exceed the total education fees.");
        RuleFor(x => x.RequestedTenureMonths).GreaterThan(0).LessThanOrEqualTo(84);
        RuleFor(x => x.EducationPurpose).NotEmpty().MaximumLength(500);
    }
}

public class UpdateLoanApplicationCommandHandler : IRequestHandler<UpdateLoanApplicationCommand, LoanApplicationDetailDto>
{
    private readonly IAppDbContext _db;
    public UpdateLoanApplicationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<LoanApplicationDetailDto> Handle(UpdateLoanApplicationCommand request, CancellationToken ct)
    {
        var application = await _db.LoanApplications
            .Include(a => a.College).Include(a => a.Course).Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(LoanApplication), request.ApplicationId);

        if (application.EmployeeId != request.RequestingUserId)
            throw new ForbiddenException("You can only edit your own application.");

        if (application.Status != ApplicationStatus.Draft)
            throw new InUseException("Only draft applications can be edited.");

        var college = await _db.Colleges.FirstOrDefaultAsync(c => c.Id == request.CollegeId && c.IsActive, ct)
            ?? throw new NotFoundException(nameof(College), request.CollegeId);
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId && c.IsActive, ct)
            ?? throw new NotFoundException(nameof(Course), request.CourseId);

        application.CollegeId = college.Id;
        application.CourseId = course.Id;
        application.Specialization = request.Specialization;
        application.CourseDurationMonths = request.CourseDurationMonths;
        application.TotalEducationFees = request.TotalEducationFees;
        application.RequestedAmount = request.RequestedAmount;
        application.RequestedTenureMonths = request.RequestedTenureMonths;
        application.EducationPurpose = request.EducationPurpose;
        application.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        application.College = college;
        application.Course = course;
        return application.ToDetailDto();
    }
}
