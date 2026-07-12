using System.Net;

namespace PensionVault.Contributions.Service.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex)
        {
            var statusCode = GetStatusCode(ex);
            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            else
                _logger.LogWarning("Handled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, statusCode);
        }
    }

    private static HttpStatusCode GetStatusCode(Exception ex) => ex switch
    {
        UnauthorizedAccessException => HttpStatusCode.Unauthorized,
        KeyNotFoundException => HttpStatusCode.NotFound,
        InvalidOperationException => HttpStatusCode.BadRequest,
        ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };

    private static Task HandleExceptionAsync(HttpContext context, Exception ex, HttpStatusCode code)
    {
        var msg = code == HttpStatusCode.InternalServerError ? "An unexpected error occurred." : ex.Message;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
        {
            status = (int)code, error = msg, timestamp = DateTime.UtcNow
        }));
    }
}

