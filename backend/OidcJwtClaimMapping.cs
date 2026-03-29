using System.Security.Claims;

namespace LowCodePlatform.Backend;

/// <summary>
/// Adds <c>tenant</c> and optional <c>admin</c> role claims on OIDC JWTs when configured
/// (<c>Auth:Oidc:TenantClaimSource</c>, <c>Auth:Oidc:GrantAdminIfRoleContains</c>).
/// </summary>
public static class OidcJwtClaimMapping
{
    public const string TenantClaimType = "tenant";

    public static void Apply(ClaimsPrincipal? principal, IConfiguration configuration)
    {
        if (principal?.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return;

        MapTenant(principal, identity, configuration);
        MapAdminRole(principal, identity, configuration);
    }

    private static void MapTenant(ClaimsPrincipal principal, ClaimsIdentity identity, IConfiguration configuration)
    {
        if (principal.HasClaim(c => c.Type == TenantClaimType))
            return;

        var raw = configuration["Auth:Oidc:TenantClaimSource"]?.Trim();
        if (string.IsNullOrEmpty(raw))
            return;

        foreach (var typ in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var v = principal.FindFirst(typ)?.Value?.Trim();
            if (string.IsNullOrEmpty(v))
                continue;

            identity.AddClaim(new Claim(TenantClaimType, v));
            return;
        }
    }

    private static void MapAdminRole(ClaimsPrincipal principal, ClaimsIdentity identity, IConfiguration configuration)
    {
        var raw = configuration["Auth:Oidc:GrantAdminIfRoleContains"]?.Trim();
        if (string.IsNullOrEmpty(raw))
            return;

        var needles = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToArray();

        if (needles.Length == 0)
            return;

        if (principal.IsInRole("admin"))
            return;

        foreach (var claim in principal.Claims)
        {
            if (!IsRoleClaim(claim.Type))
                continue;

            foreach (var needle in needles)
            {
                if (claim.Value.Contains(needle, StringComparison.OrdinalIgnoreCase))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                    return;
                }
            }
        }
    }

    private static bool IsRoleClaim(string type) =>
        string.Equals(type, ClaimTypes.Role, StringComparison.Ordinal)
        || string.Equals(type, "role", StringComparison.OrdinalIgnoreCase)
        || string.Equals(type, "roles", StringComparison.OrdinalIgnoreCase);
}
