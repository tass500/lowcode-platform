using Microsoft.Extensions.Options;

namespace LowCodePlatform.Backend.Middleware;

/// <summary>
/// Adds discovery-friendly headers for the public JSON API surface (<c>/api/*</c>).
/// Per-route <c>Deprecation</c> / <c>Sunset</c> can be layered later (RFC 8594 style).
/// </summary>
public sealed class ApiLifecycleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiLifecycleOptions _options;

    public ApiLifecycleMiddleware(RequestDelegate next, IOptions<ApiLifecycleOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public Task Invoke(HttpContext context)
    {
        if (!ShouldApply(context.Request.Path))
            return _next(context);

        var version = _options.PublicVersion?.Trim();
        if (string.IsNullOrEmpty(version))
            return _next(context);

        context.Response.OnStarting(() =>
        {
            ApplyVersionHeader(context, version);
            return Task.CompletedTask;
        });

        return _next(context);
    }

    public static bool ShouldApply(PathString path) =>
        path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    internal static void ApplyVersionHeader(HttpContext context, string version)
    {
        var headers = context.Response.Headers;
        if (!headers.ContainsKey("X-API-Version"))
            headers["X-API-Version"] = version;
    }
}
