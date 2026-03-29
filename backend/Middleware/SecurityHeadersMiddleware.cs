namespace LowCodePlatform.Backend.Middleware;

/// <summary>
/// Adds baseline OWASP-friendly HTTP response headers for API surfaces (defense in depth; reverse proxy may add more).
/// Uses <see cref="HttpResponse.OnStarting"/> so headers also apply to error bodies written by exception handling.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            ApplyHeaders(context);
            return Task.CompletedTask;
        });

        return _next(context);
    }

    internal static void ApplyHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        if (headers.ContainsKey("X-Content-Type-Options"))
            return;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] =
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";
    }
}
