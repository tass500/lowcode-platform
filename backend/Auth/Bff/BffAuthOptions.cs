namespace LowCodePlatform.Backend.Auth.Bff;

public sealed class BffAuthOptions
{
    public const string SectionName = "Auth:Bff";

    /// <summary>When true, BFF OAuth endpoints are active (still requires OIDC authority + SPA client id).</summary>
    public bool Enabled { get; set; }

    public string SessionCookieName { get; set; } = "lcp.bff.session";

    public string StateCookieName { get; set; } = "lcp.bff.state";

    public string VerifierCookieName { get; set; } = "lcp.bff.ver";

    /// <summary>Browser-visible path for the OAuth redirect_uri (must match IdP app registration).</summary>
    public string CallbackPath { get; set; } = "/api/auth/bff/callback";

    /// <summary>Relative path (leading slash) after successful login.</summary>
    public string PostLoginRedirectPath { get; set; } = "/lowcode/workflows";

    public string PostLoginErrorQueryParam { get; set; } = "bff_error";
}
