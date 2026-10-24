using EduLoan.Domain.Entities;
using EduLoan.Application.Interfaces;
using EduLoan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EduLoan.API.Services;

/// <summary>Audits successful mutating API actions without capturing request bodies or secrets.</summary>
public sealed class AuditActionFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IHttpContextAccessor _http;
    public AuditActionFilter(AppDbContext db, ICurrentUserService current, IHttpContextAccessor http)
    { _db = db; _current = current; _http = http; }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        var mutating = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
        var result = await next();
        if (!mutating || result.Exception is not null || result.Canceled || result.HttpContext.Response.StatusCode < 200 || result.HttpContext.Response.StatusCode >= 300 || context.HttpContext.User.Identity?.IsAuthenticated != true) return;

        var route = context.ActionDescriptor.AttributeRouteInfo?.Template ?? context.ActionDescriptor.DisplayName ?? "API";
        var entityType = route.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "API";
        var entityId = context.RouteData.Values.TryGetValue("id", out var id) ? Convert.ToString(id) :
            context.RouteData.Values.TryGetValue("applicationId", out var appId) ? Convert.ToString(appId) : null;
        var role = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;
        var actionName = $"{method} {route}";
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), ActorUserId = _current.UserId, ActorRole = role,
            Action = actionName[..Math.Min(120, actionName.Length)],
            EntityType = entityType[..Math.Min(120, entityType.Length)], EntityId = entityId is null ? null : entityId[..Math.Min(100, entityId.Length)],
            Outcome = "Success", CreatedAtUtc = DateTime.UtcNow,
            CorrelationId = _http.HttpContext?.TraceIdentifier, Metadata = "Successful mutating API request; request body intentionally omitted."
        });
        await _db.SaveChangesAsync(context.HttpContext.RequestAborted);
    }
}
