using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.LoanApplications;

public record CreateLoanApplicationCommand(
    Guid EmployeeId,
    int CollegeId,
    int CourseId,
    string Specialization,
    int CourseDurationMonths,
    decimal TotalEducationFees,
    decimal RequestedAmount,
    int RequestedTenureMonths,
    string EducationPurpose
) : IRequest<LoanApplicationDetailDto>;

public class CreateLoanApplicationCommandValidator : AbstractValidator<CreateLoanApplicationCommand>
{
    public CreateLoanApplicationCommandValidator()
    {
        RuleFor(x => x.CollegeId).GreaterThan(0);
        RuleFor(x => x.CourseId).GreaterThan(0);
        RuleFor(x => x.Specialization).NotEmpty().MaximumLength(150);
        RuleFor(x => x.CourseDurationMonths).GreaterThan(0).LessThanOrEqualTo(120);
        RuleFor(x => x.TotalEducationFees).GreaterThan(0);
        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(x => x.TotalEducationFees)
            .WithMessage("Requested amount cannot exceed the total education fees.");
        RuleFor(x => x.RequestedTenureMonths).GreaterThan(0).LessThanOrEqualTo(84);
        RuleFor(x => x.EducationPurpose).NotEmpty().MaximumLength(500);
    }
}

public class CreateLoanApplicationCommandHandler : IRequestHandler<CreateLoanApplicationCommand, LoanApplicationDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateLoanApplicationCommandHandler(IAppDbContext db, IDateTimeProvider dateTimeProvider)
    {
        _db = db;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<LoanApplicationDetailDto> Handle(CreateLoanApplicationCommand request, CancellationToken ct)
    {
        var college = await _db.Colleges.FirstOrDefaultAsync(c => c.Id == request.CollegeId && c.IsActive, ct)
            ?? throw new NotFoundException(nameof(College), request.CollegeId);

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId && c.IsActive, ct)
            ?? throw new NotFoundException(nameof(Course), request.CourseId);

        var application = new LoanApplication
        {
            Id = Guid.NewGuid(),
            ApplicationNumber = GenerateApplicationNumber(_dateTimeProvider.Today.Year),
            EmployeeId = request.EmployeeId,
            CollegeId = college.Id,
            CourseId = course.Id,
            Specialization = request.Specialization,
            CourseDurationMonths = request.CourseDurationMonths,
            TotalEducationFees = request.TotalEducationFees,
            RequestedAmount = request.RequestedAmount,
            RequestedTenureMonths = request.RequestedTenureMonths,
            EducationPurpose = request.EducationPurpose,
            Status = ApplicationStatus.Draft,
        };

        _db.AddLoanApplication(application);

        var rowsAffected = await _db.SaveChangesAsync(ct);

        Console.WriteLine(
            $"Application saved: {application.Id}, " +
            $"Application Number: {application.ApplicationNumber}, " +
            $"Rows Affected: {rowsAffected}"
        );
        
        await _db.SaveChangesAsync(ct);

        application.College = college;
        application.Course = course;
        return application.ToDetailDto();
    }

    // Format: EDL-{year}-{6 random hex chars}. Collisions are astronomically unlikely
    // at this scale; a production system would retry-on-conflict against the unique index instead.
    internal static string GenerateApplicationNumber(int year)
        => $"EDL-{year}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
