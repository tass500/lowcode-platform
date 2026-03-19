using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Models;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/admin/observability")]
[NoStoreNoCache]
[Authorize(Policy = "admin")]
public sealed class AdminObservabilityController : ControllerBase
{
    private readonly PlatformDbContext _db;
    private readonly InstallationService _installation;
    private readonly VersionEnforcementService _enforcement;

    public AdminObservabilityController(
        PlatformDbContext db,
        InstallationService installation,
        VersionEnforcementService enforcement)
    {
        _db = db;
        _installation = installation;
        _enforcement = enforcement;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow,
            Details: details));

    [HttpGet]
    public async Task<ActionResult<ObservabilityResponse>> Get(CancellationToken ct = default)
    {
        Installation inst;
        try
        {
            inst = await _installation.GetDefaultAsync(ct);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "installation_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, "installation_missing", "Installation not found.");
        }

        var enforcement = _enforcement.Evaluate(inst);

        var activeRuns = await _db.UpgradeRuns
            .Where(x => x.InstallationId == inst.InstallationId)
            .Where(x => x.State == UpgradeRunStates.Pending || x.State == UpgradeRunStates.Running)
            .OrderBy(x => x.State == UpgradeRunStates.Running ? 0 : 1)
            .ThenBy(x => x.StartedAtUtc ?? DateTime.MaxValue)
            .ThenBy(x => x.UpgradeRunId)
            .Select(x => new ObservabilityActiveRunDto(
                x.UpgradeRunId,
                x.TargetVersion,
                x.State,
                x.StartedAtUtc,
                x.TraceId))
            .ToListAsync(ct);

        var lastAudit = await _db.AuditLogs
            .Where(x => x.InstallationId == inst.InstallationId)
            .OrderByDescending(x => x.TimestampUtc)
            .ThenByDescending(x => x.AuditLogId)
            .Select(x => new ObservabilityLastAuditDto(
                x.AuditLogId,
                DateTime.SpecifyKind(x.TimestampUtc, DateTimeKind.Utc),
                x.Actor,
                x.Action,
                x.Target,
                x.TraceId))
            .FirstOrDefaultAsync(ct);

        return Ok(new ObservabilityResponse(
            ServerTimeUtc: DateTime.UtcNow,
            InstallationId: inst.InstallationId,
            EnforcementState: enforcement.EnforcementState,
            DaysOutOfSupport: enforcement.DaysOutOfSupport,
            ActiveRuns: activeRuns,
            LastAudit: lastAudit));
    }
}
