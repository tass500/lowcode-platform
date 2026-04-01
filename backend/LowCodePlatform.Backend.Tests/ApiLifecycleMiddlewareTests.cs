using LowCodePlatform.Backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class ApiLifecycleMiddlewareTests
{
    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        private readonly string? _publicVersion;

        public TestAppFactory(string? publicVersion = null) => _publicVersion = publicVersion;

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
                if (_publicVersion is not null)
                    dict["Api:PublicVersion"] = _publicVersion;

                cfg.AddInMemoryCollection(dict);
            });
        }
    }

    [Theory]
    [InlineData("/api/health", true)]
    [InlineData("/API/workflows", true)]
    [InlineData("/health", false)]
    [InlineData("/health/live", false)]
    [InlineData("/", false)]
    public void ShouldApply_matches_api_prefix_only(string path, bool expected)
    {
        Assert.Equal(expected, ApiLifecycleMiddleware.ShouldApply(new PathString(path)));
    }

    [Fact]
    public async Task Api_route_includes_X_API_Version_from_config()
    {
        await using var factory = new TestAppFactory("42");
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/health");
        resp.EnsureSuccessStatusCode();

        Assert.Equal("42", resp.Headers.GetValues("X-API-Version").First());
    }

    [Fact]
    public async Task When_PublicVersion_empty_header_omitted()
    {
        await using var factory = new TestAppFactory("");
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/health");
        resp.EnsureSuccessStatusCode();

        Assert.False(resp.Headers.Contains("X-API-Version"));
    }
}
