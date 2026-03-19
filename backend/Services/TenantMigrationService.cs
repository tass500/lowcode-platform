using LowCodePlatform.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

public sealed class TenantMigrationService
{
    private readonly TenantRegistryService _registry;
    private readonly ITenantSecretResolver _secrets;

    public TenantMigrationService(TenantRegistryService registry, ITenantSecretResolver secrets)
    {
        _registry = registry;
        _secrets = secrets;
    }

    public async Task<List<TenantMigrationResult>> EnsureTenantDatabasesAsync(CancellationToken ct)
    {
        var tenants = await _registry.ListAsync(ct);
        var results = new List<TenantMigrationResult>(tenants.Count);

        foreach (var t in tenants)
        {
            var startedAtUtc = DateTime.UtcNow;
            try
            {
                var cs = ResolveConnectionString(t);

                var options = new DbContextOptionsBuilder<PlatformDbContext>()
                    .UseSqlite(cs)
                    .Options;

                await using var tenantDb = new PlatformDbContext(options);

                await tenantDb.Database.MigrateAsync(ct);

                results.Add(new TenantMigrationResult(t.Slug, true, null, startedAtUtc, DateTime.UtcNow));
            }
            catch (Exception ex)
            {
                results.Add(new TenantMigrationResult(t.Slug, false, ex.Message, startedAtUtc, DateTime.UtcNow));
            }
        }

        return results;
    }

    public async Task<TenantMigrationResult> EnsureTenantDatabaseAsync(LowCodePlatform.Backend.Models.Tenant tenant, CancellationToken ct)
    {
        var startedAtUtc = DateTime.UtcNow;
        try
        {
            var cs = ResolveConnectionString(tenant);

            var options = new DbContextOptionsBuilder<PlatformDbContext>()
                .UseSqlite(cs)
                .Options;

            await using var tenantDb = new PlatformDbContext(options);
            await tenantDb.Database.MigrateAsync(ct);

            return new TenantMigrationResult(tenant.Slug, true, null, startedAtUtc, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return new TenantMigrationResult(tenant.Slug, false, ex.Message, startedAtUtc, DateTime.UtcNow);
        }
    }

    private string ResolveConnectionString(LowCodePlatform.Backend.Models.Tenant tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant.ConnectionStringSecretRef))
        {
            var resolved = _secrets.Resolve(tenant.ConnectionStringSecretRef);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved;

            if (!string.IsNullOrWhiteSpace(tenant.ConnectionString))
                return tenant.ConnectionString;

            throw new InvalidOperationException("tenant_connection_string_secret_not_resolved");
        }

        if (string.IsNullOrWhiteSpace(tenant.ConnectionString))
            throw new InvalidOperationException("tenant_connection_string_missing");

        return tenant.ConnectionString;
    }
}

public sealed record TenantMigrationResult(
    string TenantSlug,
    bool Succeeded,
    string? Error,
    DateTime StartedAtUtc,
    DateTime FinishedAtUtc);
