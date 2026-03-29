namespace LowCodePlatform.Backend;

/// <summary>Named JWT authentication schemes for symmetric (dev) vs OIDC metadata validation.</summary>
public static class LcpAuthenticationSchemeNames
{
    /// <summary>JWTs validated via IdP discovery (<see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions.Authority"/>).</summary>
    public const string OidcJwt = "OidcJwt";

    /// <summary>Selects <see cref="Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme"/> or <see cref="OidcJwt"/> from the Bearer token issuer.</summary>
    public const string JwtForwarder = "LcpJwtForwarder";
}
