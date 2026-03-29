using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class SpaOidcConfigTests
{
    private sealed class FactoryWithSpaOidc : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"lcp-spa-oidc-{Guid.NewGuid():N}.db")}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Auth:Oidc:Authority"] = "https://login.example.com/v2.0",
                    ["Auth:Oidc:SpaClientId"] = "spa-client-id",
                    ["Auth:Oidc:SpaScope"] = "openid profile",
                    ["Auth:Oidc:TenantClaimSource"] = "tid,tenant",
                });
            });
        }
    }

    [Fact]
    public async Task Spa_oidc_config_returns_json_when_oidc_and_spa_client_configured()
    {
        await using var factory = new FactoryWithSpaOidc();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/spa-oidc-config");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var json = await resp.Content.ReadAsStringAsync();
        Assert.Contains("spa-client-id", json, StringComparison.Ordinal);
        Assert.Contains("https://login.example.com/v2.0", json, StringComparison.Ordinal);
        Assert.Contains("openid profile", json, StringComparison.Ordinal);
        Assert.Contains("/lowcode/auth/callback", json, StringComparison.Ordinal);
    }

    private sealed class FactoryOidcNoSpaClient : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"lcp-no-spa-{Guid.NewGuid():N}.db")}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Auth:Oidc:Authority"] = "https://login.example.com/v2.0",
                    ["Auth:Oidc:Audience"] = "api://dummy",
                });
            });
        }
    }

    [Fact]
    public async Task Spa_oidc_config_is_not_found_without_spa_client_id()
    {
        await using var factory = new FactoryOidcNoSpaClient();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/auth/spa-oidc-config");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
