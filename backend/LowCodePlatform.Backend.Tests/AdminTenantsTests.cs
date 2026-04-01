using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using LowCodePlatform.Backend.Services;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

/// <summary>
/// Integration coverage for <c>GET /api/admin/tenants</c> and basic <c>POST</c> validation (tenant registry).
/// </summary>
public sealed class AdminTenantsTests
{
    private sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        private readonly string _mgmtDbPath;
        private readonly string _tenantDbPath;

        public TestAppFactory(string mgmtDbPath, string tenantDbPath)
        {
            _mgmtDbPath = mgmtDbPath;
            _tenantDbPath = tenantDbPath;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));

            builder.ConfigureAppConfiguration(cfg =>
            {
                var tenantCs = $"Data Source={_tenantDbPath}";
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={_mgmtDbPath}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Tenancy:DefaultTenantConnectionString"] = tenantCs,
                    ["Tenancy:DefaultTenantConnectionStringSecretRef"] = "default",
                    ["Tenancy:Secrets:default"] = tenantCs,
                    ["Admin:ApiKey"] = "test-admin-key",
                    ["Auth:Jwt:SigningKey"] = "test-signing-key-please-change-32bytes!!",
                });
            });
        }
    }

    private static async Task PrepareDatabasesAsync(string managementCs, string tenantCs, CancellationToken ct)
    {
        var managementOptions = new DbContextOptionsBuilder<ManagementDbContext>()
            .UseSqlite(managementCs)
            .Options;

        await using (var mgmt = new ManagementDbContext(managementOptions))
        {
            await mgmt.Database.MigrateAsync(ct);

            if (!await mgmt.Tenants.AnyAsync(x => x.Slug == "default", ct))
            {
                mgmt.Tenants.Add(new Tenant
                {
                    TenantId = Guid.NewGuid(),
                    Slug = "default",
                    ConnectionStringSecretRef = "default",
                    ConnectionString = null,
                    CreatedAtUtc = DateTime.UtcNow,
                });
                await mgmt.SaveChangesAsync(ct);
            }
        }

        var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(tenantCs)
            .Options;

        await using var platform = new PlatformDbContext(platformOptions);
        await platform.Database.MigrateAsync(ct);

        var installation = new InstallationService(platform);
        await installation.EnsureDefaultInstallationAsync(ct);
    }

    private static async Task<string> MintAdminJwtAsync(HttpClient client)
    {
        var req = new { subject = "test-admin", tenantSlug = (string?)null, roles = new[] { "admin" } };
        using var resp = await client.PostAsJsonAsync("/api/auth/dev-token", req);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        var token = payload!["accessToken"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private sealed class TenantListResponseDto
    {
        public List<TenantListItemDto>? Items { get; set; }
    }

    private sealed class TenantListItemDto
    {
        public string Slug { get; set; } = "";
    }

    private static (string mgmtDbPath, string tenantDbPath) CreateTempDbPaths()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-admten-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-admten-tenant-{Guid.NewGuid():N}.db");
        return (mgmtDbPath, tenantDbPath);
    }

    [Fact]
    public async Task List_returns_ok_including_default_tenant()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var resp = await client.GetAsync("/api/admin/tenants");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<TenantListResponseDto>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(dto?.Items);
        Assert.Contains(dto!.Items!, x => x.Slug == "default");
    }

    [Fact]
    public async Task List_returns_401_without_bearer_token()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        using var resp = await client.GetAsync("/api/admin/tenants");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Create_returns_400_when_slug_missing()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var resp = await client.PostAsJsonAsync(
            "/api/admin/tenants",
            new { slug = "", connectionStringSecretRef = "ignored", connectionString = "Data Source=:memory:" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.Equal("tenant_slug_missing", doc.RootElement.GetProperty("errorCode").GetString());
    }
}
