using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

/// <summary>
/// Integration coverage for <c>GET /api/auth/spa-oidc-config</c> (62b2 SPA bootstrap contract).
/// </summary>
public sealed class AuthSpaOidcConfigTests
{
    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string?>? _extraConfig;

        public TestAppFactory(IReadOnlyDictionary<string, string?>? extraConfig = null) =>
            _extraConfig = extraConfig;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
                var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-{Guid.NewGuid():N}.db");

                var dict = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={mgmtDbPath}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Tenancy:DefaultTenantConnectionString"] = $"Data Source={tenantDbPath}",
                };

                if (_extraConfig is not null)
                {
                    foreach (var kv in _extraConfig)
                        dict[kv.Key] = kv.Value;
                }

                cfg.AddInMemoryCollection(dict);
            });
        }
    }

    private sealed class SpaOidcConfigJsonDto
    {
        public string Authority { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string Scope { get; set; } = "";
        public string RedirectPath { get; set; } = "";
        public List<string> TenantClaimSources { get; set; } = new();
    }

    [Fact]
    public async Task SpaOidcConfig_returns_NotFound_when_oidc_incomplete()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/spa-oidc-config");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task SpaOidcConfig_returns_ok_when_oidc_configured()
    {
        var extra = new Dictionary<string, string?>
        {
            ["Auth:Oidc:Authority"] = "https://idp.example.com",
            ["Auth:Oidc:SpaClientId"] = "spa-client-id",
        };

        await using var factory = new TestAppFactory(extra);
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/spa-oidc-config");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<SpaOidcConfigJsonDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(dto);
        Assert.Equal("https://idp.example.com", dto!.Authority);
        Assert.Equal("spa-client-id", dto.ClientId);
        Assert.Contains("openid", dto.Scope, StringComparison.Ordinal);
        Assert.StartsWith("/", dto.RedirectPath, StringComparison.Ordinal);
        Assert.Contains("tenant", dto.TenantClaimSources, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SpaOidcConfig_respects_TenantClaimSource_csv()
    {
        var extra = new Dictionary<string, string?>
        {
            ["Auth:Oidc:Authority"] = "https://idp.example.com",
            ["Auth:Oidc:SpaClientId"] = "spa-client-id",
            ["Auth:Oidc:TenantClaimSource"] = "email, sub",
        };

        await using var factory = new TestAppFactory(extra);
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/spa-oidc-config");

        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<SpaOidcConfigJsonDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(dto);
        Assert.Equal(2, dto!.TenantClaimSources.Count);
        Assert.Contains("email", dto.TenantClaimSources, StringComparer.Ordinal);
        Assert.Contains("sub", dto.TenantClaimSources, StringComparer.Ordinal);
    }
}
