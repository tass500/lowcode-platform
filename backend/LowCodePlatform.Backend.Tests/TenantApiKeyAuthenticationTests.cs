using System.Text.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class TenantApiKeyAuthenticationTests
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

    private static async Task<string> MintTokenAsync(HttpClient client, string tenantSlug)
    {
        var req = new { subject = "test-user", tenantSlug, roles = Array.Empty<string>() };
        using var resp = await client.PostAsJsonAsync("/api/auth/dev-token", req);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        var token = payload!["accessToken"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private static async Task<string> MintAdminTokenAsync(HttpClient client)
    {
        // No tenant claim: admin calls often hit localhost / unresolved tenant; enforcement skips /api/admin anyway.
        var req = new { subject = "test-admin", tenantSlug = (string?)null, roles = new[] { "admin" } };
        using var resp = await client.PostAsJsonAsync("/api/auth/dev-token", req);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        var token = payload!["accessToken"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    [Fact]
    public async Task Tenant_api_key_allows_workflows_list_without_jwt()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-tk-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-tk-{Guid.NewGuid():N}.db");
        var managementCs = $"Data Source={mgmtDbPath}";
        await InitializeDatabasesAsync(managementCs, "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);

        using var admin = factory.CreateClient();
        var adminJwt = await MintAdminTokenAsync(admin);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminJwt);

        using var provisionResp = await admin.PostAsJsonAsync("/api/admin/tenants/t1/tenant-api-key", new { });
        provisionResp.EnsureSuccessStatusCode();
        var provisioned = await provisionResp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(provisioned);
        var apiKey = provisioned!["apiKey"].GetString();
        Assert.False(string.IsNullOrWhiteSpace(apiKey));

        using var tenant = CreateTenantClient(factory, "t1");
        tenant.DefaultRequestHeaders.Add(
            LowCodePlatform.Backend.Middleware.TenantApiKeyAuthenticationMiddleware.HeaderName,
            apiKey!);

        using var workflowsResp = await tenant.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.OK, workflowsResp.StatusCode);
    }

    [Fact]
    public async Task Invalid_tenant_api_key_is_unauthorized()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-tk2-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-tk2-{Guid.NewGuid():N}.db");
        var managementCs = $"Data Source={mgmtDbPath}";
        await InitializeDatabasesAsync(managementCs, "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);

        using var admin = factory.CreateClient();
        var adminJwt = await MintAdminTokenAsync(admin);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminJwt);
        using var provisionResp = await admin.PostAsJsonAsync("/api/admin/tenants/t1/tenant-api-key", new { });
        provisionResp.EnsureSuccessStatusCode();

        using var tenant = CreateTenantClient(factory, "t1");
        tenant.DefaultRequestHeaders.Add(
            LowCodePlatform.Backend.Middleware.TenantApiKeyAuthenticationMiddleware.HeaderName,
            "definitely-not-the-key-xxxxxxxxxxxx");

        using var workflowsResp = await tenant.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.Unauthorized, workflowsResp.StatusCode);
    }

    [Fact]
    public async Task Valid_jwt_takes_precedence_over_invalid_tenant_api_key_header()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-tk3-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-tk3-{Guid.NewGuid():N}.db");
        var managementCs = $"Data Source={mgmtDbPath}";
        await InitializeDatabasesAsync(managementCs, "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);

        using var admin = factory.CreateClient();
        var adminJwt = await MintAdminTokenAsync(admin);
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminJwt);
        using var provisionResp = await admin.PostAsJsonAsync("/api/admin/tenants/t1/tenant-api-key", new { });
        provisionResp.EnsureSuccessStatusCode();

        using var tenant = CreateTenantClient(factory, "t1");
        var token = await MintTokenAsync(tenant, "t1");
        tenant.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        tenant.DefaultRequestHeaders.Add(
            LowCodePlatform.Backend.Middleware.TenantApiKeyAuthenticationMiddleware.HeaderName,
            "wrong-key-but-jwt-should-win-xxxxxxxx");

        using var workflowsResp = await tenant.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.OK, workflowsResp.StatusCode);
    }
}
