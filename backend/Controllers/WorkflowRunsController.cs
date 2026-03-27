using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/workflows")]
[NoStoreNoCache]
[Authorize(Policy = "tenant_user")]
public sealed class WorkflowRunsController : ControllerBase
{
    private readonly Data.PlatformDbContext _db;
    private readonly WorkflowRunnerService _runner;
    private readonly WorkflowRunCancellationRegistry _runCancellation;
    private readonly AuditService _audit;
    private readonly TenantRegistryService _tenants;
    private readonly TenantContext _tenant;

    public WorkflowRunsController(
        Data.PlatformDbContext db,
        WorkflowRunnerService runner,
        WorkflowRunCancellationRegistry runCancellation,
        AuditService audit,
        TenantRegistryService tenants,
        TenantContext tenant)
    {
        _db = db;
        _runner = runner;
        _runCancellation = runCancellation;
        _audit = audit;
        _tenants = tenants;
        _tenant = tenant;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow,
            Details: details));

    private static Dictionary<string, string?> ExtractOriginalStepConfigsByKey(string? workflowDefinitionJson)
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(workflowDefinitionJson))
            return map;

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(workflowDefinitionJson);
            if (!doc.RootElement.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != System.Text.Json.JsonValueKind.Array)
                return map;

            var i = 0;
            foreach (var stepEl in stepsEl.EnumerateArray())
            {
                map[$"{i:000}"] = stepEl.GetRawText();
                i += 1;
            }
        }
        catch
        {
            return map;
        }

        return map;
    }

    private static WorkflowRunDetailsDto ToDetails(Models.WorkflowRun run)
    {
        var originalByKey = ExtractOriginalStepConfigsByKey(run.WorkflowDefinition?.DefinitionJson);

        return new WorkflowRunDetailsDto(
            WorkflowRunId: run.WorkflowRunId,
            WorkflowDefinitionId: run.WorkflowDefinitionId,
            State: run.State,
            StartedAtUtc: run.StartedAtUtc,
            FinishedAtUtc: run.FinishedAtUtc,
            TraceId: run.TraceId,
            ErrorCode: run.ErrorCode,
            ErrorMessage: run.ErrorMessage,
            Steps: run.Steps
                .OrderBy(x => x.StepKey)
                .Select(s => new WorkflowStepRunDto(
                    s.WorkflowStepRunId,
                    s.StepKey,
                    s.StepType,
                    originalByKey.TryGetValue(s.StepKey, out var orig) ? orig : null,
                    s.StepConfigJson,
                    s.OutputJson,
                    s.State,
                    s.Attempt,
                    s.LastErrorCode,
                    s.LastErrorMessage,
                    s.LastErrorConfigPath,
                    s.StartedAtUtc,
                    s.FinishedAtUtc)));
    }

    private static WorkflowRunListItemDto ToListItem(Models.WorkflowRun run)
        => new(
            WorkflowRunId: run.WorkflowRunId,
            WorkflowDefinitionId: run.WorkflowDefinitionId,
            State: run.State,
            StartedAtUtc: run.StartedAtUtc,
            FinishedAtUtc: run.FinishedAtUtc,
            TraceId: run.TraceId,
            ErrorCode: run.ErrorCode,
            ErrorMessage: run.ErrorMessage);

    [HttpPost("{id:guid}/runs")]
    public async Task<ActionResult<StartWorkflowRunResponse>> Start([FromRoute] Guid id, CancellationToken ct)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));

        var traceId = TraceIdMiddleware.GetTraceId(HttpContext);

        var run = await _runner.StartAsync(wf, traceId, ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "workflow_run_started",
            target: run.WorkflowRunId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: traceId,
            detailsJson: $"{{\"workflowDefinitionId\":\"{wf.WorkflowDefinitionId}\",\"state\":\"{run.State}\"}}",
            ct: ct);

        return Ok(new StartWorkflowRunResponse(ServerTimeUtc: DateTime.UtcNow, WorkflowRunId: run.WorkflowRunId));
    }

    /// <summary>Lists workflow runs across all definitions for the current tenant (paginated, optional filters).</summary>
    [HttpGet("runs")]
    public async Task<ActionResult<TenantWorkflowRunListResponse>> ListAll(
        [FromQuery] int take = 50,
        [FromQuery] int skip = 0,
        [FromQuery] Guid? workflowDefinitionId = null,
        [FromQuery] string? state = null,
        [FromQuery] DateTime? startedAfterUtc = null,
        [FromQuery] DateTime? startedBeforeUtc = null,
        CancellationToken ct = default)
    {
        if (take <= 0 || take > 200)
            return Problem(
                StatusCodes.Status400BadRequest,
                "take_invalid",
                "take must be between 1 and 200.",
                ErrorDetail.Single("$.take", "take_invalid", "take must be between 1 and 200."));

        if (skip < 0 || skip > 50_000)
            return Problem(
                StatusCodes.Status400BadRequest,
                "skip_invalid",
                "skip must be between 0 and 50000.",
                ErrorDetail.Single("$.skip", "skip_invalid", "skip must be between 0 and 50000."));

        if (startedAfterUtc.HasValue && startedAfterUtc.Value.Kind != DateTimeKind.Utc)
            return Problem(
                StatusCodes.Status400BadRequest,
                "started_after_utc_invalid",
                "startedAfterUtc must be a UTC timestamp (e.g. 2026-03-08T07:00:00Z).",
                ErrorDetail.Single("$.startedAfterUtc", "started_after_utc_invalid", "startedAfterUtc must be UTC."));

        if (startedBeforeUtc.HasValue && startedBeforeUtc.Value.Kind != DateTimeKind.Utc)
            return Problem(
                StatusCodes.Status400BadRequest,
                "started_before_utc_invalid",
                "startedBeforeUtc must be a UTC timestamp (e.g. 2026-03-08T07:00:00Z).",
                ErrorDetail.Single("$.startedBeforeUtc", "started_before_utc_invalid", "startedBeforeUtc must be UTC."));

        if (!string.IsNullOrWhiteSpace(state))
        {
            var s = state.Trim();
            if (!IsKnownWorkflowRunState(s))
                return Problem(
                    StatusCodes.Status400BadRequest,
                    "state_invalid",
                    $"state must be one of: {string.Join(", ", KnownWorkflowRunStates())}.",
                    ErrorDetail.Single("$.state", "state_invalid", "state filter is not a valid workflow run state."));
        }

        var query =
            from r in _db.WorkflowRuns.AsNoTracking()
            join w in _db.WorkflowDefinitions.AsNoTracking() on r.WorkflowDefinitionId equals w.WorkflowDefinitionId
            select new { r, w.Name };

        if (workflowDefinitionId.HasValue)
            query = query.Where(x => x.r.WorkflowDefinitionId == workflowDefinitionId.Value);

        if (!string.IsNullOrWhiteSpace(state))
        {
            var s = state.Trim();
            query = query.Where(x => x.r.State == s);
        }

        if (startedAfterUtc.HasValue)
            query = query.Where(x => x.r.StartedAtUtc >= startedAfterUtc.Value);

        if (startedBeforeUtc.HasValue)
            query = query.Where(x => x.r.StartedAtUtc != null && x.r.StartedAtUtc <= startedBeforeUtc.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.r.StartedAtUtc)
            .ThenByDescending(x => x.r.WorkflowRunId)
            .Skip(skip)
            .Take(take)
            .Select(x => new TenantWorkflowRunListItemDto(
                x.r.WorkflowRunId,
                x.r.WorkflowDefinitionId,
                x.Name,
                x.r.State,
                x.r.StartedAtUtc,
                x.r.FinishedAtUtc,
                x.r.TraceId,
                x.r.ErrorCode,
                x.r.ErrorMessage))
            .ToListAsync(ct);

        return Ok(new TenantWorkflowRunListResponse(ServerTimeUtc: DateTime.UtcNow, Items: items, TotalCount: totalCount));
    }

    private static bool IsKnownWorkflowRunState(string state)
        => KnownWorkflowRunStates().Contains(state, StringComparer.Ordinal);

    private static IEnumerable<string> KnownWorkflowRunStates()
    {
        yield return Models.WorkflowRunStates.Pending;
        yield return Models.WorkflowRunStates.Running;
        yield return Models.WorkflowRunStates.Succeeded;
        yield return Models.WorkflowRunStates.Failed;
        yield return Models.WorkflowRunStates.Canceled;
    }

    [HttpGet("{id:guid}/runs")]
    public async Task<ActionResult<WorkflowRunListResponse>> List([FromRoute] Guid id, CancellationToken ct)
    {
        var exists = await _db.WorkflowDefinitions.AnyAsync(x => x.WorkflowDefinitionId == id, ct);
        if (!exists)
            return Problem(StatusCodes.Status404NotFound, "workflow_not_found", "Workflow not found.");

        var items = await _db.WorkflowRuns
            .Where(x => x.WorkflowDefinitionId == id)
            .OrderByDescending(x => x.StartedAtUtc)
            .ThenByDescending(x => x.WorkflowRunId)
            .Select(x => new WorkflowRunListItemDto(
                x.WorkflowRunId,
                x.WorkflowDefinitionId,
                x.State,
                x.StartedAtUtc,
                x.FinishedAtUtc,
                x.TraceId,
                x.ErrorCode,
                x.ErrorMessage))
            .ToListAsync(ct);

        return Ok(new WorkflowRunListResponse(ServerTimeUtc: DateTime.UtcNow, Items: items));
    }

    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<WorkflowRunDetailsDto>> Get([FromRoute] Guid runId, CancellationToken ct)
    {
        var run = await _db.WorkflowRuns
            .Include(x => x.Steps)
            .Include(x => x.WorkflowDefinition)
            .FirstOrDefaultAsync(x => x.WorkflowRunId == runId, ct);

        if (run is null)
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_run_not_found",
                "Workflow run not found.",
                ErrorDetail.Single("$.workflowRunId", "workflow_run_not_found", "Workflow run not found."));

        return Ok(ToDetails(run));
    }

    /// <summary>Requests cooperative cancellation of an in-process workflow run (HTTP or scheduled).</summary>
    [HttpPost("runs/{runId:guid}/cancel")]
    public async Task<ActionResult<CancelWorkflowRunResponse>> CancelRun([FromRoute] Guid runId, CancellationToken ct)
    {
        var run = await _db.WorkflowRuns.FirstOrDefaultAsync(x => x.WorkflowRunId == runId, ct);
        if (run is null)
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_run_not_found",
                "Workflow run not found.",
                ErrorDetail.Single("$.workflowRunId", "workflow_run_not_found", "Workflow run not found."));

        if (!string.Equals(run.State, Models.WorkflowRunStates.Running, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(run.State, Models.WorkflowRunStates.Pending, StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                StatusCodes.Status409Conflict,
                "workflow_run_not_cancelable",
                "Workflow run is not in a cancelable state.",
                ErrorDetail.Single("$.state", "workflow_run_not_cancelable", "Only pending or running runs can be canceled."));
        }

        if (!_runCancellation.TryCancel(runId))
        {
            await _db.Entry(run).ReloadAsync(ct);
            if (!string.Equals(run.State, Models.WorkflowRunStates.Running, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(run.State, Models.WorkflowRunStates.Pending, StringComparison.OrdinalIgnoreCase))
            {
                return Problem(
                    StatusCodes.Status409Conflict,
                    "workflow_run_not_cancelable",
                    "Workflow run is no longer cancelable.",
                    ErrorDetail.Single("$.state", "workflow_run_not_cancelable", "Run already finished."));
            }

            return Problem(
                StatusCodes.Status503ServiceUnavailable,
                "workflow_run_cancel_unavailable",
                "Cancel signal could not be applied; try again.");
        }

        return Ok(new CancelWorkflowRunResponse(ServerTimeUtc: DateTime.UtcNow));
    }

    private async Task<Guid?> TryResolveTenantIdAsync(CancellationToken ct)
    {
        var t = await _tenants.FindBySlugAsync(_tenant.Slug, ct);
        return t?.TenantId;
    }
}
