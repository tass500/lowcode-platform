using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
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
