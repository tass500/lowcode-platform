using System.Net.Http.Headers;
using System.Text.Json;

namespace LowCodePlatform.Backend.Auth.Bff;

public sealed class OidcHttpForBff : IOidcHttpForBff
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;

    public OidcHttpForBff(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<OidcDiscoveryDocument?> GetDiscoveryAsync(string authority, CancellationToken cancellationToken = default)
    {
        var baseUrl = authority.Trim().TrimEnd('/');
        if (baseUrl.Length == 0)
            return null;

        var url = $"{baseUrl}/.well-known/openid-configuration";
        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.Timeout = TimeSpan.FromSeconds(30);

        using var resp = await client.GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<OidcDiscoveryDocument>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<OidcTokenResponse?> ExchangeCodeAsync(
        string tokenEndpoint,
        IReadOnlyDictionary<string, string> form,
        CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        using var content = new FormUrlEncodedContent(form);
        using var resp = await client.PostAsync(tokenEndpoint, content, cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
            return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonSerializer.DeserializeAsync<OidcTokenResponse>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }
}
