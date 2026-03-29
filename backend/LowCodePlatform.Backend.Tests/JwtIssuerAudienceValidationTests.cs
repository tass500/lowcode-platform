using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

/// <summary>
/// When <c>Auth:Jwt:Issuer</c> / <c>Auth:Jwt:Audience</c> are set, Bearer validation must match
/// (aligns with <see cref="LowCodePlatform.Backend.Controllers.AuthController"/> minting).
/// </summary>
public sealed class JwtIssuerAudienceValidationTests
{
    private const string TestSigningKey = "test-signing-key-please-change-32bytes!!";
    private const string TestIssuer = "https://lcp.test/";
    private const string TestAudience = "lcp-api";

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
                    ["Auth:Jwt:SigningKey"] = TestSigningKey,
                    ["Auth:Jwt:Issuer"] = TestIssuer,
                    ["Auth:Jwt:Audience"] = TestAudience,
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

    private static async Task<string> MintDevTokenAsync(HttpClient client, string tenantSlug)
    {
        var req = new { subject = "jwt-val-test", tenantSlug = tenantSlug, roles = Array.Empty<string>() };
        using var resp = await client.PostAsJsonAsync("/api/auth/dev-token", req);
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        Assert.NotNull(payload);
        var token = payload!["accessToken"]?.ToString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        return token!;
    }

    [Fact]
    public async Task Bearer_accepts_dev_token_when_issuer_and_audience_configured()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-jwt-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-jwt-{Guid.NewGuid():N}.db");
        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");
        var token = await MintDevTokenAsync(client, "t1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await client.GetAsync("/api/workflows");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Bearer_rejects_token_with_wrong_issuer_when_issuer_configured()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-jwt2-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-jwt2-{Guid.NewGuid():N}.db");
        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var bad = new JwtSecurityToken(
            issuer: "https://wrong-issuer.example/",
            audience: TestAudience,
            claims: new[] { new Claim("sub", "x"), new Claim("tenant", "t1") },
            notBefore: now,
            expires: now.AddHours(1),
            signingCredentials: creds);
        var badString = new JwtSecurityTokenHandler().WriteToken(bad);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", badString);

        using var resp = await client.GetAsync("/api/workflows");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Bearer_rejects_token_with_wrong_audience_when_audience_configured()
    {
        var mgmtDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-mgmt-jwt3-{Guid.NewGuid():N}.db");
        var tenantDbPath = Path.Combine(Path.GetTempPath(), $"lcp-test-tenant-jwt3-{Guid.NewGuid():N}.db");
        var managementCs = $"Data Source={mgmtDbPath}";
        var tenantCs = $"Data Source={tenantDbPath}";

        await InitializeDatabasesAsync(managementCs, "t1", tenantCs, CancellationToken.None);

        await using var factory = new TestAppFactory("t1", mgmtDbPath, tenantDbPath);
        using var client = CreateTenantClient(factory, "t1");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var bad = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: "wrong-audience",
            claims: new[] { new Claim("sub", "x"), new Claim("tenant", "t1") },
            notBefore: now,
            expires: now.AddHours(1),
            signingCredentials: creds);
        var badString = new JwtSecurityTokenHandler().WriteToken(bad);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", badString);

        using var resp = await client.GetAsync("/api/workflows");

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
