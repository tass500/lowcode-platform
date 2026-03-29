using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LowCodePlatform.Backend;

/// <summary>
/// Binds JWT validation from <see cref="IConfiguration"/> after all providers are registered
/// (including WebApplicationFactory in-memory overrides). Without this, <c>AddJwtBearer</c> can run
/// before test configuration is merged and the signing key fallback would not match minted dev tokens.
/// </summary>
public sealed class JwtBearerIssuerAudiencePostConfigure : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly IConfiguration _configuration;

    public JwtBearerIssuerAudiencePostConfigure(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var signingKey = _configuration["Auth:Jwt:SigningKey"] ?? "dev-insecure-signing-key-change-me";
        options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

        var jwtIssuer = _configuration["Auth:Jwt:Issuer"];
        var jwtAudience = _configuration["Auth:Jwt:Audience"];
        options.TokenValidationParameters.ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer);
        options.TokenValidationParameters.ValidIssuer = options.TokenValidationParameters.ValidateIssuer ? jwtIssuer : null;
        options.TokenValidationParameters.ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience);
        options.TokenValidationParameters.ValidAudience = options.TokenValidationParameters.ValidateAudience ? jwtAudience : null;
    }
}
