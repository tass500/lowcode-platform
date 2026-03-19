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
    private readonly AuditService _audit;
    private readonly TenantRegistryService _tenants;
    private readonly TenantContext _tenant;

    public WorkflowRunsController(
        Data.PlatformDbContext db,
        WorkflowRunnerService runner,
        AuditService audit,
        TenantRegistryService tenants,
        TenantContext tenant)
    {
        _db = db;
        _runner = runner;
        _audit = audit;
        _tenants = tenants;
        _tenant = tenant;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow));

    private static WorkflowRunDetailsDto ToDetails(Models.WorkflowRun run)
        => new(
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
                    s.StepConfigJson,
                    s.OutputJson,
                    s.State,
                    s.Attempt,
                    s.LastErrorCode,
                    s.LastErrorMessage,
                    s.StartedAtUtc,
                    s.FinishedAtUtc)));

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
            return Problem(StatusCodes.Status404NotFound, "workflow_not_found", "Workflow not found.");

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
            .FirstOrDefaultAsync(x => x.WorkflowRunId == runId, ct);

        if (run is null)
            return Problem(StatusCodes.Status404NotFound, "workflow_run_not_found", "Workflow run not found.");

        return Ok(ToDetails(run));
    }

    private async Task<Guid?> TryResolveTenantIdAsync(CancellationToken ct)
    {
        var t = await _tenants.FindBySlugAsync(_tenant.Slug, ct);
        return t?.TenantId;
    }
}
