using EduLoan.API.Services;
using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/dashboards")]
[Authorize]
public sealed class DashboardsController : ControllerBase
{
    private readonly AppDbContext _db; private readonly ICurrentUserService _current;
    public DashboardsController(AppDbContext db, ICurrentUserService current) { _db = db; _current = current; }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var userId = _current.UserId; if (userId is null) return Unauthorized(ApiResponse<object>.FailureResponse("Authenticated user identity is missing."));
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        if (role == "Employee")
        {
            var mine = _db.LoanApplications.AsNoTracking().Where(a => a.EmployeeId == userId.Value);
            var total = await mine.CountAsync(ct);
            var statuses = await mine.GroupBy(a => a.Status).Select(g => new { status = g.Key.ToString(), count = g.Count() }).OrderBy(x => x.status).ToListAsync(ct);
            var recent = await mine.OrderByDescending(a => a.CreatedAt).Take(8).Select(a => new { a.Id, a.ApplicationNumber, status = a.Status.ToString(), a.RequestedAmount, createdAt = a.CreatedAt, a.UpdatedAt }).ToListAsync(ct);
            var unread = await _db.Notifications.CountAsync(n => n.UserId == userId.Value && n.ReadAtUtc == null, ct);
            var info = await mine.CountAsync(a => a.Status == ApplicationStatus.InfoRequested, ct);
            return Ok(ApiResponse<object>.SuccessResponse(new { role, summary = new { totalApplications = total, requiringAction = info, unreadNotifications = unread }, statusOverview = statuses, recentApplications = recent }));
        }
        if (role == "Admin" || role == "HR")
        {
            var apps = _db.LoanApplications.AsNoTracking();
            var total = await apps.CountAsync(ct);
            var statuses = await apps.GroupBy(a => a.Status).Select(g => new { status = g.Key.ToString(), count = g.Count() }).OrderBy(x => x.status).ToListAsync(ct);
            var recent = await apps.OrderByDescending(a => a.CreatedAt).Take(10).Select(a => new { a.Id, a.ApplicationNumber, status = a.Status.ToString(), a.RequestedAmount, createdAt = a.CreatedAt }).ToListAsync(ct);
            var openFlags = await _db.ConsistencyFlags.CountAsync(f => f.Status == ConsistencyFlagStatus.Open || f.Status == ConsistencyFlagStatus.UnderInvestigation, ct);
            var handoffs = role == "Admin" || role == "HR" ? await _db.FinanceHandoffs.CountAsync(f => f.Status != "Processing Confirmed", ct) : 0;
            return Ok(ApiResponse<object>.SuccessResponse(new { role, summary = new { totalApplications = total, pendingReview = await apps.CountAsync(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.UnderReview, ct), requiringInformation = await apps.CountAsync(a => a.Status == ApplicationStatus.InfoRequested, ct), openConsistencyFlags = openFlags, pendingFinanceItems = handoffs }, statusOverview = statuses, recentApplications = recent }));
        }
        if (role == "Finance")
        {
            var handoffs = _db.FinanceHandoffs.AsNoTracking();
            var statuses = await handoffs.GroupBy(f => f.Status).Select(g => new { status = g.Key, count = g.Count() }).OrderBy(x => x.status).ToListAsync(ct);
            var recent = await handoffs.OrderByDescending(f => f.UpdatedAtUtc).Take(10).Select(f => new { f.ApplicationId, applicationNumber = f.Application!.ApplicationNumber, f.Status, f.UpdatedAtUtc, f.ConfirmationReference }).ToListAsync(ct);
            return Ok(ApiResponse<object>.SuccessResponse(new { role, summary = new { totalHandoffs = await handoffs.CountAsync(ct), pendingFinance = await handoffs.CountAsync(f => f.Status == "Pending Finance Review", ct), awaitingConfirmation = await handoffs.CountAsync(f => f.Status == "Awaiting Bank Confirmation", ct), processingConfirmed = await handoffs.CountAsync(f => f.Status == "Processing Confirmed", ct) }, statusOverview = statuses, recentApplications = recent }));
        }
        return Forbid();
    }
}
