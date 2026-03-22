using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class HealthEndpointsTests
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
        }
    }

    [Fact]
    public async Task Health_should_return_ok()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.True(payload.TryGetValue("status", out var status));
        Assert.Equal("ok", status);
    }

    [Fact]
    public async Task ApiHealth_should_return_ok()
    {
        await using var factory = new TestAppFactory();
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.NotNull(payload);
        Assert.True(payload.TryGetValue("status", out var status));
        Assert.Equal("ok", status);
    }
}
