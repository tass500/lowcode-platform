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

            var method = SanitizeForLog(ctx.Request.Method);
            var path = SanitizeForLog(ctx.Request.Path.Value);
            var traceId = SanitizeForLog(TraceIdMiddleware.GetTraceId(ctx));
            var tenantSlug = SanitizeForLog(tenant.Slug);

            _logger.LogInformation(
                "http_request_finished method={Method} path={Path} status={StatusCode} durationMs={DurationMs} traceId={TraceId} tenant={TenantSlug}",
                method,
                path,
                ctx.Response.StatusCode,
                sw.ElapsedMilliseconds,
                traceId,
                tenantSlug);
        }
    }

    private static string SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        const int maxLen = 1024;
        var v = value.Length > maxLen ? value[..maxLen] : value;

        Span<char> buf = stackalloc char[v.Length * 2];
        var j = 0;
        foreach (var c in v)
        {
            if (j >= buf.Length - 2)
                break;

            switch (c)
            {
                case '\r':
                    buf[j++] = '\\';
                    buf[j++] = 'r';
                    break;
                case '\n':
                    buf[j++] = '\\';
                    buf[j++] = 'n';
                    break;
                case '\t':
                    buf[j++] = '\\';
                    buf[j++] = 't';
                    break;
                default:
                    if (char.IsControl(c))
                        break;
                    buf[j++] = c;
                    break;
            }
        }

        return new string(buf[..j]);
    }
}
