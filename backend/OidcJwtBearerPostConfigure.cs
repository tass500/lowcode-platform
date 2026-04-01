using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LowCodePlatform.Backend;

/// <summary>
/// Binds OIDC JWT validation from <see cref="IConfiguration"/> after all providers are merged
/// (integration tests, environment, appsettings). Leaves options inert when <c>Auth:Oidc:Authority</c> is unset.
/// </summary>
public sealed class OidcJwtBearerPostConfigure : IPostConfigureOptions<JwtBearerOptions>
{
    private readonly IConfiguration _configuration;

    public OidcJwtBearerPostConfigure(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name != LcpAuthenticationSchemeNames.OidcJwt) return;

        var authority = _configuration["Auth:Oidc:Authority"]?.Trim();
        if (string.IsNullOrWhiteSpace(authority))
            return;

        options.Authority = authority;
        var metadata = _configuration["Auth:Oidc:MetadataAddress"]?.Trim();
        if (!string.IsNullOrWhiteSpace(metadata))
            options.MetadataAddress = metadata;

        var audience = _configuration["Auth:Oidc:Audience"]?.Trim();
        if (!string.IsNullOrWhiteSpace(audience))
            options.Audience = audience;

        if (bool.TryParse(_configuration["Auth:Oidc:RequireHttpsMetadata"], out var httpsMeta))
            options.RequireHttpsMetadata = httpsMeta;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = !string.IsNullOrWhiteSpace(audience),
            ValidAudience = string.IsNullOrWhiteSpace(audience) ? null : audience,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };

        var prior = options.Events;
        var events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                OidcJwtClaimMapping.Apply(ctx.Principal, _configuration);
                if (prior?.OnTokenValidated != null)
                    await prior.OnTokenValidated(ctx).ConfigureAwait(false);
            },
        };
        if (prior != null)
        {
            events.OnAuthenticationFailed = prior.OnAuthenticationFailed;
            events.OnChallenge = prior.OnChallenge;
            events.OnForbidden = prior.OnForbidden;
            events.OnMessageReceived = prior.OnMessageReceived;
        }

        options.Events = events;
    }
}
