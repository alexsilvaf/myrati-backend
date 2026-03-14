using System.Security.Claims;
using Myrati.Application.Abstractions;

namespace Myrati.API.Middleware;

public sealed class AuditLogMiddleware(RequestDelegate next)
{
    private static readonly string[] SkippedPrefixes =
    [
        "/health",
        "/swagger",
        "/api/v1/backoffice/events",
        "/api/v1/backoffice/notifications/stream",
        "/api/v1/public/status/stream"
    ];

    public async Task InvokeAsync(HttpContext context, IAuditLogWriter auditLogWriter, IHostEnvironment hostEnvironment)
    {
        if (!ShouldAudit(context.Request.Path))
        {
            await next(context);
            return;
        }

        var startedAt = DateTimeOffset.UtcNow;
        await next(context);

        var path = context.Request.Path.Value ?? string.Empty;
        var routeValues = context.Request.RouteValues;
        var resourceId = routeValues
            .Where(x => x.Key.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value?.ToString())
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        await auditLogWriter.WriteAsync(
            new AuditLogWriteRequest(
                startedAt,
                hostEnvironment.ApplicationName,
                BuildEventType(context.Request.Method, path),
                context.Request.Method,
                path,
                ResolveResourceType(path),
                resourceId,
                context.Response.StatusCode,
                ResolveOutcome(context.Response.StatusCode),
                context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                context.User.FindFirstValue(ClaimTypes.Email),
                context.User.FindFirstValue(ClaimTypes.Role),
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers.UserAgent.ToString(),
                context.TraceIdentifier),
            context.RequestAborted);
    }

    private static bool ShouldAudit(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        var value = path.Value!;
        if (SkippedPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return value.StartsWith("/api/v1/backoffice", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/api/v1/auth", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/api/v1/public/contact", StringComparison.OrdinalIgnoreCase)
            || value.Equals("/api/v1/public/licenses/activate", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildEventType(string method, string path) =>
        $"{method.Trim().ToLowerInvariant()}:{path.Trim().ToLowerInvariant()}";

    private static string? ResolveResourceType(string path)
    {
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length < 4)
        {
            return segments.LastOrDefault();
        }

        return segments[3];
    }

    private static string ResolveOutcome(int statusCode) =>
        statusCode switch
        {
            >= 200 and < 400 => "success",
            >= 400 and < 500 => "client_error",
            _ => "server_error"
        };
}
