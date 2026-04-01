using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LowCodePlatform.Backend.Auth.Bff;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

/// <summary>
/// BFF session cookie → middleware injects Bearer → existing JWT + tenant resolution.
/// </summary>
public sealed class BffSessionBearerWorkflowTests
{
    private sealed class FactoryWithBff : WebApplicationFactory<Program>
    {
        private readonly string _mgmtDbPath;
        private readonly string _tenantDbPath;

        public FactoryWithBff(string mgmtDbPath, string tenantDbPath)
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
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Management"] = $"Data Source={_mgmtDbPath}",
                    ["Tenancy:DefaultTenantSlug"] = "default",
                    ["Tenancy:DefaultTenantConnectionString"] = $"Data Source={_tenantDbPath}",
                    ["Tenancy:DefaultTenantConnectionStringSecretRef"] = "t1",
                    ["Tenancy:Secrets:t1"] = $"Data Source={_tenantDbPath}",
                    ["Admin:ApiKey"] = "test-admin-key",
                    ["Auth:Jwt:SigningKey"] = "test-signing-key-please-change-32bytes!!",
                    ["Auth:Bff:Enabled"] = "true",
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

    [Fact]
    public async Task Bff_cookie_session_authorizes_workflows_list_without_bearer_header()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-bff-bearer-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-bff-bearer-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new FactoryWithBff(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();
        client.BaseAddress = new Uri("http://t1.localhost");

        using var devTokenResp = await client.PostAsJsonAsync(
            "/api/auth/dev-token",
            new { subject = "bff-cookie-user", tenantSlug = "t1", roles = Array.Empty<string>() });
        devTokenResp.EnsureSuccessStatusCode();
        var tokenJson = await devTokenResp.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenJson.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        var protector = factory.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Lcp.BffSession.v1");
        var payload = new BffSessionCookiePayload
        {
            AccessToken = token!,
            ExpiresAtUnix = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
        };
        var cookieVal = Convert.ToBase64String(
            protector.Protect(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload))));

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/workflows");
        req.Headers.Add("Cookie", $"lcp.bff.session={cookieVal}");

        using var listResp = await client.SendAsync(req);
        listResp.EnsureSuccessStatusCode();
    }
}
