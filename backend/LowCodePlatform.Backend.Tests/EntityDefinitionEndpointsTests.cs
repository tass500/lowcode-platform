using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class EntityDefinitionEndpointsTests
{
    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _tenantSlug;
        private readonly string _mgmtDbPath;
        private readonly string _tenantDbPath;

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

    private static HttpClient CreateTenantClient(WebApplicationFactory<Program> factory, string tenantSlug)
    {
        var client = factory.CreateClient();
        client.BaseAddress = new Uri($"http://{tenantSlug}.localhost");
        return client;
    }

    private static async Task AuthenticateAsync(HttpClient client, string tenantSlug)
    {
        var req = new { subject = "test-user", tenantSlug = tenantSlug, roles = new string[] { } };
        using var resp = await client.PostAsJsonAsync("/api/auth/dev-token", req);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        var token = payload!["accessToken"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    [Fact]
    public async Task Entity_CRUD_should_work()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        // Create entity
        using var createResp = await client.PostAsJsonAsync("/api/entities", new { name = "Customer" });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);

        var entityId = Guid.Parse(created!["entityDefinitionId"]!.ToString()!);

        // Get entity
        using var getResp = await client.GetAsync($"/api/entities/{entityId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        // Update entity
        using var updateResp = await client.PutAsJsonAsync($"/api/entities/{entityId}", new { name = "Customer2" });
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        // Delete entity
        using var deleteResp = await client.DeleteAsync($"/api/entities/{entityId}");
        Assert.Equal(HttpStatusCode.OK, deleteResp.StatusCode);

        // Get after delete => 404
        using var getAfterDeleteResp = await client.GetAsync($"/api/entities/{entityId}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDeleteResp.StatusCode);
    }

    [Fact]
    public async Task Tenant_isolation_should_hold_for_entities()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");

        var t1DbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");
        var t2DbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t2-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var t1Cs = $"Data Source={t1DbPath}";
        var t2Cs = $"Data Source={t2DbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", t1Cs, CancellationToken.None);
        await InitializeDatabasesAsync(managementCs, "t2", t2Cs, CancellationToken.None);

        await using var f1 = new TestAppFactory("t1", mgmtDbPath, t1DbPath);
        await using var f2 = new TestAppFactory("t2", mgmtDbPath, t2DbPath);

        using var c1 = CreateTenantClient(f1, "t1");
        using var c2 = CreateTenantClient(f2, "t2");

        await AuthenticateAsync(c1, "t1");
        await AuthenticateAsync(c2, "t2");

        using var createResp = await c1.PostAsJsonAsync("/api/entities", new { name = "OnlyInT1" });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        using var list1 = await c1.GetAsync("/api/entities");
        Assert.Equal(HttpStatusCode.OK, list1.StatusCode);
        var list1Payload = await list1.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(list1Payload);

        using var list2 = await c2.GetAsync("/api/entities");
        Assert.Equal(HttpStatusCode.OK, list2.StatusCode);
        var list2Payload = await list2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(list2Payload);

        var items1 = (System.Text.Json.JsonElement)list1Payload!["items"]!;
        var items2 = (System.Text.Json.JsonElement)list2Payload!["items"]!;

        Assert.True(items1.GetArrayLength() == 1);
        Assert.True(items2.GetArrayLength() == 0);
    }
}
