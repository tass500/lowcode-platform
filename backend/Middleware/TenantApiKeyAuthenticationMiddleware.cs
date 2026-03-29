using System.Linq;
using System.Security.Claims;
using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Logging;
using LowCodePlatform.Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LowCodePlatform.Backend.Middleware;

/// <summary>
/// Authenticates tenant API requests via <see cref="HeaderName"/> when no JWT is present.
/// Runs after tenant resolution; key must match the resolved tenant row in management DB.
/// </summary>
public sealed class TenantApiKeyAuthenticationMiddleware
{
    public const string HeaderName = "X-Tenant-Api-Key";

    private readonly RequestDelegate _next;
    private readonly ILogger _securityAudit;

    public TenantApiKeyAuthenticationMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _securityAudit = loggerFactory.CreateLogger(SecurityAuditLogger.CategoryName);
    }

    public async Task Invoke(HttpContext ctx, TenantContext tenantCtx, ManagementDbContext managementDb)
    {
        if (ctx.User?.Identity?.IsAuthenticated == true)
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var headerVals))
        {
            await _next(ctx);
            return;
        }

        var provided = headerVals.FirstOrDefault()?.Trim();
        if (string.IsNullOrEmpty(provided))
        {
            await _next(ctx);
            return;
        }

        var slug = tenantCtx.Slug?.Trim();
        if (string.IsNullOrEmpty(slug))
        {
            SecurityAuditLogger.LogAuthDenied(_securityAudit, "tenant_not_resolved", ctx);
            await WriteJsonErrorAsync(ctx, StatusCodes.Status400BadRequest, "tenant_not_resolved",
                "Tenant could not be resolved for API key authentication.");
            return;
        }

        var row = await managementDb.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug, ctx.RequestAborted);

        if (row is null
            || string.IsNullOrWhiteSpace(row.TenantApiKeySha256Hex)
            || !TenantApiKeyHasher.SlowEquals(provided, row.TenantApiKeySha256Hex))
        {
            SecurityAuditLogger.LogAuthDenied(_securityAudit, "tenant_api_key_invalid", ctx);
            await WriteJsonErrorAsync(ctx, StatusCodes.Status401Unauthorized, "tenant_api_key_invalid",
                $"Missing or invalid {HeaderName}.");
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "tenant-api-key"),
            new("tenant", slug),
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "TenantApiKey");
        ctx.User = new ClaimsPrincipal(identity);

        await _next(ctx);
    }

    private static async Task WriteJsonErrorAsync(HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsJsonAsync(new ErrorResponse(
            ErrorCode: code,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(ctx),
            TimestampUtc: DateTime.UtcNow));
    }
}
