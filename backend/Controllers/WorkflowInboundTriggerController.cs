using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/workflows/{workflowDefinitionId:guid}/inbound-trigger")]
[NoStoreNoCache]
[Authorize(Policy = "tenant_user")]
public sealed class WorkflowInboundTriggerController : ControllerBase
{
    private readonly Data.PlatformDbContext _db;
    private readonly AuditService _audit;
    private readonly TenantRegistryService _tenants;
    private readonly TenantContext _tenant;

    public WorkflowInboundTriggerController(
        Data.PlatformDbContext db,
        AuditService audit,
        TenantRegistryService tenants,
        TenantContext tenant)
    {
        _db = db;
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

    [HttpPut]
    public async Task<ActionResult<WorkflowInboundTriggerStatusDto>> Put(
        [FromRoute] Guid workflowDefinitionId,
        [FromBody] SetWorkflowInboundTriggerRequest req,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Secret) || req.Secret.Trim().Length < WorkflowInboundSecretHasher.MinSecretLength)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "inbound_secret_invalid",
                $"Secret must be at least {WorkflowInboundSecretHasher.MinSecretLength} characters.",
                ErrorDetail.Single("$.secret", "inbound_secret_invalid", "Secret is missing or too short."));
        }

        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, ct);
        if (wf is null)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));
        }

        wf.InboundTriggerSecretSha256Hex = WorkflowInboundSecretHasher.Sha256HexUtf8(req.Secret.Trim());
        wf.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "user",
            action: "workflow_inbound_trigger_set",
            target: wf.WorkflowDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: null,
            ct: ct);

        return Ok(new WorkflowInboundTriggerStatusDto(InboundTriggerConfigured: true, ServerTimeUtc: DateTime.UtcNow));
    }

    [HttpDelete]
    public async Task<ActionResult<WorkflowInboundTriggerStatusDto>> Delete([FromRoute] Guid workflowDefinitionId, CancellationToken ct)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, ct);
        if (wf is null)
        {
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));
        }

        wf.InboundTriggerSecretSha256Hex = null;
        wf.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "user",
            action: "workflow_inbound_trigger_cleared",
            target: wf.WorkflowDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: null,
            ct: ct);

        return Ok(new WorkflowInboundTriggerStatusDto(InboundTriggerConfigured: false, ServerTimeUtc: DateTime.UtcNow));
    }

    private async Task<Guid?> TryResolveTenantIdAsync(CancellationToken ct)
    {
        var t = await _tenants.FindBySlugAsync(_tenant.Slug, ct);
        return t?.TenantId;
    }
}
