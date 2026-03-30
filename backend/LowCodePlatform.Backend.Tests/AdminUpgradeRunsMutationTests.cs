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
/// Integration coverage for mutating <c>/api/admin/upgrade-runs</c> (start, cancel, get by id) and enforcement.
/// </summary>
public sealed class AdminUpgradeRunsMutationTests
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

    private static async Task ForceHardBlockEnforcementAsync(string tenantCs, CancellationToken ct)
    {
        var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(tenantCs)
            .Options;

        await using var platform = new PlatformDbContext(platformOptions);
        var inst = await platform.Installations.OrderBy(x => x.CreatedAtUtc).FirstAsync(ct);
        inst.CurrentVersion = "0.0.1";
        inst.SupportedVersion = "9.9.9";
        inst.ReleaseDateUtc = DateTime.UtcNow.AddDays(-100);
        inst.UpgradeWindowDays = 60;
        inst.UpdatedAtUtc = DateTime.UtcNow;
        await platform.SaveChangesAsync(ct);
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

    private sealed class StartUpgradeResponseDto
    {
        public Guid UpgradeRunId { get; set; }
    }

    private sealed class UpgradeRunDetailsDto
    {
        public Guid UpgradeRunId { get; set; }
        public string TargetVersion { get; set; } = "";
        public string State { get; set; } = "";
        public List<StepDto>? Steps { get; set; }
    }

    private sealed class StepDto
    {
        public string StepKey { get; set; } = "";
        public string State { get; set; } = "";
    }

    private static (string mgmtDbPath, string tenantDbPath) CreateTempDbPaths()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-upgm-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-upgm-tenant-{Guid.NewGuid():N}.db");
        return (mgmtDbPath, tenantDbPath);
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Start_returns_ok_and_upgrade_run_id()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        var tenantCs = $"Data Source={tenantDbPath}";
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var resp = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "1.2.3" });

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = await resp.Content.ReadFromJsonAsync<StartUpgradeResponseDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto!.UpgradeRunId);
    }

    [Fact]
    public async Task Start_returns_409_when_active_run_already_exists()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        var tenantCs = $"Data Source={tenantDbPath}";
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using (var first = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "1.0.0" }))
        {
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        }

        using var second = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "1.0.1" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var doc = await JsonDocument.ParseAsync(await second.Content.ReadAsStreamAsync());
        Assert.Equal("upgrade_run_already_active", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Start_returns_400_when_target_version_missing()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var resp = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.Equal("target_version_missing", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Start_returns_403_when_supported_version_hard_block()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        var tenantCs = $"Data Source={tenantDbPath}";
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", tenantCs, CancellationToken.None);
        await ForceHardBlockEnforcementAsync(tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var resp = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "1.0.0" });

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
        var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        Assert.Equal("supported_version_enforcement_block", doc.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task GetById_returns_pending_run_with_steps_after_start()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var startResp = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "2.0.0" });
        startResp.EnsureSuccessStatusCode();
        var start = await startResp.Content.ReadFromJsonAsync<StartUpgradeResponseDto>(JsonOpts);
        Assert.NotNull(start);

        using var getResp = await client.GetAsync($"/api/admin/upgrade-runs/{start!.UpgradeRunId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var details = await getResp.Content.ReadFromJsonAsync<UpgradeRunDetailsDto>(JsonOpts);
        Assert.NotNull(details);
        Assert.Equal(UpgradeRunStates.Pending, details!.State);
        Assert.NotNull(details.Steps);
        Assert.Equal(2, details.Steps!.Count);
        Assert.Contains(details.Steps, s => s.StepKey == "canary-migrate");
        Assert.Contains(details.Steps, s => s.StepKey == "wave1-migrate");
    }

    [Fact]
    public async Task Cancel_returns_ok_for_pending_run()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var startResp = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "3.0.0" });
        startResp.EnsureSuccessStatusCode();
        var start = await startResp.Content.ReadFromJsonAsync<StartUpgradeResponseDto>(JsonOpts);
        Assert.NotNull(start);

        using var cancelResp = await client.PostAsync($"/api/admin/upgrade-runs/{start!.UpgradeRunId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
    }

    [Fact]
    public async Task Start_succeeds_again_after_cancel_of_pending_run()
    {
        var (mgmtDbPath, tenantDbPath) = CreateTempDbPaths();
        await PrepareDatabasesAsync($"Data Source={mgmtDbPath}", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory(mgmtDbPath, tenantDbPath);
        using var client = factory.CreateClient();

        var jwt = await MintAdminJwtAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        using var start1 = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "4.0.0" });
        start1.EnsureSuccessStatusCode();
        var id1 = (await start1.Content.ReadFromJsonAsync<StartUpgradeResponseDto>(JsonOpts))!.UpgradeRunId;

        using var cancel = await client.PostAsync($"/api/admin/upgrade-runs/{id1}/cancel", null);
        cancel.EnsureSuccessStatusCode();

        using var start2 = await client.PostAsJsonAsync("/api/admin/upgrade-runs", new { targetVersion = "4.0.1" });
        Assert.Equal(HttpStatusCode.OK, start2.StatusCode);
        var id2 = (await start2.Content.ReadFromJsonAsync<StartUpgradeResponseDto>(JsonOpts))!.UpgradeRunId;
        Assert.NotEqual(id1, id2);
    }
}
