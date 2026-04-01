using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LowCodePlatform.Backend;
using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/auth")]
[NoStoreNoCache]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly IHostEnvironment _env;

    public AuthController(IConfiguration cfg, IHostEnvironment env)
    {
        _cfg = cfg;
        _env = env;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow));

    public sealed record DevTokenRequest(string Subject, string? TenantSlug, List<string>? Roles);

    public sealed record DevTokenResponse(DateTime ServerTimeUtc, string AccessToken, DateTime ExpiresAtUtc);

    public sealed record SpaOidcConfigDto(
        string Authority,
        string ClientId,
        string Scope,
        string RedirectPath,
        IReadOnlyList<string> TenantClaimSources);

    /// <summary>
    /// Non-secret OIDC hints for the SPA (code + PKCE). 404 when disabled or incomplete config.
    /// </summary>
    [HttpGet("spa-oidc-config")]
    [AllowAnonymous]
    public ActionResult<SpaOidcConfigDto> SpaOidcConfig()
    {
        var enabled = _env.IsDevelopment()
                      || _env.IsEnvironment("Testing")
                      || _cfg.GetValue("Auth:SpaOidcConfig:Enabled", false);
        if (!enabled)
            return NotFound();

        var authority = _cfg["Auth:Oidc:Authority"]?.Trim();
        var clientId = _cfg["Auth:Oidc:SpaClientId"]?.Trim();
        if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(clientId))
            return NotFound();

        var scope = (_cfg["Auth:Oidc:SpaScope"] ?? "openid profile offline_access").Trim();
        var redirectPath = (_cfg["Auth:Oidc:SpaRedirectPath"] ?? "/lowcode/auth/callback").Trim();
        if (!redirectPath.StartsWith('/'))
            redirectPath = "/" + redirectPath;

        var tenantSources = SplitCsv(_cfg["Auth:Oidc:TenantClaimSource"]);
        if (tenantSources.Count == 0)
            tenantSources = new List<string> { OidcJwtClaimMapping.TenantClaimType };

        return Ok(new SpaOidcConfigDto(
            Authority: authority,
            ClientId: clientId,
            Scope: scope,
            RedirectPath: redirectPath,
            TenantClaimSources: tenantSources));
    }

    private static List<string> SplitCsv(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();
    }

    [HttpPost("dev-token")]
    public ActionResult<DevTokenResponse> DevToken([FromBody] DevTokenRequest req)
    {
        if (!(_env.IsDevelopment() || _env.IsEnvironment("Testing")))
            return Problem(StatusCodes.Status404NotFound, "not_found", "Not found.");

        if (string.IsNullOrWhiteSpace(req.Subject))
            return Problem(StatusCodes.Status400BadRequest, "subject_missing", "Subject is required.");

        var signingKey = _cfg["Auth:Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
            return Problem(StatusCodes.Status500InternalServerError, "jwt_signing_key_not_configured", "Auth:Jwt:SigningKey is not configured.");

        var now = DateTime.UtcNow;
        var expires = now.AddHours(12);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, req.Subject.Trim()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        if (!string.IsNullOrWhiteSpace(req.TenantSlug))
            claims.Add(new Claim("tenant", req.TenantSlug.Trim()));

        foreach (var role in (req.Roles ?? new List<string>()).Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _cfg["Auth:Jwt:Issuer"],
            audience: _cfg["Auth:Jwt:Audience"],
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new DevTokenResponse(ServerTimeUtc: now, AccessToken: tokenString, ExpiresAtUtc: expires));
    }
}
