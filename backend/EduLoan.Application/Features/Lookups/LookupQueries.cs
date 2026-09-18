using EduLoan.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.Application.Features.Lookups;

public record LookupDto(int Id, string Name);

public record GetActiveCollegesLookupQuery : IRequest<List<LookupDto>>;

public class GetActiveCollegesLookupQueryHandler : IRequestHandler<GetActiveCollegesLookupQuery, List<LookupDto>>
{
    private readonly IAppDbContext _db;
    public GetActiveCollegesLookupQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<LookupDto>> Handle(GetActiveCollegesLookupQuery request, CancellationToken ct)
        => await _db.Colleges
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto(c.Id, c.Name))
            .ToListAsync(ct);
}

public record GetActiveCoursesLookupQuery : IRequest<List<LookupDto>>;

public class GetActiveCoursesLookupQueryHandler : IRequestHandler<GetActiveCoursesLookupQuery, List<LookupDto>>
{
    private readonly IAppDbContext _db;
    public GetActiveCoursesLookupQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<LookupDto>> Handle(GetActiveCoursesLookupQuery request, CancellationToken ct)
        => await _db.Courses
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new LookupDto(c.Id, c.Name))
            .ToListAsync(ct);
}
