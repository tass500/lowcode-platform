using Microsoft.Extensions.Configuration;

namespace LowCodePlatform.Backend.Services;

public sealed class ConfigurationTenantSecretResolver : ITenantSecretResolver
{
    private readonly IConfiguration _cfg;

    public ConfigurationTenantSecretResolver(IConfiguration cfg)
    {
        _cfg = cfg;
    }

    public string? Resolve(string secretRef)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
            return null;

        return _cfg[$"Tenancy:Secrets:{secretRef}"];
    }
}
