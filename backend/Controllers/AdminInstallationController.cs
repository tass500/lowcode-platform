using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/admin/installation")]
[NoStoreNoCache]
[Authorize(Policy = "admin")]
public sealed class AdminInstallationController : ControllerBase
{
    private readonly InstallationService _installation;
    private readonly VersionEnforcementService _enforcement;
    private readonly AuditService _audit;
    private readonly Data.PlatformDbContext _db;
    private readonly IHostEnvironment _env;

    public AdminInstallationController(
        InstallationService installation,
        VersionEnforcementService enforcement,
        AuditService audit,
        Data.PlatformDbContext db,
        IHostEnvironment env)
    {
        _installation = installation;
        _enforcement = enforcement;
        _audit = audit;
        _db = db;
        _env = env;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow,
            Details: details));

    [HttpGet("status")]
    public async Task<ActionResult<InstallationStatusResponse>> GetStatus(CancellationToken ct)
    {
        Models.Installation inst;
        try
        {
            inst = await _installation.GetDefaultAsync(ct);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "installation_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, "installation_missing", "Installation not found.");
        }

        var status = _enforcement.Evaluate(inst);

        await _audit.WriteAsync("admin", "installation_status_viewed", inst.InstallationId.ToString(), inst.InstallationId, null, TraceIdMiddleware.GetTraceId(HttpContext), null, ct);

        return Ok(new InstallationStatusResponse(
            ServerTimeUtc: DateTime.UtcNow,
            InstallationId: inst.InstallationId,
            CurrentVersion: inst.CurrentVersion,
            SupportedVersion: inst.SupportedVersion,
            ReleaseDateUtc: inst.ReleaseDateUtc,
            UpgradeWindowDays: inst.UpgradeWindowDays,
            EnforcementState: status.EnforcementState,
            DaysOutOfSupport: status.DaysOutOfSupport));
    }

    public sealed record DevSetInstallationRequest(
        string CurrentVersion,
        string SupportedVersion,
        DateTime ReleaseDateUtc,
        int UpgradeWindowDays);

    [HttpPost("dev-set")]
    public async Task<ActionResult<InstallationStatusResponse>> DevSet([FromBody] DevSetInstallationRequest req, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return Problem(StatusCodes.Status404NotFound, "not_found", "Not found.");

        if (string.IsNullOrWhiteSpace(req.CurrentVersion) || string.IsNullOrWhiteSpace(req.SupportedVersion))
            return Problem(StatusCodes.Status400BadRequest, "version_missing", "CurrentVersion and SupportedVersion are required.");

        if (req.UpgradeWindowDays <= 0)
            return Problem(StatusCodes.Status400BadRequest, "upgrade_window_invalid", "UpgradeWindowDays must be > 0.");

        var inst = await _installation.GetDefaultAsync(ct);

        inst.CurrentVersion = req.CurrentVersion.Trim();
        inst.SupportedVersion = req.SupportedVersion.Trim();
        inst.ReleaseDateUtc = DateTime.SpecifyKind(req.ReleaseDateUtc, DateTimeKind.Utc);
        inst.UpgradeWindowDays = req.UpgradeWindowDays;
        inst.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var traceId = TraceIdMiddleware.GetTraceId(HttpContext);
        await _audit.WriteAsync(
            "admin",
            "installation_dev_set",
            inst.InstallationId.ToString(),
            inst.InstallationId,
            null,
            traceId,
            $"{{\"currentVersion\":\"{inst.CurrentVersion}\",\"supportedVersion\":\"{inst.SupportedVersion}\",\"releaseDateUtc\":\"{inst.ReleaseDateUtc:O}\",\"upgradeWindowDays\":{inst.UpgradeWindowDays}}}",
            ct);

        var status = _enforcement.Evaluate(inst);
        return Ok(new InstallationStatusResponse(
            ServerTimeUtc: DateTime.UtcNow,
            InstallationId: inst.InstallationId,
            CurrentVersion: inst.CurrentVersion,
            SupportedVersion: inst.SupportedVersion,
            ReleaseDateUtc: inst.ReleaseDateUtc,
            UpgradeWindowDays: inst.UpgradeWindowDays,
            EnforcementState: status.EnforcementState,
            DaysOutOfSupport: status.DaysOutOfSupport));
    }
}
