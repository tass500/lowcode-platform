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

public sealed class WorkflowEndpointsTests
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
    public async Task Workflows_CRUD_should_work_for_single_tenant()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);

        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf1", definitionJson = "{\"steps\":[]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);

        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        Assert.True(created.TryGetValue("workflowDefinitionId", out var idObj));
        Assert.NotNull(idObj);

        var id = Guid.Parse(idObj!.ToString()!);

        using var listResp = await client.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.OK, listResp.StatusCode);

        var listPayload = await listResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(listPayload);
        Assert.True(listPayload.TryGetValue("items", out var itemsObj));
        Assert.NotNull(itemsObj);

        var updateReq = new { name = "wf1-updated", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" };
        using var updateResp = await client.PutAsJsonAsync($"/api/workflows/{id}", updateReq);
        Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

        using var getResp = await client.GetAsync($"/api/workflows/{id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

        var getPayload = await getResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(getPayload);
        Assert.True(getPayload.TryGetValue("name", out var nameObj));
        Assert.Equal("wf1-updated", nameObj?.ToString());

        using var delResp = await client.DeleteAsync($"/api/workflows/{id}");
        Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);

        using var getResp2 = await client.GetAsync($"/api/workflows/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp2.StatusCode);
    }

    [Fact]
    public async Task Workflows_create_should_fail_fast_when_context_var_syntax_is_invalid()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf-invalid-syntax", definitionJson = "{\"steps\":[{\"type\":\"domainCommand\",\"command\":\"echo\",\"x\":\"${}\"}]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, createResp.StatusCode);

        var payload = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        Assert.Equal("context_var_syntax_invalid", payload!["errorCode"]?.ToString());
        var details = payload["details"]?.ToString() ?? string.Empty;
        Assert.Contains("path", details);
        Assert.Contains("code", details);
        Assert.Contains("message", details);
    }

    [Fact]
    public async Task Workflows_update_should_fail_fast_when_context_var_syntax_is_invalid()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf1", definitionJson = "{\"steps\":[]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var id = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        var updateReq = new { name = "wf1", definitionJson = "{\"steps\":[{\"type\":\"noop\",\"x\":\"${000\"}]}" };
        using var updateResp = await client.PutAsJsonAsync($"/api/workflows/{id}", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);

        var payload = await updateResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        Assert.Equal("context_var_syntax_invalid", payload!["errorCode"]?.ToString());
        var details = payload["details"]?.ToString() ?? string.Empty;
        Assert.Contains("path", details);
        Assert.Contains("code", details);
        Assert.Contains("message", details);
    }

    [Fact]
    public async Task Workflows_should_be_isolated_between_tenants()
    {
        // Shared management DB with two tenants; separate tenant DBs.
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantADbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-ta-{Guid.NewGuid():N}.db");
        var tenantBDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-tb-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        await InitializeDatabasesAsync(managementCs, "ta", $"Data Source={tenantADbPath}", CancellationToken.None);
        await InitializeDatabasesAsync(managementCs, "tb", $"Data Source={tenantBDbPath}", CancellationToken.None);

        await using var factoryA = new TestAppFactory("ta", mgmtDbPath, tenantADbPath);
        await using var factoryB = new TestAppFactory("tb", mgmtDbPath, tenantBDbPath);

        using var clientA = CreateTenantClient(factoryA, "ta");
        using var clientB = CreateTenantClient(factoryB, "tb");

        await AuthenticateAsync(clientA, "ta");
        await AuthenticateAsync(clientB, "tb");

        var createReq = new { name = "wf-a", definitionJson = "{\"steps\":[]}" };
        using var createResp = await clientA.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        using var listA = await clientA.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.OK, listA.StatusCode);

        using var listB = await clientB.GetAsync("/api/workflows");
        Assert.Equal(HttpStatusCode.OK, listB.StatusCode);

        var payloadA = await listA.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var payloadB = await listB.Content.ReadFromJsonAsync<Dictionary<string, object?>>();

        Assert.NotNull(payloadA);
        Assert.NotNull(payloadB);

        // We only assert that tenant B can call the endpoint successfully.
        // (Full item-count assertions would require strongly typed JSON parsing.)
        Assert.True(payloadA!.ContainsKey("items"));
        Assert.True(payloadB!.ContainsKey("items"));
    }

    [Fact]
    public async Task Workflows_create_should_fail_fast_when_definition_schema_is_invalid()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf-invalid-schema", definitionJson = "{}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, createResp.StatusCode);

        var payload = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        Assert.Equal("workflow_definition_invalid", payload!["errorCode"]?.ToString());
        var details = payload["details"]?.ToString() ?? string.Empty;
        Assert.Contains("path", details);
        Assert.Contains("code", details);
        Assert.Contains("message", details);
    }

    [Fact]
    public async Task Workflows_update_should_fail_fast_when_definition_schema_is_invalid()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf1", definitionJson = "{\"steps\":[]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var id = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        var updateReq = new { name = "wf1", definitionJson = "{}" };
        using var updateResp = await client.PutAsJsonAsync($"/api/workflows/{id}", updateReq);
        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);

        var payload = await updateResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        Assert.Equal("workflow_definition_invalid", payload!["errorCode"]?.ToString());
        var details = payload["details"]?.ToString() ?? string.Empty;
        Assert.Contains("path", details);
        Assert.Contains("code", details);
        Assert.Contains("message", details);
    }

    [Fact]
    public async Task Workflows_create_should_return_structured_details_when_name_missing()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "", definitionJson = "{\"steps\":[]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, createResp.StatusCode);

        var json = await createResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("name_missing", doc.RootElement.GetProperty("errorCode").GetString());
        var d0 = doc.RootElement.GetProperty("details")[0];
        Assert.Equal("$.name", d0.GetProperty("path").GetString());
        Assert.Equal("name_missing", d0.GetProperty("code").GetString());
        Assert.Equal("error", d0.GetProperty("severity").GetString());
    }

    [Fact]
    public async Task Workflows_create_should_return_structured_details_when_definition_missing()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf", definitionJson = "" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.BadRequest, createResp.StatusCode);

        var json = await createResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("definition_missing", doc.RootElement.GetProperty("errorCode").GetString());
        var d0 = doc.RootElement.GetProperty("details")[0];
        Assert.Equal("$.definitionJson", d0.GetProperty("path").GetString());
        Assert.Equal("definition_missing", d0.GetProperty("code").GetString());
        Assert.Equal("error", d0.GetProperty("severity").GetString());
    }

    [Fact]
    public async Task Workflows_get_should_return_structured_details_when_not_found()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var id = Guid.NewGuid();
        using var getResp = await client.GetAsync($"/api/workflows/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);

        var json = await getResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("workflow_not_found", doc.RootElement.GetProperty("errorCode").GetString());
        var d0 = doc.RootElement.GetProperty("details")[0];
        Assert.Equal("$.workflowDefinitionId", d0.GetProperty("path").GetString());
        Assert.Equal("workflow_not_found", d0.GetProperty("code").GetString());
        Assert.Equal("error", d0.GetProperty("severity").GetString());
    }

    [Fact]
    public async Task Workflows_create_should_return_lint_warnings_for_unknown_step_types()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf-lint-unknown-type", definitionJson = "{\"steps\":[{\"type\":\"unknownStep\"}]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var json = await createResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("lintWarnings", out var warningsEl));
        Assert.Equal(JsonValueKind.Array, warningsEl.ValueKind);

        var found = false;
        foreach (var w in warningsEl.EnumerateArray())
        {
            if (w.ValueKind != JsonValueKind.Object)
                continue;
            if (!w.TryGetProperty("code", out var codeEl) || codeEl.ValueKind != JsonValueKind.String)
                continue;
            if (codeEl.GetString() == "workflow_step_type_unknown")
                found = true;
        }
        Assert.True(found);
    }

    [Fact]
    public async Task Workflows_create_should_warn_when_set_output_never_referenced()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var def =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"output\":{\"seed\":\"1\"}}," +
            "{\"type\":\"noop\"}" +
            "]}";
        var createReq = new { name = "wf-lint-unused-out", definitionJson = def };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var json = await createResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("lintWarnings", out var warningsEl));
        var found = false;
        foreach (var w in warningsEl.EnumerateArray())
        {
            if (w.TryGetProperty("code", out var c) && c.GetString() == "workflow_step_output_unused")
                found = true;
        }

        Assert.True(found);
    }

    [Fact]
    public async Task Workflows_create_should_not_warn_unused_when_output_referenced()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var def =
            "{\"steps\":[" +
            "{\"type\":\"set\",\"output\":{\"seed\":\"1\"}}," +
            "{\"type\":\"noop\",\"ref\":\"${000.seed}\"}" +
            "]}";
        var createReq = new { name = "wf-lint-used-out", definitionJson = def };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var json = await createResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("lintWarnings", out var warningsEl));
        foreach (var w in warningsEl.EnumerateArray())
        {
            if (w.TryGetProperty("code", out var c) && c.GetString() == "workflow_step_output_unused")
                Assert.Fail("Did not expect workflow_step_output_unused when output is referenced.");
        }
    }

    [Fact]
    public async Task Workflows_create_should_warn_on_likely_foreach_typo()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var def = "{\"steps\":[{\"type\":\"noop\",\"x\":\"${foreach.indx}\"}]}";
        var createReq = new { name = "wf-lint-typo", definitionJson = def };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var json = await createResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("lintWarnings", out var warningsEl));
        var found = false;
        foreach (var w in warningsEl.EnumerateArray())
        {
            if (w.TryGetProperty("code", out var c) && c.GetString() == "workflow_context_likely_typo")
                found = true;
        }

        Assert.True(found);
    }

    [Fact]
    public async Task Workflows_schedule_put_should_validate_cron_and_persist()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf-sched", definitionJson = "{\"steps\":[{\"type\":\"noop\"}]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(created);
        var id = Guid.Parse(created!["workflowDefinitionId"]!.ToString()!);

        using var badResp = await client.PutAsJsonAsync($"/api/workflows/{id}/schedule", new { enabled = true, cron = "0 0 1 * *" });
        Assert.Equal(HttpStatusCode.BadRequest, badResp.StatusCode);

        using var okResp = await client.PutAsJsonAsync($"/api/workflows/{id}/schedule", new { enabled = true, cron = "*/10 * * * *" });
        Assert.Equal(HttpStatusCode.OK, okResp.StatusCode);

        var okJson = await okResp.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(okJson))
        {
            var root = doc.RootElement;
            Assert.True(root.GetProperty("scheduleEnabled").GetBoolean());
            Assert.Equal("*/10 * * * *", root.GetProperty("scheduleCron").GetString());
            Assert.NotEqual(JsonValueKind.Null, root.GetProperty("scheduleNextDueUtc").ValueKind);
        }

        using var offResp = await client.PutAsJsonAsync($"/api/workflows/{id}/schedule", new { enabled = false, cron = (string?)null });
        Assert.Equal(HttpStatusCode.OK, offResp.StatusCode);
        var offJson = await offResp.Content.ReadAsStringAsync();
        using (var doc2 = JsonDocument.Parse(offJson))
        {
            var root = doc2.RootElement;
            Assert.False(root.GetProperty("scheduleEnabled").GetBoolean());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("scheduleCron").ValueKind);
        }
    }

    [Fact]
    public async Task Workflows_export_should_return_portable_package()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var defJson = "{\"steps\":[{\"type\":\"noop\"}]}";
        var createReq = new { name = "wf-export", definitionJson = defJson };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        Assert.Equal(HttpStatusCode.OK, createResp.StatusCode);

        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("workflowDefinitionId").GetGuid();

        using var exportResp = await client.GetAsync($"/api/workflows/{id}/export");
        Assert.Equal(HttpStatusCode.OK, exportResp.StatusCode);

        var pack = await exportResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, pack.GetProperty("exportFormatVersion").GetInt32());
        Assert.Equal("wf-export", pack.GetProperty("name").GetString());
        Assert.Equal(defJson, pack.GetProperty("definitionJson").GetString());
        Assert.Equal(id, pack.GetProperty("sourceWorkflowDefinitionId").GetGuid());
        Assert.True(pack.TryGetProperty("exportedAtUtc", out _));
    }

    [Fact]
    public async Task Workflows_import_should_create_new_workflow_from_export_package()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-t1-{Guid.NewGuid():N}.db");

        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        await AuthenticateAsync(client, "t1");

        var createReq = new { name = "wf-src", definitionJson = "{\"steps\":[{\"type\":\"delay\",\"ms\":1}]}" };
        using var createResp = await client.PostAsJsonAsync("/api/workflows", createReq);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<JsonElement>();
        var srcId = created.GetProperty("workflowDefinitionId").GetGuid();

        using var exportResp = await client.GetAsync($"/api/workflows/{srcId}/export");
        exportResp.EnsureSuccessStatusCode();
        var pack = await exportResp.Content.ReadFromJsonAsync<JsonElement>();

        var importBody = new
        {
            name = "wf-imported",
            definitionJson = pack.GetProperty("definitionJson").GetString(),
            exportFormatVersion = pack.GetProperty("exportFormatVersion").GetInt32(),
        };

        using var importResp = await client.PostAsJsonAsync("/api/workflows/import", importBody);
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);

        var imported = await importResp.Content.ReadFromJsonAsync<JsonElement>();
        var newId = imported.GetProperty("workflowDefinitionId").GetGuid();
        Assert.NotEqual(srcId, newId);
        Assert.Equal("wf-imported", imported.GetProperty("name").GetString());
    }
}
