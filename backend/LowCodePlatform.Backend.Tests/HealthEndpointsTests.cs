using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class HealthEndpointsTests
{
    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration(cfg =>
            {
                var dbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-{Guid.NewGuid():N}.db");

                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Platform"] = $"Data Source={dbPath}",
                });
            });

            return base.CreateHost(builder);
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
