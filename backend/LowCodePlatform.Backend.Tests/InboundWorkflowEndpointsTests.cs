using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LowCodePlatform.Backend.Controllers;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class InboundWorkflowEndpointsTests
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
    public async Task Inbound_start_run_succeeds_without_jwt_when_secret_matches()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var authClient = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(authClient, "t1");

        using var createResp = await authClient.PostAsJsonAsync("/api/workflows", new
        {
            name = "wf-inbound",
            definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}",
        });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var workflowId = created.GetProperty("workflowDefinitionId").GetGuid();

        const string secret = "0123456789abcdef";
        using var putResp = await authClient.PutAsJsonAsync(
            $"/api/workflows/{workflowId}/inbound-trigger",
            new { secret });
        Assert.Equal(HttpStatusCode.OK, putResp.StatusCode);

        using var inboundClient = CreateTenantClient(factory, "t1");
        inboundClient.DefaultRequestHeaders.Add(InboundWorkflowRunsController.InboundSecretHeaderName, secret);

        using var startResp = await inboundClient.PostAsync($"/api/inbound/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(startPayload.TryGetProperty("workflowRunId", out var runIdProp));
        Assert.NotEqual(Guid.Empty, runIdProp.GetGuid());
    }

    [Fact]
    public async Task Inbound_start_run_returns_403_when_secret_wrong()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var authClient = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(authClient, "t1");

        using var createResp = await authClient.PostAsJsonAsync("/api/workflows", new
        {
            name = "wf-inbound-403",
            definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}",
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var workflowId = created.GetProperty("workflowDefinitionId").GetGuid();

        await authClient.PutAsJsonAsync(
            $"/api/workflows/{workflowId}/inbound-trigger",
            new { secret = "0123456789abcdef" });

        using var inboundClient = CreateTenantClient(factory, "t1");
        inboundClient.DefaultRequestHeaders.Add(InboundWorkflowRunsController.InboundSecretHeaderName, "wrongwrongwrong1");

        using var startResp = await inboundClient.PostAsync($"/api/inbound/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, startResp.StatusCode);
    }

    [Fact]
    public async Task Inbound_start_run_returns_404_when_not_configured()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var authClient = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(authClient, "t1");

        using var createResp = await authClient.PostAsJsonAsync("/api/workflows", new
        {
            name = "wf-no-inbound",
            definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}",
        });
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var workflowId = created.GetProperty("workflowDefinitionId").GetGuid();

        using var inboundClient = CreateTenantClient(factory, "t1");
        inboundClient.DefaultRequestHeaders.Add(InboundWorkflowRunsController.InboundSecretHeaderName, "0123456789abcdef");

        using var startResp = await inboundClient.PostAsync($"/api/inbound/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.NotFound, startResp.StatusCode);
    }
}
