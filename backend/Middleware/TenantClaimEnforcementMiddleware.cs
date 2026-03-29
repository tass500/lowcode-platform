using System.Security.Claims;
using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Services;

namespace LowCodePlatform.Backend.Middleware;

public sealed class TenantClaimEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public TenantClaimEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext ctx, TenantContext tenant)
    {
        // Admin APIs are authenticated separately; tenant routing may be "default" on localhost while JWT carries an operator tenant.
        if (ctx.Request.Path.StartsWithSegments("/api/admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        // Only enforce for authenticated calls. Anonymous calls (health, swagger, dev-token) remain unaffected.
        if (ctx.User?.Identity?.IsAuthenticated == true)
        {
            var claimedTenant = ctx.User.FindFirstValue("tenant");
            if (!string.IsNullOrWhiteSpace(claimedTenant)
                && !string.Equals(claimedTenant.Trim(), tenant.Slug, StringComparison.OrdinalIgnoreCase))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/json";

                await ctx.Response.WriteAsJsonAsync(new ErrorResponse(
                    ErrorCode: "tenant_mismatch",
                    Message: "Token tenant does not match resolved tenant.",
                    TraceId: TraceIdMiddleware.GetTraceId(ctx),
                    TimestampUtc: DateTime.UtcNow));

                return;
            }
        }

        await _next(ctx);
    }
}
