using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

public sealed class TenantRegistryService
{
    private readonly ManagementDbContext _db;

    public TenantRegistryService(ManagementDbContext db)
    {
        _db = db;
    }

    public Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct)
        => _db.Tenants.FirstOrDefaultAsync(x => x.Slug == slug, ct);

    public Task<List<Tenant>> ListAsync(CancellationToken ct)
        => _db.Tenants.OrderBy(x => x.Slug).ToListAsync(ct);

    public async Task<Tenant> CreateAsync(string slug, string? connectionStringSecretRef, string? connectionString, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new InvalidOperationException("tenant_slug_missing");

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length < 2 || normalized.Length > 50)
            throw new InvalidOperationException("tenant_slug_invalid");

        if (await _db.Tenants.AnyAsync(x => x.Slug == normalized, ct))
            throw new InvalidOperationException("tenant_slug_already_exists");

        if (string.IsNullOrWhiteSpace(connectionStringSecretRef) && string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("tenant_connection_string_missing");

        var tenant = new Tenant
        {
            TenantId = Guid.NewGuid(),
            Slug = normalized,
            ConnectionStringSecretRef = string.IsNullOrWhiteSpace(connectionStringSecretRef) ? null : connectionStringSecretRef.Trim(),
            ConnectionString = string.IsNullOrWhiteSpace(connectionString) ? null : connectionString.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }

    public async Task<string> ProvisionTenantApiKeyAsync(string slug, string? plaintextApiKey, CancellationToken ct)
    {
        var normalized = NormalizeSlug(slug);

        var tenant = await _db.Tenants.FirstOrDefaultAsync(x => x.Slug == normalized, ct)
                     ?? throw new InvalidOperationException("tenant_not_found");

        var apiKey = string.IsNullOrWhiteSpace(plaintextApiKey)
            ? TenantApiKeyHasher.GenerateRandomApiKey()
            : plaintextApiKey.Trim();

        if (apiKey.Length < 24)
            throw new InvalidOperationException("tenant_api_key_too_short");

        tenant.TenantApiKeySha256Hex = TenantApiKeyHasher.HashToHex(apiKey);
        await _db.SaveChangesAsync(ct);
        return apiKey;
    }

    public async Task ClearTenantApiKeyAsync(string slug, CancellationToken ct)
    {
        var normalized = NormalizeSlug(slug);

        var tenant = await _db.Tenants.FirstOrDefaultAsync(x => x.Slug == normalized, ct)
                     ?? throw new InvalidOperationException("tenant_not_found");

        tenant.TenantApiKeySha256Hex = null;
        await _db.SaveChangesAsync(ct);
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new InvalidOperationException("tenant_slug_missing");

        return slug.Trim().ToLowerInvariant();
    }
}
