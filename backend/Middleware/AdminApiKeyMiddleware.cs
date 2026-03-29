using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Logging;
using System.Security.Claims;

namespace LowCodePlatform.Backend.Middleware;

public sealed class AdminApiKeyMiddleware
{
    public const string HeaderName = "X-Admin-Api-Key";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _cfg;
    private readonly IHostEnvironment _env;
    private readonly ILogger _securityAudit;

    public AdminApiKeyMiddleware(RequestDelegate next, IConfiguration cfg, IHostEnvironment env, ILoggerFactory loggerFactory)
    {
        _next = next;
        _cfg = cfg;
        _env = env;
        _securityAudit = loggerFactory.CreateLogger(SecurityAuditLogger.CategoryName);
    }

    public async Task Invoke(HttpContext ctx)
    {
        if (!ctx.Request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        // If a JWT-authenticated user has the admin role, allow access without API key.
        // This supports a gradual migration from API key -> JWT-based admin auth.
        if (ctx.User?.Identity?.IsAuthenticated == true && ctx.User.IsInRole("admin"))
        {
            await _next(ctx);
            return;
        }

        var allowFallbackConfigured = _cfg["Admin:AllowApiKeyFallback"];
        var allowFallback = bool.TryParse(allowFallbackConfigured, out var parsed)
            ? parsed
            : (_env.IsDevelopment() || _env.IsEnvironment("Testing"));

        if (!allowFallback)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse(
                ErrorCode: "admin_api_key_fallback_disabled",
                Message: "Admin API key fallback is disabled. Use an admin JWT token.",
                TraceId: TraceIdMiddleware.GetTraceId(ctx),
                TimestampUtc: DateTime.UtcNow));
            return;
        }

        var configured = _cfg["Admin:ApiKey"];

        if (string.IsNullOrWhiteSpace(configured))
        {
            if (_env.IsDevelopment() || _env.IsEnvironment("Testing"))
            {
                await _next(ctx);
                return;
            }

            SecurityAuditLogger.LogSecurityConfigError(_securityAudit, "admin_api_key_not_configured", ctx);
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse(
                ErrorCode: "admin_api_key_not_configured",
                Message: "Admin API key is not configured.",
                TraceId: TraceIdMiddleware.GetTraceId(ctx),
                TimestampUtc: DateTime.UtcNow));
            return;
        }

        var provided = ctx.Request.Headers.TryGetValue(HeaderName, out var v) ? v.ToString().Trim() : null;

        if (!string.Equals(provided, configured.Trim(), StringComparison.Ordinal))
        {
            SecurityAuditLogger.LogAuthDenied(_securityAudit, "admin_unauthorized", ctx);
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new ErrorResponse(
                ErrorCode: "admin_unauthorized",
                Message: $"Missing or invalid {HeaderName}.",
                TraceId: TraceIdMiddleware.GetTraceId(ctx),
                TimestampUtc: DateTime.UtcNow));
            return;
        }

        await _next(ctx);
    }
}
