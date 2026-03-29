using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace LowCodePlatform.Backend;

/// <summary>
/// Picks symmetric JWT vs OIDC-backed JWT so both can use the same <c>Authorization: Bearer</c> header.
/// </summary>
public static class LcpJwtForwardSelector
{
    /// <summary>
    /// Returns <see cref="LcpAuthenticationSchemeNames.OidcJwt"/> when the (unverified) token issuer matches
    /// <paramref name="oidcAuthority"/> or any entry in <paramref name="extraValidIssuers"/>; otherwise symmetric JWT.
    /// </summary>
    public static string SelectScheme(HttpContext context, string? oidcAuthority, string[]? extraValidIssuers)
    {
        if (string.IsNullOrWhiteSpace(oidcAuthority) && (extraValidIssuers == null || extraValidIssuers.Length == 0))
            return JwtBearerDefaults.AuthenticationScheme;

        if (!context.Request.Headers.TryGetValue("Authorization", out var headerVals))
            return JwtBearerDefaults.AuthenticationScheme;

        var raw = headerVals.ToString();
        if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return JwtBearerDefaults.AuthenticationScheme;

        var token = raw.AsSpan("Bearer ".Length).Trim();
        if (token.IsEmpty)
            return JwtBearerDefaults.AuthenticationScheme;

        string iss;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token.ToString()))
                return JwtBearerDefaults.AuthenticationScheme;
            iss = handler.ReadJwtToken(token.ToString()).Issuer;
        }
        catch
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }

        if (string.IsNullOrEmpty(iss))
            return JwtBearerDefaults.AuthenticationScheme;

        if (extraValidIssuers is { Length: > 0 })
        {
            foreach (var allowed in extraValidIssuers)
            {
                if (string.IsNullOrWhiteSpace(allowed)) continue;
                if (string.Equals(iss.Trim(), allowed.Trim(), StringComparison.OrdinalIgnoreCase))
                    return LcpAuthenticationSchemeNames.OidcJwt;
            }
        }

        if (!string.IsNullOrWhiteSpace(oidcAuthority) && IssuerMatchesAuthority(iss, oidcAuthority))
            return LcpAuthenticationSchemeNames.OidcJwt;

        return JwtBearerDefaults.AuthenticationScheme;
    }

    private static bool IssuerMatchesAuthority(string tokenIssuer, string configuredAuthority)
    {
        var iss = tokenIssuer.TrimEnd('/');
        var auth = configuredAuthority.TrimEnd('/');
        if (string.Equals(iss, auth, StringComparison.OrdinalIgnoreCase))
            return true;
        if (iss.StartsWith(auth + "/", StringComparison.OrdinalIgnoreCase))
            return true;
        if (auth.StartsWith(iss + "/", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
