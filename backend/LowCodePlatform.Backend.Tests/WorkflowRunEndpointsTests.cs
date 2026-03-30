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
    public async Task Workflow_step_timeoutMs_should_fail_step_and_run_with_timeout_error()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var createReq = new
        {
            name = "wf-timeout",
            definitionJson = "{\"steps\":[{\"type\":\"delay\",\"ms\":200,\"timeoutMs\":50}]}"
        };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var wfId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{wfId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var getResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var details = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(details);

        Assert.Equal("failed", details!["state"]?.ToString());
        Assert.Equal("workflow_step_timed_out", details!["errorCode"]?.ToString());

        var stepsJson = JsonSerializer.Serialize(details["steps"]);
        using var stepsDoc = JsonDocument.Parse(stepsJson);
        var step0 = stepsDoc.RootElement[0];
        Assert.Equal("$.timeoutMs", step0.GetProperty("lastErrorConfigPath").GetString());
    }

    [Fact]
    public async Task Foreach_inner_step_timeoutMs_should_fail_run_with_timeout_error()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var createReq = new
        {
            name = "wf-foreach-timeout",
            definitionJson = "{\"steps\":[{\"type\":\"foreach\",\"items\":[1],\"do\":{\"type\":\"delay\",\"ms\":200,\"timeoutMs\":40}}]}"
        };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var wfId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{wfId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var getResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var details = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(details);

        Assert.Equal("failed", details!["state"]?.ToString());
        Assert.Equal("workflow_step_timed_out", details!["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Workflow_run_cancel_api_should_mark_run_canceled_in_db()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var createReq = new
        {
            name = "wf-cancel-api",
            definitionJson = "{\"steps\":[{\"type\":\"delay\",\"ms\":60000}]}"
        };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var wfId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        var startTask = client.PostAsync($"/api/workflows/{wfId}/runs", content: null);

        Guid? runId = null;
        for (var i = 0; i < 200 && runId is null; i++)
        {
            await Task.Delay(15);
            using var listResp = await client.GetAsync($"/api/workflows/{wfId}/runs");
            Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
            var list = await listResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            Assert.NotNull(list);
            var itemsJson = JsonSerializer.Serialize(list!["items"]);
            using var doc = JsonDocument.Parse(itemsJson);
            var itemsArr = doc.RootElement;
            if (itemsArr.GetArrayLength() < 1)
                continue;

            var latest = itemsArr[0];
            var st = latest.GetProperty("state").GetString();
            if (string.Equals(st, "running", StringComparison.OrdinalIgnoreCase))
                runId = Guid.Parse(latest.GetProperty("workflowRunId").GetString()!);
        }

        Assert.NotNull(runId);

        using (var cancelResp = await client.PostAsync($"/api/workflows/runs/{runId}/cancel", content: null))
        {
            Assert.Equal(HttpStatusCode.OK, cancelResp.StatusCode);
        }

        using var completed = await startTask;
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);

        using var getResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var details = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(details);
        Assert.Equal("canceled", details!["state"]?.ToString());
        Assert.Equal("canceled", details!["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Workflow_run_cancel_api_should_return_409_when_run_finished()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf-noop", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var wfId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{wfId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using (var cancelResp = await client.PostAsync($"/api/workflows/runs/{runId}/cancel", content: null))
        {
            Assert.Equal(HttpStatusCode.Conflict, cancelResp.StatusCode);
        }
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
    public async Task Workflow_run_details_should_include_original_and_resolved_step_config()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"output\":{\"a\":\"hello\"}}," +
            "{\"type\":\"noop\",\"msg\":\"${000.a}\"}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-resolved-config", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);

        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);

        var stepsJson = JsonSerializer.Serialize(runPayload!["steps"]);
        using var doc = JsonDocument.Parse(stepsJson);
        var arr = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
        Assert.True(arr.GetArrayLength() >= 2);

        var step1 = arr.EnumerateArray().First(x => x.GetProperty("stepKey").GetString() == "001");
        var original = step1.GetProperty("originalStepConfigJson").GetString();
        var resolved = step1.GetProperty("stepConfigJson").GetString();

        Assert.NotNull(original);
        Assert.NotNull(resolved);
        Assert.Contains("${000.a}", original);
        Assert.DoesNotContain("${000.a}", resolved);
        Assert.Contains("hello", resolved);
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

    [Fact]
    public async Task Require_step_should_allow_workflow_to_continue_when_required_path_exists()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}," +
            "{\"type\":\"require\",\"path\":\"000.entityRecordId\"}," +
            "{\"type\":\"noop\"}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-require-ok", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var getResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var runPayload = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("succeeded", runPayload!["state"]?.ToString());
    }

    [Fact]
    public async Task Require_step_should_fail_workflow_when_required_path_missing()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}," +
            "{\"type\":\"require\",\"path\":\"000.doesNotExist\"}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-require-fail", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var getResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var runPayload = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("failed", runPayload!["state"]?.ToString());
        Assert.Equal("require_failed", runPayload["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Domain_command_should_be_able_to_upsert_entity_record_by_unique_key()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var createJson =
            "{\"steps\":[{" +
            "\"type\":\"domainCommand\",\"command\":\"entityRecord.upsertByEntityName\",\"entityName\":\"Company\",\"uniqueKey\":\"externalId\",\"uniqueValue\":\"c-1\",\"data\":{\"externalId\":\"c-1\",\"name\":\"Acme\",\"status\":\"active\"}}]}";

        using (var wfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-upsert-create", definitionJson = createJson }))
        {
            Assert.Equal(HttpStatusCode.OK, wfResp.StatusCode);
            var wf = await wfResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            Assert.NotNull(wf);
            var wfId = Guid.Parse(wf!["workflowDefinitionId"]!.ToString()!);

            using var runResp = await client.PostAsync($"/api/workflows/{wfId}/runs", content: null);
            Assert.Equal(HttpStatusCode.OK, runResp.StatusCode);
        }

        // Find Company entity id
        using var entitiesResp = await client.GetAsync("/api/entities");
        Assert.Equal(HttpStatusCode.OK, entitiesResp.StatusCode);
        var entitiesPayload = await entitiesResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(entitiesPayload);
        var items = Assert.IsType<JsonElement>(entitiesPayload!["items"]);

        Guid? entityId = null;
        foreach (var it in items.EnumerateArray())
        {
            var name = it.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name == "Company")
            {
                entityId = Guid.Parse(it.GetProperty("entityDefinitionId").GetString()!);
                break;
            }
        }
        Assert.True(entityId.HasValue);

        // Verify 1 record exists
        using var recordsResp = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp.StatusCode);
        var recordsPayload = await recordsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload);
        var recordItems = Assert.IsType<JsonElement>(recordsPayload!["items"]);
        Assert.Equal(1, recordItems.GetArrayLength());
        var recordId = Guid.Parse(recordItems.EnumerateArray().First().GetProperty("entityRecordId").GetString()!);

        var updateJson =
            "{\"steps\":[{" +
            "\"type\":\"domainCommand\",\"command\":\"entityRecord.upsertByEntityName\",\"entityName\":\"Company\",\"uniqueKey\":\"externalId\",\"uniqueValue\":\"c-1\",\"data\":{\"externalId\":\"c-1\",\"name\":\"Acme Updated\",\"status\":\"inactive\"}}]}";

        using (var wfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-upsert-update", definitionJson = updateJson }))
        {
            Assert.Equal(HttpStatusCode.OK, wfResp.StatusCode);
            var wf = await wfResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            Assert.NotNull(wf);
            var wfId = Guid.Parse(wf!["workflowDefinitionId"]!.ToString()!);

            using var runResp = await client.PostAsync($"/api/workflows/{wfId}/runs", content: null);
            Assert.Equal(HttpStatusCode.OK, runResp.StatusCode);
        }

        // Verify still exactly 1 record, and it's the same id, with updated data
        using var recordsResp2 = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp2.StatusCode);
        var recordsPayload2 = await recordsResp2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload2);
        var recordItems2 = Assert.IsType<JsonElement>(recordsPayload2!["items"]);
        Assert.Equal(1, recordItems2.GetArrayLength());

        var only = recordItems2.EnumerateArray().First();
        var recordId2 = Guid.Parse(only.GetProperty("entityRecordId").GetString()!);
        Assert.Equal(recordId, recordId2);

        var dataJson = only.GetProperty("dataJson").GetString() ?? "{}";
        using var doc = JsonDocument.Parse(dataJson);
        Assert.Equal("Acme Updated", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("inactive", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("c-1", doc.RootElement.GetProperty("externalId").GetString());
    }

    [Fact]
    public async Task Context_variable_should_allow_referencing_previous_step_output_in_next_step_config()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}," +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.updateById\",\"recordId\":\"${000.entityRecordId}\",\"data\":{\"name\":\"Acme Updated\",\"status\":\"inactive\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-context-var-update", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        var state = runPayload!["state"]?.ToString();
        if (!string.Equals("succeeded", state, StringComparison.OrdinalIgnoreCase))
        {
            var ec = runPayload["errorCode"]?.ToString();
            var em = runPayload["errorMessage"]?.ToString();
            Assert.Fail($"Expected workflow run to succeed but was '{state}'. errorCode='{ec}' errorMessage='{em}'");
        }

        // Verify record data updated
        using var entitiesResp = await client.GetAsync("/api/entities");
        Assert.Equal(HttpStatusCode.OK, entitiesResp.StatusCode);
        var entitiesPayload = await entitiesResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(entitiesPayload);
        var items = Assert.IsType<JsonElement>(entitiesPayload!["items"]);

        Guid? entityId = null;
        foreach (var it in items.EnumerateArray())
        {
            var name = it.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name == "Company")
            {
                entityId = Guid.Parse(it.GetProperty("entityDefinitionId").GetString()!);
                break;
            }
        }
        Assert.True(entityId.HasValue);

        using var recordsResp = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp.StatusCode);
        var recordsPayload = await recordsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload);
        var recordItems = Assert.IsType<JsonElement>(recordsPayload!["items"]);
        Assert.True(recordItems.GetArrayLength() >= 1);

        var first = recordItems.EnumerateArray().First();
        var dataJson = first.GetProperty("dataJson").GetString() ?? "{}";
        using var doc = JsonDocument.Parse(dataJson);
        Assert.Equal("Acme Updated", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("inactive", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Context_variable_should_fail_fast_when_missing()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"noop\"}," +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.updateById\",\"recordId\":\"${000.entityRecordId}\",\"data\":{\"name\":\"Acme Updated\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-context-var-missing", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("failed", runPayload!["state"]?.ToString());
        Assert.Equal("context_var_not_found", runPayload["errorCode"]?.ToString());
        var msg = runPayload["errorMessage"]?.ToString() ?? string.Empty;
        Assert.Contains("Step '001' config path '$.recordId'", msg);
        Assert.Contains("000.entityRecordId", msg);

        var stepsEl = Assert.IsType<JsonElement>(runPayload["steps"]);
        var step001 = stepsEl.EnumerateArray().Single(s => s.GetProperty("stepKey").GetString() == "001");
        Assert.Equal("$.recordId", step001.GetProperty("lastErrorConfigPath").GetString());
    }

    [Fact]
    public async Task Context_variable_should_fail_fast_when_syntax_is_invalid()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"noop\"}," +
            // Missing closing brace: ${...  (should fail as syntax invalid, not silently pass through)
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.updateById\",\"recordId\":\"${000.entityRecordId\",\"data\":{\"name\":\"Acme Updated\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-context-var-syntax-invalid", definitionJson });
        Assert.Equal(HttpStatusCode.BadRequest, createResp.StatusCode);
        var createPayload = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(createPayload);
        Assert.Equal("context_var_syntax_invalid", createPayload!["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Set_step_should_seed_context_and_be_usable_via_context_vars()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        // 1) Create record
        var createJson =
            "{\"steps\":[{" +
            "\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}]}";

        using var createWfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-set-seed-create", definitionJson = createJson });
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
        var items = Assert.IsType<JsonElement>(entitiesPayload!["items"]);

        Guid? entityId = null;
        foreach (var it in items.EnumerateArray())
        {
            var name = it.TryGetProperty("name", out var n) ? n.GetString() : null;
            if (name == "Company")
            {
                entityId = Guid.Parse(it.GetProperty("entityDefinitionId").GetString()!);
                break;
            }
        }
        Assert.True(entityId.HasValue);

        using var recordsResp = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp.StatusCode);
        var recordsPayload = await recordsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload);
        var recordItems = Assert.IsType<JsonElement>(recordsPayload!["items"]);
        var first = recordItems.EnumerateArray().First();
        var recordId = first.GetProperty("entityRecordId").GetString()!;

        // 2) Update record using a 'set' step to seed recordId into context
        var updateJson =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"output\":{\"recordId\":\"" + recordId + "\"}}," +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.updateById\",\"recordId\":\"${000.recordId}\",\"data\":{\"name\":\"Acme Updated\",\"status\":\"inactive\"}}" +
            "]}";

        using var updateWfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-set-seed-update", definitionJson = updateJson });
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

        using var recordsResp2 = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp2.StatusCode);
        var recordsPayload2 = await recordsResp2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload2);
        var recordItems2 = Assert.IsType<JsonElement>(recordsPayload2!["items"]);

        JsonElement? updated = null;
        foreach (var it in recordItems2.EnumerateArray())
        {
            var idStr = it.GetProperty("entityRecordId").GetString();
            if (idStr == recordId)
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

    [Fact]
    public async Task Set_step_should_fail_when_outputJson_is_invalid_json()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"outputJson\":\"{not json}\"}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-set-invalid", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("failed", runPayload!["state"]?.ToString());
        Assert.Equal("set_output_json_invalid", runPayload["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Domain_command_should_be_able_to_delete_entity_record_by_id()
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
        using var createWfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-domain-create-for-delete", definitionJson = createJson });
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

        // Delete record via domainCommand
        var deleteJson =
            "{\"steps\":[{\"type\":\"domainCommand\",\"command\":\"entityRecord.deleteById\",\"recordId\":\"" + recordId + "\"}]}";
        using var deleteWfResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-domain-delete", definitionJson = deleteJson });
        Assert.Equal(HttpStatusCode.OK, deleteWfResp.StatusCode);
        var deleteWf = await deleteWfResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(deleteWf);
        var deleteWfId = Guid.Parse(deleteWf!["workflowDefinitionId"]!.ToString()!);

        using var deleteRunResp = await client.PostAsync($"/api/workflows/{deleteWfId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, deleteRunResp.StatusCode);
        var deleteRunPayload = await deleteRunResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(deleteRunPayload);
        var deleteRunId = Guid.Parse(deleteRunPayload!["workflowRunId"]!.ToString()!);

        using (var deleteRunGet = await client.GetAsync($"/api/workflows/runs/{deleteRunId}"))
        {
            Assert.Equal(HttpStatusCode.OK, deleteRunGet.StatusCode);
            var deleteRunDetails = await deleteRunGet.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            Assert.NotNull(deleteRunDetails);
            Assert.Equal("succeeded", deleteRunDetails!["state"]?.ToString());
        }

        // Records list endpoint should now be empty
        using var recordsResp2 = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp2.StatusCode);
        var recordsPayload2 = await recordsResp2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload2);
        var recordItems2 = Assert.IsType<JsonElement>(recordsPayload2!["items"]);
        Assert.Equal(0, recordItems2.GetArrayLength());
    }

    [Fact]
    public async Task Map_step_should_project_context_values_and_be_usable_via_context_vars()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        // Create a workflow: create record -> map output -> update using ${...}
        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}," +
            "{\"type\":\"map\",\"mappings\":{\"recordId\":\"000.entityRecordId\"}}," +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.updateById\",\"recordId\":\"${001.recordId}\",\"data\":{\"name\":\"Acme Updated\",\"status\":\"inactive\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-map-projection", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        var state = runPayload!["state"]?.ToString();
        if (!string.Equals("succeeded", state, StringComparison.OrdinalIgnoreCase))
        {
            var ec = runPayload["errorCode"]?.ToString();
            var em = runPayload["errorMessage"]?.ToString();
            string? stepsDebug = null;
            try
            {
                if (runPayload.TryGetValue("steps", out var stepsObj) && stepsObj is JsonElement stepsArrEl && stepsArrEl.ValueKind == JsonValueKind.Array)
                {
                    var lines = new List<string>();
                    foreach (var s in stepsArrEl.EnumerateArray())
                    {
                        var stepKey = s.TryGetProperty("stepKey", out var sk) ? sk.GetString() : null;
                        var stepType = s.TryGetProperty("stepType", out var st) ? st.GetString() : null;
                        var stepState = s.TryGetProperty("state", out var ss) ? ss.GetString() : null;
                        var stepEc = s.TryGetProperty("lastErrorCode", out var sec) ? sec.GetString() : null;
                        var stepEm = s.TryGetProperty("lastErrorMessage", out var sem) ? sem.GetString() : null;
                        lines.Add($"stepKey={stepKey} stepType={stepType} state={stepState} lastErrorCode={stepEc} lastErrorMessage={stepEm}");
                    }

                    stepsDebug = string.Join("\n", lines);
                }
            }
            catch
            {
                // ignore
            }

            Assert.Fail($"Expected workflow run to succeed but was '{state}'. errorCode='{ec}' errorMessage='{em}'\nsteps:\n{stepsDebug}");
        }

        // Verify record data updated
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

        using var recordsResp = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp.StatusCode);
        var recordsPayload = await recordsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload);
        var recordItems = Assert.IsType<JsonElement>(recordsPayload!["items"]);
        Assert.True(recordItems.GetArrayLength() >= 1);
        var first = recordItems.EnumerateArray().First();
        var dataJson = first.GetProperty("dataJson").GetString() ?? "{}";
        using var dataDoc = JsonDocument.Parse(dataJson);
        Assert.Equal("Acme Updated", dataDoc.RootElement.GetProperty("name").GetString());
        Assert.Equal("inactive", dataDoc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Map_step_should_fail_fast_when_source_path_is_missing()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"map\",\"mappings\":{\"x\":\"000.doesNotExist\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-map-missing", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("failed", runPayload!["state"]?.ToString());
        Assert.Equal("map_source_not_found", runPayload["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Merge_step_should_combine_sources_and_be_usable_via_context_vars()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        // create -> map (recordId) -> merge (inline + map output) -> update using ${...}
        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.createByEntityName\",\"entityName\":\"Company\",\"data\":{\"name\":\"Acme Ltd\",\"status\":\"active\"}}," +
            "{\"type\":\"map\",\"mappings\":{\"recordId\":\"000.entityRecordId\"}}," +
            "{\"type\":\"merge\",\"sources\":[{\"note\":\"hello\"},\"001\"]}," +
            "{\"type\":\"domainCommand\",\"command\":\"entityRecord.updateById\",\"recordId\":\"${002.recordId}\",\"data\":{\"name\":\"Acme Updated\",\"status\":\"inactive\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-merge-sources", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        var state = runPayload!["state"]?.ToString();
        if (!string.Equals("succeeded", state, StringComparison.OrdinalIgnoreCase))
        {
            var ec = runPayload["errorCode"]?.ToString();
            var em = runPayload["errorMessage"]?.ToString();
            Assert.Fail($"Expected workflow run to succeed but was '{state}'. errorCode='{ec}' errorMessage='{em}'");
        }

        // Verify record data updated
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

        using var recordsResp = await client.GetAsync($"/api/entities/{entityId.Value}/records");
        Assert.Equal(HttpStatusCode.OK, recordsResp.StatusCode);
        var recordsPayload = await recordsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(recordsPayload);
        var recordItems = Assert.IsType<JsonElement>(recordsPayload!["items"]);
        Assert.True(recordItems.GetArrayLength() >= 1);
        var first = recordItems.EnumerateArray().First();
        var dataJson = first.GetProperty("dataJson").GetString() ?? "{}";
        using var dataDoc = JsonDocument.Parse(dataJson);
        Assert.Equal("Acme Updated", dataDoc.RootElement.GetProperty("name").GetString());
        Assert.Equal("inactive", dataDoc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Merge_step_should_fail_fast_when_source_path_is_missing()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"merge\",\"sources\":[\"000\"]}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-merge-missing", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        var state = runPayload!["state"]?.ToString();
        if (!string.Equals("failed", state, StringComparison.OrdinalIgnoreCase))
        {
            var ec = runPayload["errorCode"]?.ToString();
            var em = runPayload["errorMessage"]?.ToString();
            Assert.Fail($"Expected workflow run to fail but was '{state}'. errorCode='{ec}' errorMessage='{em}'");
        }

        Assert.Equal("merge_source_not_found", runPayload["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Foreach_step_should_iterate_array_and_expose_item_context()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"output\":{\"items\":[{\"n\":1},{\"n\":2}]}} ," +
            "{\"type\":\"foreach\",\"items\":\"000.items\",\"do\":{\"type\":\"map\",\"mappings\":{\"n\":\"item.n\"}}}," +
            "{\"type\":\"map\",\"mappings\":{\"first\":\"001.0.n\",\"second\":\"001.1.n\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-foreach-items", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        var state = runPayload!["state"]?.ToString();
        if (!string.Equals("succeeded", state, StringComparison.OrdinalIgnoreCase))
        {
            var ec = runPayload["errorCode"]?.ToString();
            var em = runPayload["errorMessage"]?.ToString();

            string? stepsDebug = null;
            try
            {
                if (runPayload.TryGetValue("steps", out var stepsObj) && stepsObj is JsonElement stepsArrEl && stepsArrEl.ValueKind == JsonValueKind.Array)
                {
                    var lines = new List<string>();
                    foreach (var s in stepsArrEl.EnumerateArray())
                    {
                        var stepKey = s.TryGetProperty("stepKey", out var sk) ? sk.GetString() : null;
                        var stepType = s.TryGetProperty("stepType", out var st) ? st.GetString() : null;
                        var stepState = s.TryGetProperty("state", out var ss) ? ss.GetString() : null;
                        var stepEc = s.TryGetProperty("lastErrorCode", out var sec) ? sec.GetString() : null;
                        var stepEm = s.TryGetProperty("lastErrorMessage", out var sem) ? sem.GetString() : null;
                        lines.Add($"stepKey={stepKey} stepType={stepType} state={stepState} lastErrorCode={stepEc} lastErrorMessage={stepEm}");
                    }

                    stepsDebug = string.Join("\n", lines);
                }
            }
            catch
            {
                // ignore
            }

            Assert.Fail($"Expected workflow run to succeed but was '{state}'. errorCode='{ec}' errorMessage='{em}'\nsteps:\n{stepsDebug}");
        }

        var stepsEl = Assert.IsType<JsonElement>(runPayload["steps"]);
        var step002 = stepsEl.EnumerateArray().First(x => x.GetProperty("stepKey").GetString() == "002");
        var outputJson = step002.GetProperty("outputJson").GetString() ?? "{}";
        using var doc = JsonDocument.Parse(outputJson);
        Assert.Equal(1, doc.RootElement.GetProperty("first").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("second").GetInt32());
    }

    [Fact]
    public async Task Foreach_step_should_fail_fast_when_items_path_is_missing()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"foreach\",\"items\":\"000.items\",\"do\":{\"type\":\"noop\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-foreach-missing", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("failed", runPayload!["state"]?.ToString());
        Assert.Equal("foreach_items_not_found", runPayload["errorCode"]?.ToString());
    }

    [Fact]
    public async Task Switch_step_should_execute_matching_case_branch()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"output\":{\"kind\":\"a\"}}," +
            "{\"type\":\"switch\",\"value\":\"000.kind\",\"cases\":[" +
            "{\"when\":\"a\",\"do\":{\"type\":\"set\",\"output\":{\"result\":1}}}," +
            "{\"when\":\"b\",\"do\":{\"type\":\"set\",\"output\":{\"result\":2}}}" +
            "]}," +
            "{\"type\":\"map\",\"mappings\":{\"result\":\"001.result\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-switch-match", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("succeeded", runPayload!["state"]?.ToString());

        var stepsEl = Assert.IsType<JsonElement>(runPayload["steps"]);
        var step002 = stepsEl.EnumerateArray().First(x => x.GetProperty("stepKey").GetString() == "002");
        var outputJson = step002.GetProperty("outputJson").GetString() ?? "{}";
        using var doc = JsonDocument.Parse(outputJson);
        Assert.Equal(1, doc.RootElement.GetProperty("result").GetInt32());
    }

    [Fact]
    public async Task Switch_step_should_execute_default_branch_when_no_case_matches()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"output\":{\"kind\":\"c\"}}," +
            "{\"type\":\"switch\",\"value\":\"000.kind\",\"cases\":[" +
            "{\"when\":\"a\",\"do\":{\"type\":\"set\",\"output\":{\"result\":1}}}" +
            "],\"default\":{\"type\":\"set\",\"output\":{\"result\":99}}}," +
            "{\"type\":\"map\",\"mappings\":{\"result\":\"001.result\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-switch-default", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("succeeded", runPayload!["state"]?.ToString());

        var stepsEl = Assert.IsType<JsonElement>(runPayload["steps"]);
        var step002 = stepsEl.EnumerateArray().First(x => x.GetProperty("stepKey").GetString() == "002");
        var outputJson = step002.GetProperty("outputJson").GetString() ?? "{}";
        using var doc = JsonDocument.Parse(outputJson);
        Assert.Equal(99, doc.RootElement.GetProperty("result").GetInt32());
    }

    [Fact]
    public async Task Step_retry_should_succeed_after_transient_failures()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"unstable\",\"failTimes\":2,\"retry\":{\"maxAttempts\":3,\"delayMs\":0},\"output\":{\"ok\":true}}," +
            "{\"type\":\"map\",\"mappings\":{\"ok\":\"000.ok\"}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-retry-success", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("succeeded", runPayload!["state"]?.ToString());

        var stepsEl = Assert.IsType<JsonElement>(runPayload["steps"]);
        var step000 = stepsEl.EnumerateArray().First(x => x.GetProperty("stepKey").GetString() == "000");
        Assert.Equal(3, step000.GetProperty("attempt").GetInt32());

        var step001 = stepsEl.EnumerateArray().First(x => x.GetProperty("stepKey").GetString() == "001");
        var outputJson = step001.GetProperty("outputJson").GetString() ?? "{}";
        using var doc = JsonDocument.Parse(outputJson);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task Step_retry_should_fail_when_max_attempts_is_exhausted()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        await AuthenticateAsync(client, "t1");

        var definitionJson =
            "{\"steps\":[" +
            "{\"type\":\"unstable\",\"failTimes\":5,\"retry\":{\"maxAttempts\":3,\"delayMs\":0}}" +
            "]}";

        using var createResp = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-retry-fail", definitionJson });
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var workflowId = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var startResp = await client.PostAsync($"/api/workflows/{workflowId}/runs", content: null);
        Assert.Equal(HttpStatusCode.OK, startResp.StatusCode);
        var startPayload = await startResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(startPayload);
        var runId = Guid.Parse(startPayload!["workflowRunId"]!.ToString()!);

        using var runDetailsResp = await client.GetAsync($"/api/workflows/runs/{runId}");
        Assert.Equal(HttpStatusCode.OK, runDetailsResp.StatusCode);
        var runPayload = await runDetailsResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(runPayload);
        Assert.Equal("failed", runPayload!["state"]?.ToString());

        var stepsEl = Assert.IsType<JsonElement>(runPayload["steps"]);
        var step000 = stepsEl.EnumerateArray().First(x => x.GetProperty("stepKey").GetString() == "000");
        Assert.Equal(3, step000.GetProperty("attempt").GetInt32());
        Assert.Equal("unstable_failed", step000.GetProperty("lastErrorCode").GetString());
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_returns_items_with_workflow_name_and_total_count()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var c1 = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-list-a", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
        Assert.Equal(HttpStatusCode.OK, c1.StatusCode);
        var w1 = await c1.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var id1 = Guid.Parse(w1!["workflowDefinitionId"]!.ToString()!);

        using var c2 = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-list-b", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
        Assert.Equal(HttpStatusCode.OK, c2.StatusCode);
        var w2 = await c2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var id2 = Guid.Parse(w2!["workflowDefinitionId"]!.ToString()!);

        using var s1 = await client.PostAsync($"/api/workflows/{id1}/runs", null);
        using var s2 = await client.PostAsync($"/api/workflows/{id2}/runs", null);
        Assert.Equal(HttpStatusCode.OK, s1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, s2.StatusCode);

        using var listResp = await client.GetAsync("/api/workflows/runs?take=20&skip=0");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 2);
        var items = doc.RootElement.GetProperty("items");
        var names = new List<string>();
        foreach (var el in items.EnumerateArray())
            names.Add(el.GetProperty("workflowName").GetString()!);
        Assert.Contains("wf-list-a", names);
        Assert.Contains("wf-list-b", names);
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_invalid_take_returns_400()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var bad = await client.GetAsync("/api/workflows/runs?take=0");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_invalid_state_returns_400()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var bad = await client.GetAsync("/api/workflows/runs?state=not_a_valid_state");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_filters_by_workflow_definition_id()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var c1 = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-filter-a", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
        Assert.Equal(HttpStatusCode.OK, c1.StatusCode);
        var w1 = await c1.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var id1 = Guid.Parse(w1!["workflowDefinitionId"]!.ToString()!);

        using var c2 = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-filter-b", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
        Assert.Equal(HttpStatusCode.OK, c2.StatusCode);
        var w2 = await c2.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var id2 = Guid.Parse(w2!["workflowDefinitionId"]!.ToString()!);

        using (var s1 = await client.PostAsync($"/api/workflows/{id1}/runs", null))
            Assert.Equal(HttpStatusCode.OK, s1.StatusCode);
        using (var s2 = await client.PostAsync($"/api/workflows/{id2}/runs", null))
            Assert.Equal(HttpStatusCode.OK, s2.StatusCode);

        using var listResp = await client.GetAsync($"/api/workflows/runs?take=50&skip=0&workflowDefinitionId={id1}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("totalCount").GetInt32());
        var items = doc.RootElement.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(id1.ToString(), items[0].GetProperty("workflowDefinitionId").GetString());
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_pagination_skip_take()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        for (var i = 0; i < 3; i++)
        {
            using var c = await client.PostAsJsonAsync("/api/workflows", new { name = $"wf-page-{i}", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
            Assert.Equal(HttpStatusCode.OK, c.StatusCode);
            var w = await c.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
            var id = Guid.Parse(w!["workflowDefinitionId"]!.ToString()!);
            using var s = await client.PostAsync($"/api/workflows/{id}/runs", null);
            Assert.Equal(HttpStatusCode.OK, s.StatusCode);
        }

        using var p1 = await client.GetAsync("/api/workflows/runs?take=2&skip=0");
        Assert.Equal(HttpStatusCode.OK, p1.StatusCode);
        var j1 = await p1.Content.ReadAsStringAsync();
        using var d1 = JsonDocument.Parse(j1);
        Assert.Equal(3, d1.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, d1.RootElement.GetProperty("items").GetArrayLength());

        using var p2 = await client.GetAsync("/api/workflows/runs?take=2&skip=2");
        Assert.Equal(HttpStatusCode.OK, p2.StatusCode);
        var j2 = await p2.Content.ReadAsStringAsync();
        using var d2 = JsonDocument.Parse(j2);
        Assert.Equal(3, d2.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, d2.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_started_after_utc_future_returns_empty()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var c = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-future", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var w = await c.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var id = Guid.Parse(w!["workflowDefinitionId"]!.ToString()!);
        using (var s = await client.PostAsync($"/api/workflows/{id}/runs", null))
            Assert.Equal(HttpStatusCode.OK, s.StatusCode);

        var future = DateTime.UtcNow.AddYears(1);
        var q = Uri.EscapeDataString(future.ToString("o"));
        using var listResp = await client.GetAsync($"/api/workflows/runs?take=50&skip=0&startedAfterUtc={q}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(0, doc.RootElement.GetProperty("totalCount").GetInt32());
        Assert.Equal(0, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_state_succeeded_returns_only_succeeded_runs()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var c = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-succ", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var w = await c.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var id = Guid.Parse(w!["workflowDefinitionId"]!.ToString()!);
        using (var s = await client.PostAsync($"/api/workflows/{id}/runs", null))
            Assert.Equal(HttpStatusCode.OK, s.StatusCode);

        using var listResp = await client.GetAsync("/api/workflows/runs?take=50&skip=0&state=succeeded");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
        foreach (var el in doc.RootElement.GetProperty("items").EnumerateArray())
            Assert.Equal("succeeded", el.GetProperty("state").GetString());
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_started_after_utc_without_z_returns_400_when_kind_not_utc()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        // No timezone suffix — model binder typically yields DateTimeKind.Unspecified, which the API rejects.
        using var bad = await client.GetAsync("/api/workflows/runs?take=50&skip=0&startedAfterUtc=2020-01-01T00:00:00");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_started_before_utc_without_z_returns_400_when_kind_not_utc()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var bad = await client.GetAsync("/api/workflows/runs?take=50&skip=0&startedBeforeUtc=2030-01-01T00:00:00");
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [Fact]
    public async Task Tenant_wide_workflow_runs_list_started_before_utc_inclusive_with_z_returns_matching_runs()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        await InitializeDatabasesAsync($"Data Source={mgmtDbPath}", "t1", $"Data Source={tenantDbPath}", CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        using var c = await client.PostAsJsonAsync("/api/workflows", new { name = "wf-before", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" });
        Assert.Equal(HttpStatusCode.OK, c.StatusCode);
        var w = await c.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var id = Guid.Parse(w!["workflowDefinitionId"]!.ToString()!);
        using (var s = await client.PostAsync($"/api/workflows/{id}/runs", null))
            Assert.Equal(HttpStatusCode.OK, s.StatusCode);

        var before = DateTime.UtcNow.AddYears(1);
        var q = Uri.EscapeDataString(before.ToString("o"));
        using var listResp = await client.GetAsync($"/api/workflows/runs?take=50&skip=0&startedBeforeUtc={q}");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);
        var json = await listResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("totalCount").GetInt32() >= 1);
    }
}
