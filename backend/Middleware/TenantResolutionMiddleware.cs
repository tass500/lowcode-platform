using LowCodePlatform.Backend.Services;

namespace LowCodePlatform.Backend.Middleware;

public sealed class TenantResolutionMiddleware
{
    public const string TenantHeaderName = "X-Tenant-Id";

    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;

    public TenantResolutionMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task Invoke(HttpContext ctx, TenantContext tenant)
    {
        var fromHost = TryGetTenantFromHost(ctx.Request.Host.Host);

        string? fromHeader = null;
        if (_env.IsDevelopment())
            fromHeader = TryGetTenantFromHeader(ctx);

        tenant.Slug = (fromHost ?? fromHeader ?? tenant.Slug).Trim();

        await _next(ctx);
    }

    private static string? TryGetTenantFromHeader(HttpContext ctx)
        => ctx.Request.Headers.TryGetValue(TenantHeaderName, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString().Trim()
            : null;

    private static string? TryGetTenantFromHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            return null;

        if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Length >= 2 ? parts[0] : null;
        }

        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return labels.Length >= 3 ? labels[0] : null;
    }
}
