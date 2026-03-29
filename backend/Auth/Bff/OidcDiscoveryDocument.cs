using System.Text.Json.Serialization;

namespace LowCodePlatform.Backend.Auth.Bff;

public sealed class OidcDiscoveryDocument
{
    [JsonPropertyName("authorization_endpoint")]
    public string? AuthorizationEndpoint { get; set; }

    [JsonPropertyName("token_endpoint")]
    public string? TokenEndpoint { get; set; }
}

public sealed class OidcTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }
}

public sealed class BffSessionCookiePayload
{
    public string AccessToken { get; set; } = "";

    public string? RefreshToken { get; set; }

    public long ExpiresAtUnix { get; set; }
}
