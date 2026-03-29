using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace LowCodePlatform.Backend.Auth.Bff;

public interface IBffSessionReader
{
    /// <summary>
    /// Reads and validates the BFF session cookie (Data Protection + expiry). Returns false when BFF is disabled or cookie missing/invalid/expired.
    /// </summary>
    bool TryGetValidPayload(HttpContext context, [NotNullWhen(true)] out BffSessionCookiePayload? payload);
}
