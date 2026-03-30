using LowCodePlatform.Backend.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LowCodePlatform.Backend.Tests.Deprecation;

/// <summary>
/// Probe controller (test assembly only; application part wired in integration tests).
/// </summary>
[ApiController]
[Route("api/_test")]
public sealed class DeprecationProbeController : ControllerBase
{
    [HttpGet("deprecation-probe")]
    [ApiDeprecated(SunsetUtcIso = "2030-06-15T12:00:00Z")]
    public IActionResult Get() => Ok(new { ok = true });
}
