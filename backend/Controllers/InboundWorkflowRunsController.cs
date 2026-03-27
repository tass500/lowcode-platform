using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

/// <summary>Starts a workflow run without JWT: tenant from host (e.g. <c>t1.localhost</c>) + shared secret header.</summary>
[ApiController]
[Route("api/inbound/workflows")]
[NoStoreNoCache]
[AllowAnonymous]
public sealed class InboundWorkflowRunsController : ControllerBase
{
    public const string InboundSecretHeaderName = "X-Workflow-Inbound-Secret";

    private readonly Data.PlatformDbContext _db;
    private readonly WorkflowRunnerService _runner;
    private readonly AuditService _audit;
    private readonly TenantRegistryService _tenants;
    private readonly TenantContext _tenant;

    public InboundWorkflowRunsController(
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

    private ObjectResult Problem(int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow,
            Details: details));

    [HttpPost("{workflowDefinitionId:guid}/runs")]
    public async Task<ActionResult<StartWorkflowRunResponse>> Start([FromRoute] Guid workflowDefinitionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_tenant.Slug))
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "tenant_not_resolved",
                "Tenant could not be resolved from the request host. Use a host like {tenant}.localhost:PORT.",
                ErrorDetail.Single("$.host", "tenant_not_resolved", "Tenant host is required for inbound workflow runs."));
        }

        if (!Request.Headers.TryGetValue(InboundSecretHeaderName, out var secretValues)
            || string.IsNullOrWhiteSpace(secretValues.ToString()))
        {
            return Problem(
                StatusCodes.Status401Unauthorized,
                "inbound_secret_missing",
                $"Missing or empty header {InboundSecretHeaderName}.",
                ErrorDetail.Single($"$.headers.{InboundSecretHeaderName}", "inbound_secret_missing", "Inbound secret header is required."));
        }

        var providedSecret = secretValues.ToString().Trim();

        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, ct);
        if (wf is null)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));
        }

        if (string.IsNullOrEmpty(wf.InboundTriggerSecretSha256Hex))
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_inbound_not_configured",
                "Inbound trigger is not enabled for this workflow.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_inbound_not_configured", "Configure a secret via PUT /api/workflows/{id}/inbound-trigger."));
        }

        if (!WorkflowInboundSecretHasher.IsValidSecret(providedSecret, wf.InboundTriggerSecretSha256Hex))
        {
            return Problem(
                StatusCodes.Status403Forbidden,
                "inbound_secret_invalid",
                "Inbound secret does not match.",
                ErrorDetail.Single($"$.headers.{InboundSecretHeaderName}", "inbound_secret_invalid", "Secret mismatch."));
        }

        var traceId = TraceIdMiddleware.GetTraceId(HttpContext);
        var run = await _runner.StartAsync(wf, traceId, ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "inbound",
            action: "workflow_run_started",
            target: run.WorkflowRunId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: traceId,
            detailsJson: $"{{\"workflowDefinitionId\":\"{wf.WorkflowDefinitionId}\",\"state\":\"{run.State}\",\"source\":\"inbound\"}}",
            ct: ct);

        return Ok(new StartWorkflowRunResponse(ServerTimeUtc: DateTime.UtcNow, WorkflowRunId: run.WorkflowRunId));
    }

    private async Task<Guid?> TryResolveTenantIdAsync(CancellationToken ct)
    {
        var t = await _tenants.FindBySlugAsync(_tenant.Slug, ct);
        return t?.TenantId;
    }
}
