using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Members.Data;
using Members.Domain.Entities;

namespace Members.API.Filters;

public class AuditLogFilter : IAsyncActionFilter
{
    private readonly MembersDbContext _db;

    public AuditLogFilter(MembersDbContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();

        if (resultContext.Exception == null &&
            resultContext.HttpContext.Response.StatusCode >= 200 &&
            resultContext.HttpContext.Response.StatusCode < 300)
        {
            var httpContext = resultContext.HttpContext;
            var method = httpContext.Request.Method;

            if (method == "POST" || method == "PUT" || method == "DELETE")
            {
                var path = httpContext.Request.Path.Value ?? "";

                if (path.Contains("/api/auth/login") || path.Contains("/api/auth/refresh"))
                    return;

                var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                    return;

                var actionName = context.ActionDescriptor.RouteValues["action"] ?? "Unknown";
                var controllerName = context.ActionDescriptor.RouteValues["controller"] ?? "Unknown";

                var cleanAction = $"{actionName}{controllerName}";
                if (cleanAction.EndsWith("sController"))
                    cleanAction = cleanAction[..^11];
                else if (cleanAction.EndsWith("Controller"))
                    cleanAction = cleanAction[..^10];

                string? recordId = null;
                if (context.RouteData.Values.TryGetValue("id", out var idVal))
                    recordId = idVal?.ToString();

                var auditLog = new AuditLog
                {
                    AuditId = Guid.NewGuid(),
                    UserId = userId,
                    Action = cleanAction,
                    EntityType = controllerName,
                    RecordId = recordId,
                    Timestamp = DateTime.UtcNow
                };

                try
                {
                    _db.AuditLogs.Add(auditLog);
                    await _db.SaveChangesAsync();
                }
                catch
                {
                    // Never let audit logging break the main request
                }
            }
        }
    }
}


