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
    public NotificationsController(AppDbContext db, ICurrentUserService current) { _db = db; _current = current; }

    private bool TryGetUserId(out Guid userId)
    {
        userId = _current.UserId.GetValueOrDefault();
        return userId != Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] bool unreadOnly = false, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30, [FromQuery] int? take = null, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse<object>.FailureResponse("Authenticated user identity is missing."));
        // Keep legacy take query compatibility while supporting page-based pagination.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(take ?? pageSize, 1, 100);
        var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => n.ReadAtUtc == null);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new { n.Id, n.Title, n.Message, n.Category, n.Link, n.CreatedAtUtc, n.ReadAtUtc, IsRead = n.ReadAtUtc != null })
            .ToListAsync(ct);
        var unreadCount = await _db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAtUtc == null, ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { items, unreadCount, page, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse<object>.FailureResponse("Authenticated user identity is missing."));
        var count = await _db.Notifications.CountAsync(n => n.UserId == userId && n.ReadAtUtc == null, ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { unreadCount = count }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOne(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse<object>.FailureResponse("Authenticated user identity is missing."));
        var item = await _db.Notifications.AsNoTracking().Where(n => n.Id == id && n.UserId == userId)
            .Select(n => new { n.Id, n.Title, n.Message, n.Category, n.Link, n.CreatedAtUtc, n.ReadAtUtc, IsRead = n.ReadAtUtc != null })
            .FirstOrDefaultAsync(ct);
        return item is null ? NotFound(ApiResponse<object>.FailureResponse("Notification not found.")) : Ok(ApiResponse<object>.SuccessResponse(item));
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse<object>.FailureResponse("Authenticated user identity is missing."));
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, ct);
        if (notification is null) return NotFound(ApiResponse<object>.FailureResponse("Notification not found."));
        notification.ReadAtUtc ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { notification.Id, notification.ReadAtUtc, IsRead = true }, "Notification marked as read."));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized(ApiResponse<object>.FailureResponse("Authenticated user identity is missing."));
        var unread = await _db.Notifications.Where(n => n.UserId == userId && n.ReadAtUtc == null).ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var item in unread) item.ReadAtUtc = now;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.SuccessResponse(new { updatedCount = unread.Count }, "Notifications marked as read."));
    }
}
