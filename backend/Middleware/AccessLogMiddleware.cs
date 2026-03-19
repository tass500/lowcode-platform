using System.Diagnostics;
using LowCodePlatform.Backend.Services;

namespace LowCodePlatform.Backend.Middleware;

public sealed class AccessLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AccessLogMiddleware> _logger;

    public AccessLogMiddleware(RequestDelegate next, ILogger<AccessLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext ctx, TenantContext tenant)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(ctx);
        }
        finally
        {
            sw.Stop();

            _logger.LogInformation(
                "http_request_finished method={Method} path={Path} status={StatusCode} durationMs={DurationMs} traceId={TraceId} tenant={TenantSlug}",
                ctx.Request.Method,
                ctx.Request.Path.Value ?? string.Empty,
                ctx.Response.StatusCode,
                sw.ElapsedMilliseconds,
                TraceIdMiddleware.GetTraceId(ctx),
                tenant.Slug);
        }
    }
}
