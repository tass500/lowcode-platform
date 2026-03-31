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

public sealed class EntityFieldEndpointsTests
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

    private static async Task<Guid> CreateEntityAsync(HttpClient client, string name)
    {
        using var createEntity = await client.PostAsJsonAsync("/api/entities", new { name });
        Assert.Equal(HttpStatusCode.OK, createEntity.StatusCode);
        var created = await createEntity.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        return Guid.Parse(created!["entityDefinitionId"]!.ToString()!);
    }

    [Fact]
    public async Task Field_create_unknown_entity_returns_404()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var missingEntity = Guid.NewGuid();
        using var resp = await client.PostAsJsonAsync(
            $"/api/entities/{missingEntity}/fields",
            new { name = "x", fieldType = "string", isRequired = false });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("entity_not_found", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Field_create_duplicate_name_returns_409()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var entityId = await CreateEntityAsync(client, "WithFields");

        using (var a = await client.PostAsJsonAsync(
                   $"/api/entities/{entityId}/fields",
                   new { name = "dup", fieldType = "string", isRequired = false }))
            Assert.Equal(HttpStatusCode.OK, a.StatusCode);

        using var b = await client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { name = "dup", fieldType = "string", isRequired = false });
        Assert.Equal(HttpStatusCode.Conflict, b.StatusCode);
        var json = await b.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("field_already_exists", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Field_create_field_type_missing_returns_400()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var entityId = await CreateEntityAsync(client, "FieldTypeTest");

        using var resp = await client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { name = "a", fieldType = "   ", isRequired = false });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("field_type_missing", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Field_create_max_length_zero_returns_400()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var entityId = await CreateEntityAsync(client, "MaxLenTest");

        using var resp = await client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { name = "a", fieldType = "string", isRequired = false, maxLength = 0 });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("max_length_invalid", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Field_update_duplicate_name_returns_409()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var entityId = await CreateEntityAsync(client, "UpdDup");

        using var f1 = await client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { name = "Alpha", fieldType = "string", isRequired = false });
        Assert.Equal(HttpStatusCode.OK, f1.StatusCode);
        using var f2 = await client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { name = "Beta", fieldType = "string", isRequired = false });
        Assert.Equal(HttpStatusCode.OK, f2.StatusCode);
        var betaId = Guid.Parse((await f2.Content.ReadFromJsonAsync<Dictionary<string, object?>>()!)!["fieldDefinitionId"]!.ToString()!);

        using var put = await client.PutAsJsonAsync(
            $"/api/entities/{entityId}/fields/{betaId}",
            new { name = "Alpha", fieldType = "string", isRequired = false });
        Assert.Equal(HttpStatusCode.Conflict, put.StatusCode);
        var json = await put.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("field_already_exists", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Field_update_same_name_succeeds()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var entityId = await CreateEntityAsync(client, "StableField");

        using var post = await client.PostAsJsonAsync(
            $"/api/entities/{entityId}/fields",
            new { name = "Email", fieldType = "string", isRequired = true, maxLength = 200 });
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        var fieldId = Guid.Parse((await post.Content.ReadFromJsonAsync<Dictionary<string, object?>>()!)!["fieldDefinitionId"]!.ToString()!);

        using var put = await client.PutAsJsonAsync(
            $"/api/entities/{entityId}/fields/{fieldId}",
            new { name = "Email", fieldType = "string", isRequired = true, maxLength = 200 });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
    }

    [Fact]
    public async Task Field_delete_unknown_field_returns_404()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var entityId = await CreateEntityAsync(client, "DelField404");

        using var resp = await client.DeleteAsync($"/api/entities/{entityId}/fields/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("field_not_found", doc.RootElement.GetProperty("errorCode").GetString());
    }
}
