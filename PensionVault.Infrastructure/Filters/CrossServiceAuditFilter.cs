using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using PensionVault.Infrastructure.Services;

namespace PensionVault.Infrastructure.Filters;

/// <summary>
/// Action filter for non-Members microservices.
/// Uses IRawAuditWriter (raw SQL) to write audit logs to the central Members DB
/// without triggering EF model building for AuditLog/User/AnnuityPlan etc.
/// </summary>
public class CrossServiceAuditFilter : IAsyncActionFilter
{
    private readonly IRawAuditWriter _auditWriter;

    public CrossServiceAuditFilter(IRawAuditWriter auditWriter)
    {
        _auditWriter = auditWriter;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();

        if (resultContext.Exception == null &&
            resultContext.HttpContext.Response.StatusCode >= 200 &&
            resultContext.HttpContext.Response.StatusCode < 300)
        {
            var method = resultContext.HttpContext.Request.Method;
            if (method != "POST" && method != "PUT" && method != "DELETE") return;

            var path = resultContext.HttpContext.Request.Path.Value ?? "";
            if (path.Contains("/api/auth/login") || path.Contains("/api/auth/refresh")) return;

            var userIdClaim = resultContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId)) return;

            var action     = context.ActionDescriptor.RouteValues["action"] ?? "Unknown";
            var controller = context.ActionDescriptor.RouteValues["controller"] ?? "Unknown";

            context.RouteData.Values.TryGetValue("id", out var idVal);
            var recordId = idVal?.ToString();

            await _auditWriter.WriteAsync(userId, $"{action}{controller}", controller, recordId);
        }
    }
}
