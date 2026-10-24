using EduLoan.Application.Common;
using EduLoan.API.Services;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/audit") ]
[Authorize(Roles = "Admin,HR")]
public sealed class AuditController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] Guid? actorUserId, [FromQuery] string? action, [FromQuery] string? entityType, [FromQuery] string? entityId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date) return BadRequest(ApiResponse<object>.FailureResponse("Start date must be on or before end date."));
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (startDate.HasValue) q = q.Where(x => x.CreatedAtUtc >= startDate.Value.Date);
        if (endDate.HasValue) q = q.Where(x => x.CreatedAtUtc < endDate.Value.Date.AddDays(1));
        if (actorUserId.HasValue) q = q.Where(x => x.ActorUserId == actorUserId);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => x.Action.Contains(action));
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(x => x.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) q = q.Where(x => x.EntityId == entityId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new { x.Id, x.ActorUserId, x.ActorRole, x.Action, x.EntityType, x.EntityId, x.Outcome, x.CreatedAtUtc, x.CorrelationId, x.Metadata }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) }));
    }
}
