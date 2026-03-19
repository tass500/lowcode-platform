using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Models;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/admin/upgrade-runs")]
[NoStoreNoCache]
[Authorize(Policy = "admin")]
public sealed class AdminUpgradeRunsController : ControllerBase
{
    private readonly PlatformDbContext _db;
    private readonly InstallationService _installation;
    private readonly AuditService _audit;
    private readonly VersionEnforcementService _enforcement;
    private readonly DevUpgradeFaults _devFaults;
    private readonly IHostEnvironment _env;

    public AdminUpgradeRunsController(
        PlatformDbContext db,
        InstallationService installation,
        AuditService audit,
        VersionEnforcementService enforcement,
        DevUpgradeFaults devFaults,
        IHostEnvironment env)
    {
        _db = db;
        _installation = installation;
        _audit = audit;
        _enforcement = enforcement;
        _devFaults = devFaults;
        _env = env;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow,
            Details: details));

    private static UpgradeRunDetailsResponse ToDetailsResponse(UpgradeRun run)
    {
        var steps = run.Steps
            .OrderBy(x => x.StepKey)
            .Select(s => new UpgradeRunStepDto(
                s.StepKey,
                s.State,
                s.Attempt,
                s.NextRetryAtUtc,
                s.LastErrorCode,
                s.LastErrorMessage,
                s.StartedAtUtc,
                s.FinishedAtUtc));

        return new UpgradeRunDetailsResponse(
            ServerTimeUtc: DateTime.UtcNow,
            UpgradeRunId: run.UpgradeRunId,
            InstallationId: run.InstallationId,
            TargetVersion: run.TargetVersion,
            State: run.State,
            StartedAtUtc: run.StartedAtUtc,
            FinishedAtUtc: run.FinishedAtUtc,
            ErrorCode: run.ErrorCode,
            ErrorMessage: run.ErrorMessage,
            TraceId: run.TraceId,
            Steps: steps);
    }

    public sealed record StartUpgradeRequest(string TargetVersion);

    [HttpGet("recent")]
    public async Task<ActionResult<RecentUpgradeRunsResponse>> Recent([FromQuery] int take = 10, CancellationToken ct = default)
    {
        if (take <= 0 || take > 100)
            return Problem(StatusCodes.Status400BadRequest, "take_invalid", "take must be between 1 and 100.");

        LowCodePlatform.Backend.Models.Installation inst;
        try
        {
            inst = await _installation.GetDefaultAsync(ct);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "installation_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, "installation_missing", "Installation not found.");
        }

        var items = await _db.UpgradeRuns
            .Where(x => x.InstallationId == inst.InstallationId)
            .OrderByDescending(x => x.StartedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(x => x.UpgradeRunId)
            .Take(take)
            .Select(x => new UpgradeRunSummaryDto(
                x.UpgradeRunId,
                x.TargetVersion,
                x.State,
                x.StartedAtUtc,
                x.FinishedAtUtc,
                x.TraceId))
            .ToListAsync(ct);

        return Ok(new RecentUpgradeRunsResponse(ServerTimeUtc: DateTime.UtcNow, Items: items));
    }

    [HttpGet("latest")]
    public async Task<ActionResult<UpgradeRunDetailsResponse>> Latest(CancellationToken ct = default)
    {
        LowCodePlatform.Backend.Models.Installation inst;
        try
        {
            inst = await _installation.GetDefaultAsync(ct);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "installation_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, "installation_missing", "Installation not found.");
        }

        var active = await _db.UpgradeRuns
            .Where(x => x.InstallationId == inst.InstallationId)
            .Where(x => x.State == UpgradeRunStates.Pending || x.State == UpgradeRunStates.Running)
            .OrderByDescending(x => x.StartedAtUtc ?? DateTime.MinValue)
            .ThenByDescending(x => x.UpgradeRunId)
            .Select(x => x.UpgradeRunId)
            .FirstOrDefaultAsync(ct);

        var id = active;

        if (id == Guid.Empty)
        {
            id = await _db.UpgradeRuns
                .Where(x => x.InstallationId == inst.InstallationId)
                .OrderByDescending(x => x.StartedAtUtc ?? DateTime.MinValue)
                .ThenByDescending(x => x.UpgradeRunId)
                .Select(x => x.UpgradeRunId)
                .FirstOrDefaultAsync(ct);
        }

        if (id == Guid.Empty)
            return Problem(StatusCodes.Status404NotFound, "upgrade_run_not_found", "No upgrade runs found.");

        var run = await _db.UpgradeRuns.Include(x => x.Steps).FirstOrDefaultAsync(x => x.UpgradeRunId == id, ct);
        if (run is null)
            return Problem(StatusCodes.Status404NotFound, "upgrade_run_not_found", "Upgrade run not found.");

        return Ok(ToDetailsResponse(run));
    }

    [HttpGet("queue")]
    public async Task<ActionResult<QueueUpgradeRunsResponse>> Queue(CancellationToken ct = default)
    {
        LowCodePlatform.Backend.Models.Installation inst;
        try
        {
            inst = await _installation.GetDefaultAsync(ct);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "installation_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, "installation_missing", "Installation not found.");
        }

        var items = await _db.UpgradeRuns
            .Where(x => x.InstallationId == inst.InstallationId)
            .Where(x => x.State == UpgradeRunStates.Running || x.State == UpgradeRunStates.Pending)
            .OrderBy(x => x.State == UpgradeRunStates.Running ? 0 : 1)
            .ThenBy(x => x.StartedAtUtc ?? DateTime.MaxValue)
            .Select(x => new QueueUpgradeRunDto(
                x.UpgradeRunId,
                x.TargetVersion,
                x.State,
                x.StartedAtUtc,
                x.TraceId))
            .ToListAsync(ct);

        return Ok(new QueueUpgradeRunsResponse(ServerTimeUtc: DateTime.UtcNow, Items: items));
    }

    public sealed record DevFailStepRequest(string StepKey);

    [HttpPost("{id:guid}/dev-fail-step")]
    public async Task<ActionResult<SimpleStatusResponse>> DevFailStep([FromRoute] Guid id, [FromBody] DevFailStepRequest req, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return Problem(StatusCodes.Status404NotFound, "not_found", "Not found.");

        if (string.IsNullOrWhiteSpace(req.StepKey))
            return Problem(StatusCodes.Status400BadRequest, "step_key_missing", "StepKey is required.");

        var stepKey = req.StepKey.Trim();
        if (stepKey is not ("canary-migrate" or "wave1-migrate"))
            return Problem(StatusCodes.Status400BadRequest, "step_key_invalid", "StepKey must be one of: canary-migrate, wave1-migrate.");

        var runExists = await _db.UpgradeRuns.AnyAsync(x => x.UpgradeRunId == id, ct);
        if (!runExists)
            return Problem(StatusCodes.Status404NotFound, "upgrade_run_not_found", "Upgrade run not found.");

        _devFaults.RequestFailOnce(id, stepKey);

        var traceId = TraceIdMiddleware.GetTraceId(HttpContext);
        await _audit.WriteAsync("admin", "upgrade_dev_fail_step_requested", stepKey, null, null, traceId, $"{{\"upgradeRunId\":\"{id}\"}}", ct);

        return Ok(new SimpleStatusResponse(ServerTimeUtc: DateTime.UtcNow, Status: "ok"));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SimpleStatusResponse>> Cancel([FromRoute] Guid id, CancellationToken ct)
    {
        var run = await _db.UpgradeRuns.Include(x => x.Steps).FirstOrDefaultAsync(x => x.UpgradeRunId == id, ct);
        if (run is null)
            return Problem(StatusCodes.Status404NotFound, "upgrade_run_not_found", "Upgrade run not found.");

        var traceId = TraceIdMiddleware.GetTraceId(HttpContext);

        if (run.State is UpgradeRunStates.Succeeded or UpgradeRunStates.Failed or UpgradeRunStates.Canceled)
            return Problem(StatusCodes.Status400BadRequest, "upgrade_run_not_cancelable", "Upgrade run is not cancelable in its current state.");

        var now = DateTime.UtcNow;
        run.State = UpgradeRunStates.Canceled;
        run.ErrorCode = "canceled";
        run.ErrorMessage = "Canceled by admin.";
        run.FinishedAtUtc = now;

        foreach (var step in run.Steps.Where(x => x.State != UpgradeRunStepStates.Succeeded))
        {
            step.State = UpgradeRunStepStates.Canceled;
            step.FinishedAtUtc ??= now;
            step.NextRetryAtUtc = null;
            step.LastErrorCode = "canceled";
            step.LastErrorMessage = "Canceled by admin.";
        }

        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("admin", "upgrade_run_canceled", run.UpgradeRunId.ToString(), run.InstallationId, null, traceId, null, ct);

        return Ok(new SimpleStatusResponse(ServerTimeUtc: DateTime.UtcNow, Status: "ok"));
    }

    [HttpPost]
    public async Task<ActionResult<StartUpgradeResponse>> StartUpgrade([FromBody] StartUpgradeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.TargetVersion))
            return Problem(StatusCodes.Status400BadRequest, "target_version_missing", "TargetVersion is required.");

        var inst = await _installation.GetDefaultAsync(ct);
        var traceId = TraceIdMiddleware.GetTraceId(HttpContext);

        var enforcement = _enforcement.Evaluate(inst);
        if (enforcement.EnforcementState is EnforcementStates.HardBlock)
        {
            await _audit.WriteAsync("admin", "upgrade_blocked_by_supported_version", inst.InstallationId.ToString(), inst.InstallationId, null, traceId, null, ct);
            return Problem(
                StatusCodes.Status403Forbidden,
                "supported_version_enforcement_block",
                "Upgrade operations are blocked until the installation is upgraded.",
                new List<ErrorDetail>
                {
                    new("enforcementState", "enforcement_state", $"{enforcement.EnforcementState}", "error"),
                    new("daysOutOfSupport", "days_out_of_support", $"{enforcement.DaysOutOfSupport}", "error"),
                });
        }

        if (enforcement.EnforcementState is EnforcementStates.SoftBlock)
            await _audit.WriteAsync("admin", "upgrade_started_under_soft_block", inst.InstallationId.ToString(), inst.InstallationId, null, traceId, $"{{\"daysOutOfSupport\":{enforcement.DaysOutOfSupport}}}", ct);

        var hasActive = await _db.UpgradeRuns.AnyAsync(
            x => x.InstallationId == inst.InstallationId && (x.State == UpgradeRunStates.Pending || x.State == UpgradeRunStates.Running),
            ct);
        if (hasActive)
        {
            await _audit.WriteAsync("admin", "upgrade_start_blocked_by_active_run", inst.InstallationId.ToString(), inst.InstallationId, null, traceId, null, ct);
            return Problem(StatusCodes.Status409Conflict, "upgrade_run_already_active", "An upgrade run is already active. Cancel it or wait until it finishes.");
        }

        var run = new UpgradeRun
        {
            UpgradeRunId = Guid.NewGuid(),
            InstallationId = inst.InstallationId,
            TargetVersion = req.TargetVersion.Trim(),
            State = UpgradeRunStates.Pending,
            StartedAtUtc = DateTime.UtcNow,
            TraceId = traceId,
            Steps = new List<UpgradeRunStep>
            {
                new()
                {
                    UpgradeRunStepId = Guid.NewGuid(),
                    StepKey = "canary-migrate",
                    State = UpgradeRunStepStates.Pending,
                },
                new()
                {
                    UpgradeRunStepId = Guid.NewGuid(),
                    StepKey = "wave1-migrate",
                    State = UpgradeRunStepStates.Pending,
                },
            }
        };

        _db.UpgradeRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        await _audit.WriteAsync("admin", "upgrade_run_created", run.UpgradeRunId.ToString(), inst.InstallationId, null, traceId, null, ct);

        return Ok(new StartUpgradeResponse(ServerTimeUtc: DateTime.UtcNow, UpgradeRunId: run.UpgradeRunId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UpgradeRunDetailsResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        var run = await _db.UpgradeRuns.Include(x => x.Steps).FirstOrDefaultAsync(x => x.UpgradeRunId == id, ct);
        if (run is null)
            return Problem(StatusCodes.Status404NotFound, "upgrade_run_not_found", "Upgrade run not found.");

        return Ok(ToDetailsResponse(run));
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<SimpleStatusResponse>> Retry([FromRoute] Guid id, CancellationToken ct)
    {
        var run = await _db.UpgradeRuns.Include(x => x.Steps).FirstOrDefaultAsync(x => x.UpgradeRunId == id, ct);
        if (run is null)
            return Problem(StatusCodes.Status404NotFound, "upgrade_run_not_found", "Upgrade run not found.");

        var traceId = TraceIdMiddleware.GetTraceId(HttpContext);

        var inst = await _installation.GetDefaultAsync(ct);
        var enforcement = _enforcement.Evaluate(inst);
        if (enforcement.EnforcementState is EnforcementStates.HardBlock)
        {
            await _audit.WriteAsync("admin", "upgrade_retry_blocked_by_supported_version", inst.InstallationId.ToString(), inst.InstallationId, null, traceId, null, ct);
            return Problem(
                StatusCodes.Status403Forbidden,
                "supported_version_enforcement_block",
                "Upgrade operations are blocked until the installation is upgraded.",
                new List<ErrorDetail>
                {
                    new("enforcementState", "enforcement_state", $"{enforcement.EnforcementState}", "error"),
                    new("daysOutOfSupport", "days_out_of_support", $"{enforcement.DaysOutOfSupport}", "error"),
                });
        }

        if (enforcement.EnforcementState is EnforcementStates.SoftBlock)
            await _audit.WriteAsync("admin", "upgrade_retry_started_under_soft_block", inst.InstallationId.ToString(), inst.InstallationId, null, traceId, $"{{\"daysOutOfSupport\":{enforcement.DaysOutOfSupport}}}", ct);

        var hasOtherActive = await _db.UpgradeRuns.AnyAsync(
            x => x.InstallationId == inst.InstallationId
                 && x.UpgradeRunId != id
                 && (x.State == UpgradeRunStates.Pending || x.State == UpgradeRunStates.Running),
            ct);
        if (hasOtherActive)
        {
            await _audit.WriteAsync("admin", "upgrade_retry_blocked_by_active_run", inst.InstallationId.ToString(), inst.InstallationId, null, traceId, null, ct);
            return Problem(StatusCodes.Status409Conflict, "upgrade_run_already_active", "Another upgrade run is already active. Cancel it or wait until it finishes.");
        }

        if (run.State != UpgradeRunStates.Failed)
            return Problem(StatusCodes.Status400BadRequest, "upgrade_run_not_failed", "Upgrade run is not in failed state.");

        run.State = UpgradeRunStates.Running;
        run.ErrorCode = null;
        run.ErrorMessage = null;
        run.FinishedAtUtc = null;

        foreach (var step in run.Steps.Where(x => x.State == UpgradeRunStepStates.Failed))
        {
            step.State = UpgradeRunStepStates.Pending;
            step.NextRetryAtUtc = null;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.WriteAsync("admin", "upgrade_run_retried", run.UpgradeRunId.ToString(), run.InstallationId, null, traceId, null, ct);

        return Ok(new SimpleStatusResponse(ServerTimeUtc: DateTime.UtcNow, Status: "ok"));
    }
}
