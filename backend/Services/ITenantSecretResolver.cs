namespace LowCodePlatform.Backend.Services;

public interface ITenantSecretResolver
{
    string? Resolve(string secretRef);
}
