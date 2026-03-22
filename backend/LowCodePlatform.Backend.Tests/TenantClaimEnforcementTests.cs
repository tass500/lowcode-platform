using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class TenantClaimEnforcementTests
{
    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _mgmtDbPath;
        private readonly string _tenantDbPath;
        private readonly string _tenantSlug;

        public TestAppFactory(string tenantSlug, string mgmtDbPath, string tenantDbPath)
        {
            _tenantSlug = tenantSlug;
            _mgmtDbPath = mgmtDbPath;
            _tenantDbPath = tenantDbPath;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));
            builder.ConfigureAppConfiguration(cfg =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={_mgmtDbPath}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Tenancy:DefaultTenantConnectionString"] = $"Data Source={_tenantDbPath}",
                    ["Tenancy:DefaultTenantConnectionStringSecretRef"] = _tenantSlug,
                    [$"Tenancy:Secrets:{_tenantSlug}"] = $"Data Source={_tenantDbPath}",
                    ["Admin:ApiKey"] = "test-admin-key",
                    ["Auth:Jwt:SigningKey"] = "test-signing-key-please-change-32bytes!!",
                });
            });
        }
    }

    private static async Task InitializeDatabasesAsync(string managementCs, string tenantSlug, string tenantCs, CancellationToken ct)
    {
        var managementOptions = new DbContextOptionsBuilder<LowCodePlatform.Backend.Data.ManagementDbContext>()
            .UseSqlite(managementCs)
            .Options;

        await using (var mgmt = new LowCodePlatform.Backend.Data.ManagementDbContext(managementOptions))
        {
            await mgmt.Database.MigrateAsync(ct);

            if (!await mgmt.Tenants.AnyAsync(x => x.Slug == tenantSlug, ct))
            {
                mgmt.Tenants.Add(new LowCodePlatform.Backend.Models.Tenant
                {
                    TenantId = Guid.NewGuid(),
                    Slug = tenantSlug,
                    ConnectionStringSecretRef = tenantSlug,
                    ConnectionString = null,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                await mgmt.SaveChangesAsync(ct);
            }
        }

        var tenantOptions = new DbContextOptionsBuilder<LowCodePlatform.Backend.Data.PlatformDbContext>()
            .UseSqlite(tenantCs)
            .Options;

        await using var tenantDb = new LowCodePlatform.Backend.Data.PlatformDbContext(tenantOptions);
        await tenantDb.Database.MigrateAsync(ct);
    }

    private static async Task<string> MintTokenAsync(HttpClient client, string tokenTenantSlug)
    {
        var req = new { subject = "test-user", tenantSlug = tokenTenantSlug, roles = new string[] { } };
        using var resp = await client.PostAsJsonAsync("/api/auth/dev-token", req);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);

        var token = payload!["accessToken"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    [Fact]
    public async Task Token_tenant_mismatch_should_be_forbidden()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var t1DbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");
        var t2DbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t2-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        await InitializeDatabasesAsync(managementCs, "t1", $"Data Source={t1DbPath}", CancellationToken.None);
        await InitializeDatabasesAsync(managementCs, "t2", $"Data Source={t2DbPath}", CancellationToken.None);

        await using var f1 = new TestAppFactory("t1", mgmtDbPath, t1DbPath);
        using var clientT2 = f1.CreateClient();
        clientT2.BaseAddress = new Uri("http://t2.localhost");

        // Token is minted for tenant t1, but request is routed to tenant t2.
        var token = await MintTokenAsync(clientT2, "t1");
        clientT2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await clientT2.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        Assert.Equal("tenant_mismatch", payload!["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Token_without_tenant_claim_should_be_forbidden_on_tenant_endpoints()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var t1DbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        await InitializeDatabasesAsync(managementCs, "t1", $"Data Source={t1DbPath}", CancellationToken.None);

        await using var f1 = new TestAppFactory("t1", mgmtDbPath, t1DbPath);
        using var clientT1 = f1.CreateClient();
        clientT1.BaseAddress = new Uri("http://t1.localhost");

        // Token minted without tenant claim.
        var token = await MintTokenAsync(clientT1, tokenTenantSlug: "");
        clientT1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await clientT1.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }
}
