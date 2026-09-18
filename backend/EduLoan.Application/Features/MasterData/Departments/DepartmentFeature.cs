using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.MasterData.Departments;

public record DepartmentDto(int Id, string Name, bool IsActive);

// ---- Get all ----
public record GetDepartmentsQuery : IRequest<List<DepartmentDto>>;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, List<DepartmentDto>>
{
    private readonly IAppDbContext _db;
    public GetDepartmentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken ct)
        => await _db.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(d.Id, d.Name, d.IsActive))
            .ToListAsync(ct);
}

// ---- Create ----
public record CreateDepartmentCommand(string Name) : IRequest<DepartmentDto>;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IAppDbContext _db;
    public CreateDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var nameExists = await _db.Departments.AnyAsync(d => d.Name == request.Name, ct);
        if (nameExists) throw new DuplicateNameException("department", request.Name);

        var department = new Department { Name = request.Name, IsActive = true };
        _db.AddDepartment(department);
        await _db.SaveChangesAsync(ct);

        return new DepartmentDto(department.Id, department.Name, department.IsActive);
    }
}

// ---- Update ----
public record UpdateDepartmentCommand(int Id, string Name, bool IsActive) : IRequest<DepartmentDto>;

public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    private readonly IAppDbContext _db;
    public UpdateDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        var nameTakenByOther = await _db.Departments
            .AnyAsync(d => d.Name == request.Name && d.Id != request.Id, ct);
        if (nameTakenByOther) throw new DuplicateNameException("department", request.Name);

        department.Name = request.Name;
        department.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);

        return new DepartmentDto(department.Id, department.Name, department.IsActive);
    }
}

// ---- Delete ----
public record DeleteDepartmentCommand(int Id) : IRequest;

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand>
{
    private readonly IAppDbContext _db;
    public DeleteDepartmentCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteDepartmentCommand request, CancellationToken ct)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Department), request.Id);

        var inUse = await _db.Users.AnyAsync(u => u.DepartmentId == request.Id, ct);
        if (inUse)
            throw new InUseException("This department has employees assigned to it and cannot be deleted.");

        _db.RemoveDepartment(department);
        await _db.SaveChangesAsync(ct);
    }
}
