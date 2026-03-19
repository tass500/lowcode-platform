using LowCodePlatform.Backend.Models;

namespace LowCodePlatform.Backend.Services;

public sealed class TenantDbConnectionStringProvider
{
    private readonly TenantContext _tenant;
    private readonly TenantRegistryService _registry;
    private readonly ITenantSecretResolver _secrets;
    private readonly IHostEnvironment _env;

    private string? _cached;

    public TenantDbConnectionStringProvider(
        TenantContext tenant,
        TenantRegistryService registry,
        ITenantSecretResolver secrets,
        IHostEnvironment env)
    {
        _tenant = tenant;
        _registry = registry;
        _secrets = secrets;
        _env = env;
    }

    public async Task<string> GetAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_cached))
            return _cached!;

        var t = await _registry.FindBySlugAsync(_tenant.Slug, ct);
        if (t is null)
        {
            // Dev/Test safety valve: allow demo flows to work even if the management DB is stale
            // but Tenancy:Secrets already contains a connection for the tenant slug.
            if (_env.IsDevelopment() || _env.IsEnvironment("Testing"))
            {
                var resolved = _secrets.Resolve(_tenant.Slug);
                if (!string.IsNullOrWhiteSpace(resolved))
                    t = await _registry.CreateAsync(_tenant.Slug, connectionStringSecretRef: _tenant.Slug, connectionString: null, ct);
            }

            if (t is null)
                throw new InvalidOperationException("tenant_not_found");
        }

        if (!string.IsNullOrWhiteSpace(t.ConnectionStringSecretRef))
        {
            var resolved = _secrets.Resolve(t.ConnectionStringSecretRef);
            if (string.IsNullOrWhiteSpace(resolved))
                throw new InvalidOperationException("tenant_connection_string_secret_not_resolved");
            _cached = resolved;
            return _cached;
        }

        if (string.IsNullOrWhiteSpace(t.ConnectionString))
            throw new InvalidOperationException("tenant_connection_string_missing");

        _cached = t.ConnectionString;
        return _cached;
    }

    public string Get()
    {
        if (!string.IsNullOrWhiteSpace(_cached))
            return _cached!;

        return GetAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public string GetOrThrow() => _cached ?? throw new InvalidOperationException("tenant_connection_string_not_resolved");
}
