using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.MasterData.Courses;

public record CourseDto(int Id, string Name, string Level, int StandardDurationMonths, bool IsActive);

internal static class CourseLevels
{
    public static readonly string[] Valid = { "UG", "PG", "Diploma", "Certification" };
}

public record GetCoursesQuery : IRequest<List<CourseDto>>;

public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, List<CourseDto>>
{
    private readonly IAppDbContext _db;
    public GetCoursesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<CourseDto>> Handle(GetCoursesQuery request, CancellationToken ct)
        => await _db.Courses
            .OrderBy(c => c.Name)
            .Select(c => new CourseDto(c.Id, c.Name, c.Level, c.StandardDurationMonths, c.IsActive))
            .ToListAsync(ct);
}

public record CreateCourseCommand(string Name, string Level, int StandardDurationMonths) : IRequest<CourseDto>;

public class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Level).NotEmpty().Must(l => CourseLevels.Valid.Contains(l))
            .WithMessage($"Level must be one of: {string.Join(", ", CourseLevels.Valid)}.");
        RuleFor(x => x.StandardDurationMonths).GreaterThan(0).LessThanOrEqualTo(120);
    }
}

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, CourseDto>
{
    private readonly IAppDbContext _db;
    public CreateCourseCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CourseDto> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        var nameExists = await _db.Courses.AnyAsync(c => c.Name == request.Name, ct);
        if (nameExists) throw new DuplicateNameException("course", request.Name);

        var course = new Course
        {
            Name = request.Name,
            Level = request.Level,
            StandardDurationMonths = request.StandardDurationMonths,
            IsActive = true,
        };
        _db.AddCourse(course);
        await _db.SaveChangesAsync(ct);

        return new CourseDto(course.Id, course.Name, course.Level, course.StandardDurationMonths, course.IsActive);
    }
}

public record UpdateCourseCommand(int Id, string Name, string Level, int StandardDurationMonths, bool IsActive) : IRequest<CourseDto>;

public class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Level).NotEmpty().Must(l => CourseLevels.Valid.Contains(l))
            .WithMessage($"Level must be one of: {string.Join(", ", CourseLevels.Valid)}.");
        RuleFor(x => x.StandardDurationMonths).GreaterThan(0).LessThanOrEqualTo(120);
    }
}

public class UpdateCourseCommandHandler : IRequestHandler<UpdateCourseCommand, CourseDto>
{
    private readonly IAppDbContext _db;
    public UpdateCourseCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CourseDto> Handle(UpdateCourseCommand request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Course), request.Id);

        var nameTakenByOther = await _db.Courses
            .AnyAsync(c => c.Name == request.Name && c.Id != request.Id, ct);
        if (nameTakenByOther) throw new DuplicateNameException("course", request.Name);

        course.Name = request.Name;
        course.Level = request.Level;
        course.StandardDurationMonths = request.StandardDurationMonths;
        course.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        return new CourseDto(course.Id, course.Name, course.Level, course.StandardDurationMonths, course.IsActive);
    }
}

// NOTE: no in-use check yet — will be added once Module 4 (Loan Application)
// introduces a real foreign-key reference from LoanApplication -> Course.
public record DeleteCourseCommand(int Id) : IRequest;

public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand>
{
    private readonly IAppDbContext _db;
    public DeleteCourseCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCourseCommand request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Course), request.Id);

        _db.RemoveCourse(course);
        await _db.SaveChangesAsync(ct);
    }
}
