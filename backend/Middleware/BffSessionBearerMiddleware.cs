using LowCodePlatform.Backend.Auth.Bff;

namespace LowCodePlatform.Backend.Middleware;

/// <summary>
/// When no Authorization header is present, copies a valid BFF session access token into <c>Authorization: Bearer</c>
/// so the existing JWT authentication pipeline (forwarder + symmetric / OIDC) runs unchanged.
/// </summary>
public sealed class BffSessionBearerMiddleware
{
    private readonly RequestDelegate _next;

    public BffSessionBearerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IBffSessionReader sessionReader)
    {
        var existing = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(existing)
            && existing.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (sessionReader.TryGetValidPayload(context, out var payload)
            && !string.IsNullOrEmpty(payload.AccessToken))
        {
            context.Request.Headers.Authorization = "Bearer " + payload.AccessToken;
        }

        await _next(context).ConfigureAwait(false);
    }
}
