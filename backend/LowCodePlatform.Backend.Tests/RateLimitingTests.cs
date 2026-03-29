using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class RateLimitingTests
{
    /// <summary>
    /// Default Testing env disables rate limiting unless <c>RateLimiting:Enabled</c> is true.
    /// </summary>
    private sealed class FactoryWithRateLimit : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={Path.Combine(Path.GetTempPath(), $"lcp-rl-{Guid.NewGuid():N}.db")}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Auth:Oidc:Authority"] = "https://login.example.com/v2.0",
                    ["Auth:Oidc:SpaClientId"] = "spa-client-id",
                    ["RateLimiting:Enabled"] = "true",
                    ["RateLimiting:PermitLimit"] = "2",
                    ["RateLimiting:WindowSeconds"] = "60",
                });
            });
        }
    }

    [Fact]
    public async Task Global_rate_limit_returns_429_when_per_ip_quota_exceeded()
    {
        await using var factory = new FactoryWithRateLimit();
        using var client = factory.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            using var ok = await client.GetAsync("/api/auth/spa-oidc-config");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        using var limited = await client.GetAsync("/api/auth/spa-oidc-config");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
