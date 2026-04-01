namespace LowCodePlatform.Backend.Auth.Bff;

public interface IOidcHttpForBff
{
    Task<OidcDiscoveryDocument?> GetDiscoveryAsync(string authority, CancellationToken cancellationToken = default);

    Task<OidcTokenResponse?> ExchangeCodeAsync(
        string tokenEndpoint,
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken = default);
}
