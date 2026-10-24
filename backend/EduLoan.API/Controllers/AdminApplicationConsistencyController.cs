using System.Security.Claims;
using EduLoan.Application.Common;
using EduLoan.Application.Features.AdminApplications;
using EduLoan.Domain.Entities;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin,HR")]
public sealed class AdminApplicationConsistencyController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApplicationConsistencyChecker _checker;

    public AdminApplicationConsistencyController(AppDbContext db, ApplicationConsistencyChecker checker)
    { _db = db; _checker = checker; }

    private Guid ActorId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
        ? id : throw new UnauthorizedAccessException("Authenticated user id is missing.");

    [HttpGet("applications/{applicationId:guid}/consistency-checks")]
    public async Task<ActionResult<ApiResponse<ApplicationConsistencySummary>>> GetChecks(Guid applicationId, CancellationToken ct)
    {
        var target = await _db.LoanApplications.AsNoTracking().Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (target is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        var duplicates = await GetDuplicates(target, ct);
        var detected = _checker.Evaluate(target).ToList();
        if (duplicates.Count > 0) detected.Add(new("POTENTIAL_DUPLICATE", "Potential duplicate application", "High",
            $"Found {duplicates.Count} other non-cancelled, non-rejected application(s) for the same employee, college, and course. Review the records; this is not a fraud determination."));
        await PersistDetectedFlags(target.Id, detected, ct);
        return Ok(ApiResponse<ApplicationConsistencySummary>.SuccessResponse(new(target.Id, detected, duplicates,
            "These deterministic checks identify data inconsistencies for Admin/HR review only. They do not establish fraud and do not approve or reject an application.")));
    }

    [HttpGet("consistency-flags")]
    public async Task<IActionResult> ListFlags([FromQuery] string? status, [FromQuery] string? category,
        [FromQuery] string? severity, [FromQuery] Guid? applicationId, [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) return BadRequest(ApiResponse<object>.FailureResponse("Page must be greater than zero."));
        if (pageSize < 1 || pageSize > 100) return BadRequest(ApiResponse<object>.FailureResponse("Page size must be between 1 and 100."));
        var q = _db.ConsistencyFlags.AsNoTracking().Include(f => f.Application).AsQueryable();
        if (Enum.TryParse<ConsistencyFlagStatus>(status, true, out var parsedStatus)) q = q.Where(f => f.Status == parsedStatus);
        else if (!string.IsNullOrWhiteSpace(status)) return BadRequest(ApiResponse<object>.FailureResponse("Unsupported flag status."));
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(f => f.Category == category);
        if (!string.IsNullOrWhiteSpace(severity)) q = q.Where(f => f.Severity == severity);
        if (applicationId.HasValue) q = q.Where(f => f.ApplicationId == applicationId.Value);
        if (fromUtc.HasValue) q = q.Where(f => f.DetectedAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(f => f.DetectedAtUtc <= toUtc.Value);
        var totalCount = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(f => f.DetectedAtUtc).ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(f => new {
                f.Id, f.ApplicationId, ApplicationNumber = f.Application!.ApplicationNumber, f.RuleCode, f.Title,
                f.Category, f.Severity, f.Description, f.DetectedAtUtc, Status = f.Status.ToString(), f.ReviewedByUserId,
                f.ReviewedAtUtc, f.ResolutionNotes
            }).ToListAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new {
            items = rows, page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        }));
    }

    [HttpGet("consistency-flags/{id:guid}")]
    public async Task<IActionResult> GetFlag(Guid id, CancellationToken ct)
    {
        var flag = await _db.ConsistencyFlags.AsNoTracking().Include(f => f.Application)
            .Include(f => f.History).ThenInclude(h => h.Actor).FirstOrDefaultAsync(f => f.Id == id, ct);
        if (flag is null) return NotFound(ApiResponse<object>.FailureResponse("Consistency flag not found."));
        return Ok(ApiResponse<object>.SuccessResponse(new {
            flag.Id, flag.ApplicationId, flag.Application!.ApplicationNumber, flag.RuleCode, flag.Title,
            flag.Category, flag.Severity, flag.Description, flag.DetectedAtUtc, Status = flag.Status.ToString(),
            flag.ReviewedByUserId, flag.ReviewedAtUtc, flag.ResolutionNotes,
            History = flag.History.OrderBy(h => h.CreatedAtUtc).Select(h => new { h.Id, h.ActorUserId, ActorName = h.Actor!.FullName, h.Action, h.Notes, h.CreatedAtUtc })
        }));
    }

    [HttpPost("applications/{applicationId:guid}/consistency-flags/evaluate")]
    public async Task<IActionResult> Evaluate(Guid applicationId, CancellationToken ct)
    {
        var app = await _db.LoanApplications.Include(a => a.Documents).FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (app is null) return NotFound(ApiResponse<object>.FailureResponse("Application not found."));
        var duplicates = await GetDuplicates(app, ct);
        var detected = _checker.Evaluate(app).ToList();
        if (duplicates.Count > 0) detected.Add(new("POTENTIAL_DUPLICATE", "Potential duplicate application", "High",
            $"Found {duplicates.Count} other non-cancelled, non-rejected application(s) for the same employee, college, and course. Review the records; this is not a fraud determination."));
        var created = await PersistDetectedFlags(app.Id, detected, ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { applicationId, detectedCount = detected.Count, createdCount = created, flags = detected }));
    }

    [HttpPatch("consistency-flags/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateConsistencyFlagStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ConsistencyFlagStatus>(request.Status, true, out var next))
            return BadRequest(ApiResponse<object>.FailureResponse("Unsupported status."));
        var notes = request.Notes?.Trim();
        if (notes?.Length > 1000) return BadRequest(ApiResponse<object>.FailureResponse("Notes cannot exceed 1000 characters."));
        if (ConsistencyFlagTransitionPolicy.RequiresNotes(next) && string.IsNullOrWhiteSpace(notes))
            return BadRequest(ApiResponse<object>.FailureResponse("Resolution or dismissal notes are required."));

        var flag = await _db.ConsistencyFlags.Include(f => f.Application).FirstOrDefaultAsync(f => f.Id == id, ct);
        if (flag is null) return NotFound(ApiResponse<object>.FailureResponse("Consistency flag not found."));
        var allowed = ConsistencyFlagTransitionPolicy.CanTransition(flag.Status, next);
        if (!allowed || next == flag.Status) return Conflict(ApiResponse<object>.FailureResponse($"Invalid flag status transition: {flag.Status} → {next}."));
        var actorId = ActorId;
        var actor = await _db.Users.FirstOrDefaultAsync(u => u.Id == actorId && u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.HR), ct);
        if (actor is null) return Forbid();
        var now = DateTime.UtcNow;
        flag.Status = next;
        flag.ReviewedByUserId = actorId;
        flag.ReviewedAtUtc = now;
        flag.ResolutionNotes = next is ConsistencyFlagStatus.Resolved or ConsistencyFlagStatus.Dismissed ? notes : null;
        _db.ConsistencyFlagHistories.Add(new ConsistencyFlagHistory { Id = Guid.NewGuid(), ConsistencyFlagId = flag.Id,
            ActorUserId = actorId, Action = next.ToString(), Notes = notes, CreatedAtUtc = now });
        var recipients = await _db.Users.Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.HR) && u.Id != actorId)
            .Select(u => u.Id).ToListAsync(ct);
        foreach (var recipient in recipients)
            _db.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = recipient, Title = "Consistency flag updated",
                Message = $"Flag {flag.RuleCode} for application {flag.Application!.ApplicationNumber} changed to {next}.",
                Category = "ConsistencyFlag", Link = $"/admin/applications/{flag.ApplicationId}", CreatedAtUtc = now });
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(ApiResponse<object>.FailureResponse("The flag was updated by another reviewer. Refresh and retry.")); }
        catch (DbUpdateException) { return Conflict(ApiResponse<object>.FailureResponse("The flag changed concurrently or the requested transition conflicts with persisted state. Refresh and retry.")); }
        return Ok(ApiResponse<object>.SuccessResponse(new { flag.Id, Status = flag.Status.ToString(), flag.ReviewedByUserId, flag.ReviewedAtUtc, flag.ResolutionNotes }));
    }

    private async Task<int> PersistDetectedFlags(Guid applicationId, IReadOnlyList<ApplicationConsistencyFlag> detected, CancellationToken ct)
    {
        var active = await _db.ConsistencyFlags.Where(f => f.ApplicationId == applicationId &&
            (f.Status == ConsistencyFlagStatus.Open || f.Status == ConsistencyFlagStatus.UnderInvestigation)).ToListAsync(ct);
        var byCode = active.ToDictionary(f => f.RuleCode, StringComparer.OrdinalIgnoreCase);
        var created = 0;
        var actorId = ActorId;
        var now = DateTime.UtcNow;
        foreach (var item in detected)
        {
            if (byCode.ContainsKey(item.Code)) continue;
            var flag = new ConsistencyFlag { Id = Guid.NewGuid(), ApplicationId = applicationId, RuleCode = item.Code,
                Title = item.Title, Category = item.Code.StartsWith("POTENTIAL_DUPLICATE", StringComparison.Ordinal) ? "Duplicate" : "Consistency",
                Severity = item.Severity, Description = item.Description, DetectedAtUtc = now, Status = ConsistencyFlagStatus.Open };
            _db.ConsistencyFlags.Add(flag);
            _db.ConsistencyFlagHistories.Add(new ConsistencyFlagHistory { Id = Guid.NewGuid(), ConsistencyFlagId = flag.Id,
                ActorUserId = actorId, Action = "Detected", Notes = "Created by deterministic consistency evaluation.", CreatedAtUtc = now });
            created++;
        }
        if (created > 0)
        {
            var recipients = await _db.Users.Where(u => u.IsActive && (u.Role == UserRole.Admin || u.Role == UserRole.HR))
                .Select(u => u.Id).ToListAsync(ct);
            foreach (var recipient in recipients)
                _db.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = recipient, Title = "Consistency flags detected",
                    Message = $"{created} new consistency flag(s) detected for application {applicationId}.", Category = "ConsistencyFlag",
                    Link = $"/admin/applications/{applicationId}", CreatedAtUtc = now });
            try { await _db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (ex.GetBaseException() is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
            {
                // The filtered unique index is the concurrency guard. A competing evaluator may have inserted the same flags.
                _db.ChangeTracker.Clear();
                return 0;
            }
        }
        return created;
    }

    private async Task<List<DuplicateApplicationReference>> GetDuplicates(LoanApplication target, CancellationToken ct) =>
        await _db.LoanApplications.AsNoTracking().Where(a => a.Id != target.Id && a.EmployeeId == target.EmployeeId &&
            a.CollegeId == target.CollegeId && a.CourseId == target.CourseId && a.Status != ApplicationStatus.Draft &&
            a.Status != ApplicationStatus.Cancelled && a.Status != ApplicationStatus.Rejected)
            .OrderByDescending(a => a.CreatedAt).Select(a => new DuplicateApplicationReference(a.Id, a.ApplicationNumber, a.Status.ToString())).ToListAsync(ct);
}

public sealed record UpdateConsistencyFlagStatusRequest(string Status, string? Notes);
