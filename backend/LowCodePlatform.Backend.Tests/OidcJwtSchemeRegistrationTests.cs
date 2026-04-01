using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class OidcJwtSchemeRegistrationTests
{
    private const string TestOidcAuthority = "https://login.microsoftonline.com/common/v2.0";

    private sealed class FactoryWithOidc : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"lcp-oidc-scheme-{Guid.NewGuid():N}.db")}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Auth:Oidc:Authority"] = TestOidcAuthority,
                    ["Auth:Oidc:Audience"] = "api://dummy-audience",
                });
            });
        }
    }

    private sealed class FactoryWithoutOidc : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"lcp-no-oidc-{Guid.NewGuid():N}.db")}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                });
            });
        }
    }

    [Fact]
    public async Task Oidc_jwt_options_pick_up_authority_from_configuration()
    {
        await using var factory = new FactoryWithOidc();
        using var _ = factory.CreateClient();

        var provider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await provider.GetAllSchemesAsync();
        var names = schemes.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(LcpAuthenticationSchemeNames.OidcJwt, names);
        Assert.Contains(LcpAuthenticationSchemeNames.JwtForwarder, names);

        var jwtOpts = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(LcpAuthenticationSchemeNames.OidcJwt);
        Assert.Equal(TestOidcAuthority, jwtOpts.Authority);
        Assert.Equal("api://dummy-audience", jwtOpts.Audience);
    }

    [Fact]
    public async Task Oidc_jwt_options_stay_unbound_when_authority_not_configured()
    {
        await using var factory = new FactoryWithoutOidc();
        using var _ = factory.CreateClient();

        var jwtOpts = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(LcpAuthenticationSchemeNames.OidcJwt);
        Assert.Null(jwtOpts.Authority);

        var provider = factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var schemes = await provider.GetAllSchemesAsync();
        var names = schemes.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(LcpAuthenticationSchemeNames.JwtForwarder, names);
    }
}
