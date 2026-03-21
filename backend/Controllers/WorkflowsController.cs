using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/workflows")]
[NoStoreNoCache]
[Authorize(Policy = "tenant_user")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly Data.PlatformDbContext _db;
    private readonly AuditService _audit;
    private readonly TenantRegistryService _tenants;
    private readonly TenantContext _tenant;

    public WorkflowsController(Data.PlatformDbContext db, AuditService audit, TenantRegistryService tenants, TenantContext tenant)
    {
        _db = db;
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

    private static string? ValidateContextVarSyntax(string s)
    {
        if (string.IsNullOrEmpty(s))
            return null;
        if (!s.Contains("${", StringComparison.Ordinal))
            return null;

        var idx = 0;
        while (true)
        {
            var start = s.IndexOf("${", idx, StringComparison.Ordinal);
            if (start < 0)
                break;

            var end = s.IndexOf('}', start + 2);
            var endQuote = s.IndexOf('"', start + 2);
            if (end < 0 || (endQuote >= 0 && endQuote < end))
                return "Invalid context variable syntax: missing closing '}' in '${...}'.";

            var inner = s.Substring(start + 2, end - (start + 2));
            if (string.IsNullOrWhiteSpace(inner))
                return "Invalid context variable syntax: empty path in '${...}'.";

            idx = end + 1;
        }

        // Also detect explicit ${} which would otherwise slip through regex-based replacements.
        idx = 0;
        while (true)
        {
            var start = s.IndexOf("${}", idx, StringComparison.Ordinal);
            if (start < 0)
                break;
            return "Invalid context variable syntax: empty path in '${...}'.";
        }

        return null;
    }

    private static string? ValidateWorkflowDefinitionSchema(string definitionJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(definitionJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return "Invalid workflow definition: root must be a JSON object.";

            if (!doc.RootElement.TryGetProperty("steps", out var stepsEl))
                return "Invalid workflow definition: 'steps' is required.";

            if (stepsEl.ValueKind != JsonValueKind.Array)
                return "Invalid workflow definition: 'steps' must be an array.";

            foreach (var stepEl in stepsEl.EnumerateArray())
            {
                if (stepEl.ValueKind != JsonValueKind.Object)
                    return "Invalid workflow definition: each step must be an object.";

                if (!stepEl.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
                    return "Invalid workflow definition: each step must have a string 'type'.";

                var type = typeEl.GetString();
                if (string.IsNullOrWhiteSpace(type))
                    return "Invalid workflow definition: each step must have a non-empty string 'type'.";
            }

            return null;
        }
        catch (JsonException)
        {
            return "Invalid workflow definition: definitionJson must be valid JSON.";
        }
    }

    public sealed record CreateWorkflowRequest(string Name, string DefinitionJson);

    public sealed record UpdateWorkflowRequest(string Name, string DefinitionJson);

    [HttpGet]
    public async Task<ActionResult<WorkflowListResponse>> List(CancellationToken ct)
    {
        var items = await _db.WorkflowDefinitions
            .OrderBy(x => x.Name)
            .ThenBy(x => x.WorkflowDefinitionId)
            .Select(x => new WorkflowDefinitionListItemDto(
                x.WorkflowDefinitionId,
                x.Name,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(ct);

        return Ok(new WorkflowListResponse(ServerTimeUtc: DateTime.UtcNow, Items: items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowDefinitionDetailsDto>> Get([FromRoute] Guid id, CancellationToken ct)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(StatusCodes.Status404NotFound, "workflow_not_found", "Workflow not found.");

        return Ok(new WorkflowDefinitionDetailsDto(
            wf.WorkflowDefinitionId,
            wf.Name,
            wf.DefinitionJson,
            wf.CreatedAtUtc,
            wf.UpdatedAtUtc));
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDefinitionDetailsDto>> Create([FromBody] CreateWorkflowRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(StatusCodes.Status400BadRequest, "name_missing", "Name is required.");

        if (string.IsNullOrWhiteSpace(req.DefinitionJson))
            return Problem(StatusCodes.Status400BadRequest, "definition_missing", "DefinitionJson is required.");

        var schemaError = ValidateWorkflowDefinitionSchema(req.DefinitionJson);
        if (!string.IsNullOrWhiteSpace(schemaError))
            return Problem(StatusCodes.Status400BadRequest, "workflow_definition_invalid", schemaError);

        var syntaxError = ValidateContextVarSyntax(req.DefinitionJson);
        if (!string.IsNullOrWhiteSpace(syntaxError))
            return Problem(StatusCodes.Status400BadRequest, "context_var_syntax_invalid", syntaxError);

        var wf = new Models.WorkflowDefinition
        {
            WorkflowDefinitionId = Guid.NewGuid(),
            Name = req.Name.Trim(),
            DefinitionJson = req.DefinitionJson.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.WorkflowDefinitions.Add(wf);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "workflow_created",
            target: wf.WorkflowDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: null,
            ct: ct);

        return Ok(new WorkflowDefinitionDetailsDto(
            wf.WorkflowDefinitionId,
            wf.Name,
            wf.DefinitionJson,
            wf.CreatedAtUtc,
            wf.UpdatedAtUtc));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkflowDefinitionDetailsDto>> Update([FromRoute] Guid id, [FromBody] UpdateWorkflowRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(StatusCodes.Status400BadRequest, "name_missing", "Name is required.");

        if (string.IsNullOrWhiteSpace(req.DefinitionJson))
            return Problem(StatusCodes.Status400BadRequest, "definition_missing", "DefinitionJson is required.");

        var schemaError = ValidateWorkflowDefinitionSchema(req.DefinitionJson);
        if (!string.IsNullOrWhiteSpace(schemaError))
            return Problem(StatusCodes.Status400BadRequest, "workflow_definition_invalid", schemaError);

        var syntaxError = ValidateContextVarSyntax(req.DefinitionJson);
        if (!string.IsNullOrWhiteSpace(syntaxError))
            return Problem(StatusCodes.Status400BadRequest, "context_var_syntax_invalid", syntaxError);

        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(StatusCodes.Status404NotFound, "workflow_not_found", "Workflow not found.");

        wf.Name = req.Name.Trim();
        wf.DefinitionJson = req.DefinitionJson.Trim();
        wf.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "workflow_updated",
            target: wf.WorkflowDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: null,
            ct: ct);

        return Ok(new WorkflowDefinitionDetailsDto(
            wf.WorkflowDefinitionId,
            wf.Name,
            wf.DefinitionJson,
            wf.CreatedAtUtc,
            wf.UpdatedAtUtc));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(StatusCodes.Status404NotFound, "workflow_not_found", "Workflow not found.");

        _db.WorkflowDefinitions.Remove(wf);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "workflow_deleted",
            target: id.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: null,
            ct: ct);

        return Ok();
    }

    private async Task<Guid?> TryResolveTenantIdAsync(CancellationToken ct)
    {
        var t = await _tenants.FindBySlugAsync(_tenant.Slug, ct);
        return t?.TenantId;
    }
}
