using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class EntityRecordEndpointsTests
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
    public async Task Entity_records_list_unknown_entity_returns_404()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var missing = Guid.NewGuid();
        using var resp = await client.GetAsync($"/api/entities/{missing}/records");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Entity_records_list_empty_includes_serverTimeUtc_and_items_array()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var createEntity = await client.PostAsJsonAsync("/api/entities", new { name = "EmptyRecords" });
        Assert.Equal(HttpStatusCode.OK, createEntity.StatusCode);
        var created = await createEntity.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var entityId = Guid.Parse(created!["entityDefinitionId"]!.ToString()!);

        using var listResp = await client.GetAsync($"/api/entities/{entityId}/records");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("serverTimeUtc", out var st) && st.ValueKind == JsonValueKind.String);
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(0, items.GetArrayLength());
    }

    [Fact]
    public async Task Entity_records_list_orders_by_updated_at_desc_then_record_id_desc()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var createEntity = await client.PostAsJsonAsync("/api/entities", new { name = "OrderTest" });
        Assert.Equal(HttpStatusCode.OK, createEntity.StatusCode);
        var created = await createEntity.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var entityId = Guid.Parse(created!["entityDefinitionId"]!.ToString()!);

        using var post1 = await client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { dataJson = "{\"a\":1}" });
        Assert.Equal(HttpStatusCode.OK, post1.StatusCode);
        var rec1 = await post1.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(rec1);
        var recordId1 = Guid.Parse(rec1!["entityRecordId"]!.ToString()!);

        using var post2 = await client.PostAsJsonAsync($"/api/entities/{entityId}/records", new { dataJson = "{\"a\":2}" });
        Assert.Equal(HttpStatusCode.OK, post2.StatusCode);
        var rec2 = await post2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(rec2);
        _ = Guid.Parse(rec2!["entityRecordId"]!.ToString()!);

        using (var put = await client.PutAsJsonAsync($"/api/entities/{entityId}/records/{recordId1}", new { dataJson = "{\"a\":3}" }))
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        using var listResp = await client.GetAsync($"/api/entities/{entityId}/records");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(recordId1.ToString(), items[0].GetProperty("entityRecordId").GetString());
    }
}
