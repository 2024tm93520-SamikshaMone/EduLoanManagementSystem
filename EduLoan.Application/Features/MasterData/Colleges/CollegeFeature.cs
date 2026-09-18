using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.MasterData.Colleges;

public record CollegeDto(int Id, string Name, string City, bool IsActive);

public record GetCollegesQuery : IRequest<List<CollegeDto>>;

public class GetCollegesQueryHandler : IRequestHandler<GetCollegesQuery, List<CollegeDto>>
{
    private readonly IAppDbContext _db;
    public GetCollegesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<CollegeDto>> Handle(GetCollegesQuery request, CancellationToken ct)
        => await _db.Colleges
            .OrderBy(c => c.Name)
            .Select(c => new CollegeDto(c.Id, c.Name, c.City, c.IsActive))
            .ToListAsync(ct);
}

public record CreateCollegeCommand(string Name, string City) : IRequest<CollegeDto>;

public class CreateCollegeCommandValidator : AbstractValidator<CreateCollegeCommand>
{
    public CreateCollegeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
    }
}

public class CreateCollegeCommandHandler : IRequestHandler<CreateCollegeCommand, CollegeDto>
{
    private readonly IAppDbContext _db;
    public CreateCollegeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CollegeDto> Handle(CreateCollegeCommand request, CancellationToken ct)
    {
        var nameExists = await _db.Colleges.AnyAsync(c => c.Name == request.Name, ct);
        if (nameExists) throw new DuplicateNameException("college", request.Name);

        var college = new College { Name = request.Name, City = request.City, IsActive = true };
        _db.AddCollege(college);
        await _db.SaveChangesAsync(ct);

        return new CollegeDto(college.Id, college.Name, college.City, college.IsActive);
    }
}

public record UpdateCollegeCommand(int Id, string Name, string City, bool IsActive) : IRequest<CollegeDto>;

public class UpdateCollegeCommandValidator : AbstractValidator<UpdateCollegeCommand>
{
    public UpdateCollegeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
    }
}

public class UpdateCollegeCommandHandler : IRequestHandler<UpdateCollegeCommand, CollegeDto>
{
    private readonly IAppDbContext _db;
    public UpdateCollegeCommandHandler(IAppDbContext db) => _db = db;

    public async Task<CollegeDto> Handle(UpdateCollegeCommand request, CancellationToken ct)
    {
        var college = await _db.Colleges.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(College), request.Id);

        var nameTakenByOther = await _db.Colleges
            .AnyAsync(c => c.Name == request.Name && c.Id != request.Id, ct);
        if (nameTakenByOther) throw new DuplicateNameException("college", request.Name);

        college.Name = request.Name;
        college.City = request.City;
        college.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        return new CollegeDto(college.Id, college.Name, college.City, college.IsActive);
    }
}

// NOTE: no in-use check yet — will be added once the Loan Application feature (Module 4)
// introduces a real foreign-key reference from LoanApplication -> College.
public record DeleteCollegeCommand(int Id) : IRequest;

public class DeleteCollegeCommandHandler : IRequestHandler<DeleteCollegeCommand>
{
    private readonly IAppDbContext _db;
    public DeleteCollegeCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCollegeCommand request, CancellationToken ct)
    {
        var college = await _db.Colleges.FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(College), request.Id);

        _db.RemoveCollege(college);
        await _db.SaveChangesAsync(ct);
    }
}
