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

public sealed class WorkflowRunEndpointsTests
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
    public async Task Workflow_run_start_and_status_should_work()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        // Create a workflow with two steps: noop + delay
        var createReq = new { name = "wf1", definitionJson = "{\"steps\":[{\"type\":\"noop\"},{\"type\":\"delay\",\"ms\":1}]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        // Start run
        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        // Get run status
        using var getResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var runPayload = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);

        Assert.Equal(runId.ToString(), runPayload!["workflowRunId"]?.ToString());
        Assert.Equal(workflowId.ToString(), runPayload!["workflowDefinitionId"]?.ToString());
        Assert.NotNull(runPayload["state"]);
    }

    [Fact]
    public async Task Domain_command_should_be_able_to_create_entity_record()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        // Create a workflow that creates an entity record via domainCommand
        var definitionJson =
            "{\"steps\":[{\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}]}";
        var createReq = new { name = "wf-domain-create-record", definitionJson = definitionJson };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        // Start run
        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        // Get run details
        using var getResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var runPayload = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("succeeded", runPayload!["state"]?.ToString());

        // Find entity id by listing entities
        using var entitiesResp = await client.GetAsync("/api/entities");
        Assert.Equal(HttpStatusCode.OK, entitiesResp.StatusCode);
        var entitiesPayload = await entitiesResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(entitiesPayload);
        var items = Assert.IsType<System.Text.Json.JsonElement>(entitiesPayload!["items"]);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, items.ValueKind);

        Guid? entityId = null;
        foreach (var it in items.EnumerateArray())
        {
            var name = it.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name == "Company")
            {
                var idStr = it.GetProperty("entityDefinitionId").GetString();
                entityId = Guid.Parse(idStr!);
                break;
            }
        }

        Assert.True(entityId.HasValue);

        // Records list endpoint should have at least 1 record
        using var recordsResp = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp.StatusCode);
        var recordsPayload = await recordsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload);
        var recordsItems = Assert.IsType<System.Text.Json.JsonElement>(recordsPayload!["items"]);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, recordsItems.ValueKind);
        Assert.True(recordsItems.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Domain_command_should_be_able_to_update_entity_record_by_id()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        // Create a record first
        var createJson =
            "{\"steps\":[{\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}]}";
        using var createWfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-domain-create", definitionJson = createJson });
        Assert.Equal(HttpStatusCode.OK, createWfResp.StatusCode);
        var createdWf = await createWfResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(createdWf);
        var createWfId = Guid.Parse(createdWf!["workflowDefinitionId"]!.ToString()!);

        using var createRunResp = await client.PostAsync($"/api/workflows/{createWfId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, createRunResp.StatusCode);
        var createRunPayload = await createRunResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(createRunPayload);
        var createRunId = Guid.Parse(createRunPayload!["workflowRunId"]!.ToString()!);

        using (var createRunGet = await client.GetAsync($"/api/workflows/runs/{createRunId}"))
        {
            Assert.Equal(HttpStatusCode.OK, createRunGet.StatusCode);
            var createRunDetails = await createRunGet.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            Assert.NotNull(createRunDetails);
            Assert.Equal("succeeded", createRunDetails!["state"]?.ToString());
        }

        // Find Company entity id
        using var entitiesResp = await client.GetAsync("/api/entities");
        Assert.Equal(HttpStatusCode.OK, entitiesResp.StatusCode);
        var entitiesPayload = await entitiesResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(entitiesPayload);
        var entitiesItems = Assert.IsType<JsonElement>(entitiesPayload!["items"]);

        Guid? entityId = null;
        foreach (var it in entitiesItems.EnumerateArray())
        {
            var name = it.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name == "Company")
            {
                entityId = Guid.Parse(it.GetProperty("entityDefinitionId").GetString()!);
                break;
            }
        }
        Assert.True(entityId.HasValue);

        // Get record id
        using var recordsResp = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp.StatusCode);
        var recordsPayload = await recordsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload);
        var recordItems = Assert.IsType<JsonElement>(recordsPayload!["items"]);
        Assert.True(recordItems.GetArrayLength() >= 1);

        var firstRecord = recordItems.EnumerateArray().First();
        var recordId = Guid.Parse(firstRecord.GetProperty("entityRecordId").GetString()!);

        // Update record via domainCommand
        var updateJson =
            "{\"steps\":[{\"type\":\"domainCommand\",\"command\":\"entityRecord.updateById\",\"recordId\":\"" + recordId + "\",\"data\":{\"name\":\"Acme Updated\",\"status\":\"inactive\"}}]}";
        using var updateWfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-domain-update", definitionJson = updateJson });
        Assert.Equal(HttpStatusCode.OK, updateWfResp.StatusCode);
        var updateWf = await updateWfResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(updateWf);
        var updateWfId = Guid.Parse(updateWf!["workflowDefinitionId"]!.ToString()!);

        using var updateRunResp = await client.PostAsync($"/api/workflows/{updateWfId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, updateRunResp.StatusCode);
        var updateRunPayload = await updateRunResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(updateRunPayload);
        var updateRunId = Guid.Parse(updateRunPayload!["workflowRunId"]!.ToString()!);

        using (var updateRunGet = await client.GetAsync($"/api/workflows/runs/{updateRunId}"))
        {
            Assert.Equal(HttpStatusCode.OK, updateRunGet.StatusCode);
            var updateRunDetails = await updateRunGet.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            Assert.NotNull(updateRunDetails);
            Assert.Equal("succeeded", updateRunDetails!["state"]?.ToString());
        }

        // Verify record data updated
        using var recordsResp2 = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp2.StatusCode);
        var recordsPayload2 = await recordsResp2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload2);
        var recordItems2 = Assert.IsType<JsonElement>(recordsPayload2!["items"]);

        JsonElement? updated = null;
        foreach (var it in recordItems2.EnumerateArray())
        {
            var idStr = it.GetProperty("entityRecordId").GetString();
            if (idStr == recordId.ToString())
            {
                updated = it;
                break;
            }
        }

        Assert.True(updated.HasValue);
        var dataJson = updated.Value.GetProperty("dataJson").GetString() ?? "{}";
        using var dataDoc = JsonDocument.Parse(dataJson);
        Assert.Equal("Acme Updated", dataDoc.RootElement.GetProperty("name").GetString());
        Assert.Equal("inactive", dataDoc.RootElement.GetProperty("status").GetString());
    }
}
