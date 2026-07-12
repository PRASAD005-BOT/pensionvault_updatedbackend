using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using PensionVault.Domain.Entities;

namespace PensionVault.Infrastructure.Filters;

/// <summary>
/// Automatically captures POST/PUT/DELETE actions into AuditLogs.
/// Works with either AppDbContext (for Members service / monolith) or
/// AuditDbContext (for non-Members microservices writing to the shared Members DB).
/// </summary>
public class AuditLogFilter : IAsyncActionFilter
{
    private readonly Microsoft.EntityFrameworkCore.DbContext _db;
    private readonly Microsoft.EntityFrameworkCore.DbSet<AuditLog> _auditLogs;

    public AuditLogFilter(Data.AppDbContext context)
    {
        _db = context;
        _auditLogs = context.AuditLogs;
    }

    public AuditLogFilter(Data.AuditDbContext context)
    {
        _db = context;
        _auditLogs = context.AuditLogs;
    }

    public AuditLogFilter(Data.MembersDbContext context)
    {
        _db = context;
        _auditLogs = context.AuditLogs;
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
                    _auditLogs.Add(auditLog);
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
