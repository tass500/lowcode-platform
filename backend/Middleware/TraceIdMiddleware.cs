namespace LowCodePlatform.Backend.Middleware;

public sealed class TraceIdMiddleware
{
    public const string TraceIdItemKey = "traceId";
    public const string TraceIdHeaderName = "X-Trace-Id";

    private readonly RequestDelegate _next;

    public TraceIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var traceId = ctx.Request.Headers.TryGetValue(TraceIdHeaderName, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString().Trim()
            : Guid.NewGuid().ToString("N");

        ctx.Items[TraceIdItemKey] = traceId;
        ctx.Response.Headers[TraceIdHeaderName] = traceId;

        await _next(ctx);
    }

    public static string GetTraceId(HttpContext ctx)
        => (ctx.Items.TryGetValue(TraceIdItemKey, out var v) ? v?.ToString() : null) ?? "unknown";
}
