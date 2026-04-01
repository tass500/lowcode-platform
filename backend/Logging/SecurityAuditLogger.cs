using LowCodePlatform.Backend.Middleware;

namespace LowCodePlatform.Backend.Logging;

/// <summary>
/// Structured security-relevant log lines (no secrets, no raw API key material).
/// Use category filter <c>LowCodePlatform.Backend.SecurityAudit</c> or message prefix for SIEM routing.
/// </summary>
public static class SecurityAuditLogger
{
    public const string CategoryName = "LowCodePlatform.Backend.SecurityAudit";

    /// <summary>Authentication/authorization denied (401/403-style outcomes).</summary>
    public static void LogAuthDenied(ILogger logger, string eventId, HttpContext ctx, string? reasonCode = null)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (path.Length > 512)
            path = path[..512];

        var traceId = TraceIdMiddleware.GetTraceId(ctx);
        var code = reasonCode ?? eventId;

        logger.LogWarning(
            "security_auth_denied eventId={SecurityEventId} path={Path} traceId={TraceId} reasonCode={ReasonCode}",
            eventId,
            path,
            traceId,
            code);
    }

    /// <summary>Server-side security configuration missing (e.g. admin key not set in production).</summary>
    public static void LogSecurityConfigError(ILogger logger, string eventId, HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (path.Length > 512)
            path = path[..512];

        var traceId = TraceIdMiddleware.GetTraceId(ctx);

        logger.LogError(
            "security_config_error eventId={SecurityEventId} path={Path} traceId={TraceId}",
            eventId,
            path,
            traceId);
    }
}
