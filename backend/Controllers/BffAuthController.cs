using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using LowCodePlatform.Backend;
using LowCodePlatform.Backend.Auth.Bff;
using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/auth/bff")]
[NoStoreNoCache]
public sealed class BffAuthController : ControllerBase
{
    private readonly IConfiguration _cfg;
    private readonly IHostEnvironment _env;
    private readonly BffAuthOptions _options;
    private readonly IOidcHttpForBff _oidc;
    private readonly IDataProtector _protector;
    private readonly IBffSessionReader _bffSessionReader;

    public BffAuthController(
        IConfiguration cfg,
        IHostEnvironment env,
        IOptions<BffAuthOptions> options,
        IOidcHttpForBff oidc,
        IDataProtectionProvider dataProtection,
        IBffSessionReader bffSessionReader)
    {
        _cfg = cfg;
        _env = env;
        _options = options.Value;
        _oidc = oidc;
        _protector = dataProtection.CreateProtector("Lcp.BffSession.v1");
        _bffSessionReader = bffSessionReader;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow));

    private bool IsBffFeatureOn() => BffAuthFeature.IsEnabled(_options, _env, _cfg);

    private CookieOptions EphemeralPkceCookieOptions() =>
        new()
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/api/auth/bff",
        };

    private CookieOptions SessionCookieOptions() =>
        new()
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(7),
            Path = "/",
        };

    private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host.Value}";

    private string RedirectUri() => PublicBaseUrl() + _options.CallbackPath;

    private bool TryGetOidcParams(out string authority, out string clientId, out string scope)
    {
        authority = _cfg["Auth:Oidc:Authority"]?.Trim() ?? "";
        clientId = _cfg["Auth:Oidc:SpaClientId"]?.Trim() ?? "";
        scope = (_cfg["Auth:Oidc:SpaScope"] ?? "openid profile offline_access").Trim();
        return authority.Length > 0 && clientId.Length > 0;
    }

    private IActionResult RedirectWithBffError(string errorCode)
    {
        var param = string.IsNullOrWhiteSpace(_options.PostLoginErrorQueryParam) ? "bff_error" : _options.PostLoginErrorQueryParam;
        return LocalRedirect($"/lowcode/auth?{Uri.EscapeDataString(param)}={Uri.EscapeDataString(errorCode)}");
    }

    private void ClearPkceCookies()
    {
        var o = EphemeralPkceCookieOptions();
        Response.Cookies.Delete(_options.StateCookieName, o);
        Response.Cookies.Delete(_options.VerifierCookieName, o);
    }

    private static string BuildAuthorizeUrl(
        string authorizationEndpoint,
        string clientId,
        string scope,
        string redirectUri,
        string state,
        string codeChallenge)
    {
        var q = new Dictionary<string, string?>
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
        };
        var qb = QueryString.Create(q.Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value)));
        return authorizationEndpoint + qb.ToUriComponent();
    }

    private string? ReadTenantHint(string accessToken)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            var raw = _cfg["Auth:Oidc:TenantClaimSource"]?.Trim();
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (var typ in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var c = jwt.Claims.FirstOrDefault(x => x.Type == typ);
                    if (!string.IsNullOrEmpty(c?.Value))
                        return c.Value;
                }
            }

            return jwt.Claims.FirstOrDefault(x => x.Type == OidcJwtClaimMapping.TenantClaimType)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadSubjectHint(string accessToken)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            return jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
        }
        catch
        {
            return null;
        }
    }

    public sealed record BffMetaResponseDto(bool Enabled, bool Configured, string? LoginPath, string? SessionPath, string? CallbackPath);

    /// <summary>SPA discovers whether server-side BFF OAuth is on and configured.</summary>
    [HttpGet("meta")]
    [AllowAnonymous]
    public ActionResult<BffMetaResponseDto> Meta()
    {
        if (!IsBffFeatureOn())
            return Ok(new BffMetaResponseDto(false, false, null, null, null));

        var configured = TryGetOidcParams(out _, out _, out _);
        return Ok(new BffMetaResponseDto(
            true,
            configured,
            configured ? "/api/auth/bff/login" : null,
            configured ? "/api/auth/bff/session" : null,
            configured ? _options.CallbackPath : null));
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(CancellationToken cancellationToken)
    {
        if (!IsBffFeatureOn())
            return NotFound();

        if (!TryGetOidcParams(out var authority, out var clientId, out var scope))
            return Problem(StatusCodes.Status503ServiceUnavailable, "bff_oidc_not_configured", "Auth:Oidc:Authority and Auth:Oidc:SpaClientId are required for BFF login.");

        var discovery = await _oidc.GetDiscoveryAsync(authority, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(discovery?.AuthorizationEndpoint) || string.IsNullOrWhiteSpace(discovery.TokenEndpoint))
            return Problem(StatusCodes.Status503ServiceUnavailable, "bff_oidc_discovery_failed", "OIDC discovery failed.");

        var state = BffPkce.CreateState();
        var verifier = BffPkce.CreateVerifier();
        var challenge = BffPkce.CreateChallenge(verifier);

        var ephemeral = EphemeralPkceCookieOptions();
        Response.Cookies.Append(_options.StateCookieName, state, ephemeral);
        Response.Cookies.Append(_options.VerifierCookieName, verifier, ephemeral);

        var redirectUri = RedirectUri();
        var authUrl = BuildAuthorizeUrl(discovery.AuthorizationEndpoint, clientId, scope, redirectUri, state, challenge);
        return Redirect(authUrl);
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state, CancellationToken cancellationToken)
    {
        if (!IsBffFeatureOn())
            return NotFound();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            ClearPkceCookies();
            return RedirectWithBffError("missing_code_or_state");
        }

        if (!Request.Cookies.TryGetValue(_options.StateCookieName, out var expectedState)
            || !Request.Cookies.TryGetValue(_options.VerifierCookieName, out var verifier)
            || string.IsNullOrEmpty(expectedState)
            || string.IsNullOrEmpty(verifier))
        {
            ClearPkceCookies();
            return RedirectWithBffError("missing_pkce_cookie");
        }

        if (!string.Equals(expectedState, state, StringComparison.Ordinal))
        {
            ClearPkceCookies();
            return RedirectWithBffError("state_mismatch");
        }

        if (!TryGetOidcParams(out var authority, out var clientId, out _))
        {
            ClearPkceCookies();
            return RedirectWithBffError("oidc_not_configured");
        }

        var discovery = await _oidc.GetDiscoveryAsync(authority, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(discovery?.TokenEndpoint))
        {
            ClearPkceCookies();
            return RedirectWithBffError("discovery_failed");
        }

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = RedirectUri(),
            ["client_id"] = clientId,
            ["code_verifier"] = verifier,
        };

        var token = await _oidc.ExchangeCodeAsync(discovery.TokenEndpoint, form, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(token?.AccessToken))
        {
            ClearPkceCookies();
            return RedirectWithBffError("token_exchange_failed");
        }

        ClearPkceCookies();

        var expiresIn = token.ExpiresIn is > 0 and var s ? s : 3600;
        var exp = DateTimeOffset.UtcNow.AddSeconds(expiresIn).ToUnixTimeSeconds();
        var payload = new BffSessionCookiePayload
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAtUnix = exp,
        };
        var json = JsonSerializer.Serialize(payload);
        var protectedBytes = _protector.Protect(Encoding.UTF8.GetBytes(json));
        var cookieValue = Convert.ToBase64String(protectedBytes);

        Response.Cookies.Append(_options.SessionCookieName, cookieValue, SessionCookieOptions());

        var path = _options.PostLoginRedirectPath;
        if (!path.StartsWith('/'))
            path = "/" + path;
        return LocalRedirect(path);
    }

    public sealed record BffSessionResponseDto(bool Authenticated, DateTime? AccessTokenExpiresAtUtc, string? TenantHint, string? SubjectHint);

    [HttpGet("session")]
    [AllowAnonymous]
    public ActionResult<BffSessionResponseDto> Session()
    {
        if (!IsBffFeatureOn())
            return NotFound();

        if (!_bffSessionReader.TryGetValidPayload(HttpContext, out var payload))
            return Ok(new BffSessionResponseDto(false, null, null, null));

        var expUtc = DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresAtUnix).UtcDateTime;
        var tenantHint = ReadTenantHint(payload.AccessToken);
        var subjectHint = ReadSubjectHint(payload.AccessToken);
        return Ok(new BffSessionResponseDto(true, expUtc, tenantHint, subjectHint));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        if (!IsBffFeatureOn())
            return NotFound();

        Response.Cookies.Delete(_options.SessionCookieName, SessionCookieOptions());
        return Ok();
    }
}
