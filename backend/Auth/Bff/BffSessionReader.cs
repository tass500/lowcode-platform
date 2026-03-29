using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LowCodePlatform.Backend.Auth.Bff;

public sealed class BffSessionReader : IBffSessionReader
{
    private readonly BffAuthOptions _options;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;

    public BffSessionReader(
        IOptions<BffAuthOptions> options,
        IHostEnvironment env,
        IConfiguration configuration,
        IDataProtectionProvider dataProtection)
    {
        _options = options.Value;
        _env = env;
        _configuration = configuration;
        _protector = dataProtection.CreateProtector("Lcp.BffSession.v1");
    }

    public bool TryGetValidPayload(HttpContext context, [NotNullWhen(true)] out BffSessionCookiePayload? payload)
    {
        payload = null;
        if (!BffAuthFeature.IsEnabled(_options, _env, _configuration))
            return false;

        if (!context.Request.Cookies.TryGetValue(_options.SessionCookieName, out var raw) || string.IsNullOrEmpty(raw))
            return false;

        try
        {
            var bytes = _protector.Unprotect(Convert.FromBase64String(raw));
            var p = JsonSerializer.Deserialize<BffSessionCookiePayload>(Encoding.UTF8.GetString(bytes));
            if (p?.AccessToken is null || p.AccessToken.Length == 0)
                return false;

            var expUtc = DateTimeOffset.FromUnixTimeSeconds(p.ExpiresAtUnix).UtcDateTime;
            if (expUtc <= DateTime.UtcNow)
                return false;

            payload = p;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
