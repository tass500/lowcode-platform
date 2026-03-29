using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using LowCodePlatform.Backend.Auth.Bff;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class BffAuthEndpointsTests
{
    private sealed class FactoryBffOff : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"lcp-bff-off-{Guid.NewGuid():N}.db")}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                });
            });
        }
    }

    private sealed class FactoryBffOn : WebApplicationFactory<Program>
    {
        public FakeOidcHttpForBff FakeOidc { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"lcp-bff-on-{Guid.NewGuid():N}.db")}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Auth:Bff:Enabled"] = "true",
                    ["Auth:Oidc:Authority"] = "https://idp.test",
                    ["Auth:Oidc:SpaClientId"] = "test-spa-client",
                    ["Auth:Oidc:SpaScope"] = "openid profile",
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IOidcHttpForBff>();
                services.AddSingleton<IOidcHttpForBff>(FakeOidc);
            });
        }
    }

    public sealed class FakeOidcHttpForBff : IOidcHttpForBff
    {
        public OidcDiscoveryDocument Discovery { get; set; } = new()
        {
            AuthorizationEndpoint = "https://idp.test/authorize",
            TokenEndpoint = "https://idp.test/token",
        };

        public OidcTokenResponse? NextToken { get; set; } = new()
        {
            AccessToken = "bff-test-access-token",
            ExpiresIn = 3600,
            RefreshToken = "bff-test-refresh",
        };

        public Task<OidcDiscoveryDocument?> GetDiscoveryAsync(string authority, CancellationToken cancellationToken = default) =>
            Task.FromResult<OidcDiscoveryDocument?>(Discovery);

        public Task<OidcTokenResponse?> ExchangeCodeAsync(
            string tokenEndpoint,
            IReadOnlyDictionary<string, string> form,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(NextToken);
    }

    [Fact]
    public async Task Bff_meta_when_disabled_reports_enabled_false()
    {
        await using var factory = new FactoryBffOff();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/bff/meta");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"enabled\":false", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Bff_login_when_disabled_returns_404()
    {
        await using var factory = new FactoryBffOff();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/bff/login");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Bff_logout_when_disabled_returns_404()
    {
        await using var factory = new FactoryBffOff();
        using var client = factory.CreateClient();

        using var resp = await client.PostAsync("/api/auth/bff/logout", null);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Bff_logout_when_enabled_returns_ok()
    {
        await using var factory = new FactoryBffOn();
        using var client = factory.CreateClient();

        using var resp = await client.PostAsync("/api/auth/bff/logout", null);
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Bff_meta_when_enabled_and_oidc_configured_reports_configured()
    {
        await using var factory = new FactoryBffOn();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/bff/meta");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"enabled\":true", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"configured\":true", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/api/auth/bff/login", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bff_session_without_cookie_reports_unauthenticated()
    {
        await using var factory = new FactoryBffOn();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/bff/session");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("\"authenticated\":false", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Bff_login_redirects_to_authorize_with_pkce()
    {
        await using var factory = new FactoryBffOn();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var resp = await client.GetAsync("/api/auth/bff/login");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, resp.StatusCode);
        var loc = resp.Headers.Location?.ToString() ?? "";
        Assert.StartsWith("https://idp.test/authorize", loc, StringComparison.Ordinal);
        Assert.Contains("code_challenge=", loc, StringComparison.Ordinal);
        Assert.Contains("state=", loc, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bff_callback_sets_session_cookie_session_json_has_no_raw_jwt_but_has_subject_hint()
    {
        await using var factory = new FactoryBffOn();
        factory.FakeOidc.NextToken = new OidcTokenResponse
        {
            AccessToken = CreateSignedJwtWithSub("user-42"),
            ExpiresIn = 3600,
        };

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var loginResp = await client.GetAsync("/api/auth/bff/login");
        var loc = loginResp.Headers.Location!;
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(loc.Query);
        var state = query["state"].ToString();

        using var cb = await client.GetAsync($"/api/auth/bff/callback?code=c&state={Uri.EscapeDataString(state)}");
        Assert.Equal(System.Net.HttpStatusCode.Redirect, cb.StatusCode);

        using var sess = await client.GetAsync("/api/auth/bff/session");
        var json = await sess.Content.ReadAsStringAsync();
        Assert.Contains("\"authenticated\":true", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(factory.FakeOidc.NextToken.AccessToken!, json, StringComparison.Ordinal);
        Assert.Contains("user-42", json, StringComparison.Ordinal);
    }

    private static string CreateSignedJwtWithSub(string sub)
    {
        var keyBytes = RandomNumberGenerator.GetBytes(32);
        var key = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, sub) },
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
