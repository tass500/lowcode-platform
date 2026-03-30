using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using LowCodePlatform.Backend.Tests.Deprecation;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class DeprecationHeadersIntegrationTests
{
    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
                var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-{Guid.NewGuid():N}.db");

                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={mgmtDbPath}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Tenancy:DefaultTenantConnectionString"] = $"Data Source={tenantDbPath}",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddControllers().AddApplicationPart(typeof(DeprecationProbeController).Assembly);
            });
        }
    }

    [Fact]
    public async Task Deprecated_probe_emits_Deprecation_Sunset_and_X_API_Version()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/_test/deprecation-probe");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("true", resp.Headers.GetValues("Deprecation").First());
        Assert.False(string.IsNullOrWhiteSpace(resp.Headers.GetValues("Sunset").First()));
        Assert.Equal("1", resp.Headers.GetValues("X-API-Version").First());
    }
}
