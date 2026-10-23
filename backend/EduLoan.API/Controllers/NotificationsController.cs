using EduLoan.Application.Common;
using EduLoan.Application.Interfaces;
using EduLoan.Domain.Entities;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduLoan.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;

    public NotificationsController(AppDbContext db, ICurrentUserService current)
    { _db = db; _current = current; }

    private Guid UserId => _current.UserId ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false, [FromQuery] int take = 30, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == UserId);
        if (unreadOnly) query = query.Where(n => n.ReadAtUtc == null);
        var items = await query.OrderByDescending(n => n.CreatedAtUtc).Take(take)
            .Select(n => new { n.Id, n.Title, n.Message, n.Category, n.Link, n.CreatedAtUtc, n.ReadAtUtc, IsRead = n.ReadAtUtc != null })
            .ToListAsync(ct);
        var unreadCount = await _db.Notifications.CountAsync(n => n.UserId == UserId && n.ReadAtUtc == null, ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, unreadCount }));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId, ct);
        if (notification is null) return NotFound(ApiResponse<object>.FailureResponse("Notification not found."));
        notification.ReadAtUtc ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { notification.Id, notification.ReadAtUtc, IsRead = true }, "Notification marked as read."));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == UserId && n.ReadAtUtc == null).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var item in unread) item.ReadAtUtc = now;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { updatedCount = unread.Count }, "Notifications marked as read."));
    }
}
