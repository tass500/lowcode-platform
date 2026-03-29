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
    private sealed record WorkflowValidationIssue(string? Path, string Code, string Message);

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

    private ObjectResult Problem(int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow,
            Details: details));

    private static List<ErrorDetail> ToErrorDetails(IEnumerable<WorkflowValidationIssue> issues)
        => issues
            .Select(x => new ErrorDetail(
                Path: x.Path,
                Code: x.Code,
                Message: x.Message,
                Severity: "error"))
            .ToList();

    private static List<WorkflowValidationIssue> ValidateContextVarSyntax(string s)
    {
        var issues = new List<WorkflowValidationIssue>();

        if (string.IsNullOrEmpty(s))
            return issues;
        if (!s.Contains("${", StringComparison.Ordinal))
            return issues;

        var idx = 0;
        while (true)
        {
            var start = s.IndexOf("${", idx, StringComparison.Ordinal);
            if (start < 0)
                break;

            var end = s.IndexOf('}', start + 2);
            var endQuote = s.IndexOf('"', start + 2);
            if (end < 0 || (endQuote >= 0 && endQuote < end))
            {
                issues.Add(new WorkflowValidationIssue(
                    Path: "$.definitionJson",
                    Code: "context_var_missing_closing_brace",
                    Message: "Invalid context variable syntax: missing closing '}' in '${...}'."));
                return issues;
            }

            var inner = s.Substring(start + 2, end - (start + 2));
            if (string.IsNullOrWhiteSpace(inner))
            {
                issues.Add(new WorkflowValidationIssue(
                    Path: "$.definitionJson",
                    Code: "context_var_empty_path",
                    Message: "Invalid context variable syntax: empty path in '${...}'."));
                return issues;
            }

            idx = end + 1;
        }

        // Also detect explicit ${} which would otherwise slip through regex-based replacements.
        idx = 0;
        while (true)
        {
            var start = s.IndexOf("${}", idx, StringComparison.Ordinal);
            if (start < 0)
                break;
            issues.Add(new WorkflowValidationIssue(
                Path: "$.definitionJson",
                Code: "context_var_empty_path",
                Message: "Invalid context variable syntax: empty path in '${...}'."));
            return issues;
        }

        return issues;
    }

    private static List<WorkflowValidationIssue> ValidateWorkflowDefinitionSchema(string definitionJson)
    {
        var issues = new List<WorkflowValidationIssue>();

        try
        {
            using var doc = JsonDocument.Parse(definitionJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                issues.Add(new WorkflowValidationIssue(
                    Path: "$",
                    Code: "workflow_definition_root_invalid",
                    Message: "Invalid workflow definition: root must be a JSON object."));
                return issues;
            }

            if (!doc.RootElement.TryGetProperty("steps", out var stepsEl))
            {
                issues.Add(new WorkflowValidationIssue(
                    Path: "$.steps",
                    Code: "workflow_steps_missing",
                    Message: "Invalid workflow definition: 'steps' is required."));
                return issues;
            }

            if (stepsEl.ValueKind != JsonValueKind.Array)
            {
                issues.Add(new WorkflowValidationIssue(
                    Path: "$.steps",
                    Code: "workflow_steps_type_invalid",
                    Message: "Invalid workflow definition: 'steps' must be an array."));
                return issues;
            }

            var idx = 0;
            foreach (var stepEl in stepsEl.EnumerateArray())
            {
                if (stepEl.ValueKind != JsonValueKind.Object)
                {
                    issues.Add(new WorkflowValidationIssue(
                        Path: $"$.steps[{idx}]",
                        Code: "workflow_step_type_invalid",
                        Message: "Invalid workflow definition: each step must be an object."));
                    return issues;
                }

                if (!stepEl.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
                {
                    issues.Add(new WorkflowValidationIssue(
                        Path: $"$.steps[{idx}].type",
                        Code: "workflow_step_type_missing",
                        Message: "Invalid workflow definition: each step must have a string 'type'."));
                    return issues;
                }

                var type = typeEl.GetString();
                if (string.IsNullOrWhiteSpace(type))
                {
                    issues.Add(new WorkflowValidationIssue(
                        Path: $"$.steps[{idx}].type",
                        Code: "workflow_step_type_empty",
                        Message: "Invalid workflow definition: each step must have a non-empty string 'type'."));
                    return issues;
                }

                idx += 1;
            }

            return issues;
        }
        catch (JsonException)
        {
            issues.Add(new WorkflowValidationIssue(
                Path: "$.definitionJson",
                Code: "workflow_definition_json_invalid",
                Message: "Invalid workflow definition: definitionJson must be valid JSON."));
            return issues;
        }
    }

    public sealed record CreateWorkflowRequest(string Name, string DefinitionJson, string? InboundTriggerSecret = null);

    public sealed record UpdateWorkflowRequest(string Name, string DefinitionJson);

    private static WorkflowDefinitionDetailsDto ToDetailsDto(Models.WorkflowDefinition wf)
    {
        var lintWarnings = WorkflowDefinitionLinter.Lint(wf.DefinitionJson);
        return new WorkflowDefinitionDetailsDto(
            wf.WorkflowDefinitionId,
            wf.Name,
            wf.DefinitionJson,
            lintWarnings,
            InboundTriggerConfigured: !string.IsNullOrEmpty(wf.InboundTriggerSecretSha256Hex),
            wf.ScheduleEnabled,
            wf.ScheduleCron,
            wf.ScheduleNextDueUtc,
            wf.CreatedAtUtc,
            wf.UpdatedAtUtc);
    }

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
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));

        return Ok(ToDetailsDto(wf));
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDefinitionDetailsDto>> Create([FromBody] CreateWorkflowRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(
                StatusCodes.Status400BadRequest,
                "name_missing",
                "Name is required.",
                ErrorDetail.Single("$.name", "name_missing", "Name is required."));

        if (string.IsNullOrWhiteSpace(req.DefinitionJson))
            return Problem(
                StatusCodes.Status400BadRequest,
                "definition_missing",
                "DefinitionJson is required.",
                ErrorDetail.Single("$.definitionJson", "definition_missing", "DefinitionJson is required."));

        var schemaIssues = ValidateWorkflowDefinitionSchema(req.DefinitionJson);
        if (schemaIssues.Count > 0)
            return Problem(
                StatusCodes.Status400BadRequest,
                "workflow_definition_invalid",
                schemaIssues[0].Message,
                ToErrorDetails(schemaIssues));

        var syntaxIssues = ValidateContextVarSyntax(req.DefinitionJson);
        if (syntaxIssues.Count > 0)
            return Problem(
                StatusCodes.Status400BadRequest,
                "context_var_syntax_invalid",
                syntaxIssues[0].Message,
                ToErrorDetails(syntaxIssues));

        if (!string.IsNullOrWhiteSpace(req.InboundTriggerSecret))
        {
            if (req.InboundTriggerSecret.Trim().Length < WorkflowInboundSecretHasher.MinSecretLength)
            {
                return Problem(
                    StatusCodes.Status400BadRequest,
                    "inbound_secret_invalid",
                    $"InboundTriggerSecret must be at least {WorkflowInboundSecretHasher.MinSecretLength} characters when provided.",
                    ErrorDetail.Single("$.inboundTriggerSecret", "inbound_secret_invalid", "Inbound secret is too short."));
            }
        }

        var wf = new Models.WorkflowDefinition
        {
            WorkflowDefinitionId = Guid.NewGuid(),
            Name = req.Name.Trim(),
            DefinitionJson = req.DefinitionJson.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        if (!string.IsNullOrWhiteSpace(req.InboundTriggerSecret))
            wf.InboundTriggerSecretSha256Hex = WorkflowInboundSecretHasher.Sha256HexUtf8(req.InboundTriggerSecret.Trim());

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

        return Ok(ToDetailsDto(wf));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkflowDefinitionDetailsDto>> Update([FromRoute] Guid id, [FromBody] UpdateWorkflowRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(
                StatusCodes.Status400BadRequest,
                "name_missing",
                "Name is required.",
                ErrorDetail.Single("$.name", "name_missing", "Name is required."));

        if (string.IsNullOrWhiteSpace(req.DefinitionJson))
            return Problem(
                StatusCodes.Status400BadRequest,
                "definition_missing",
                "DefinitionJson is required.",
                ErrorDetail.Single("$.definitionJson", "definition_missing", "DefinitionJson is required."));

        var schemaIssues = ValidateWorkflowDefinitionSchema(req.DefinitionJson);
        if (schemaIssues.Count > 0)
            return Problem(
                StatusCodes.Status400BadRequest,
                "workflow_definition_invalid",
                schemaIssues[0].Message,
                ToErrorDetails(schemaIssues));

        var syntaxIssues = ValidateContextVarSyntax(req.DefinitionJson);
        if (syntaxIssues.Count > 0)
            return Problem(
                StatusCodes.Status400BadRequest,
                "context_var_syntax_invalid",
                syntaxIssues[0].Message,
                ToErrorDetails(syntaxIssues));

        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));

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

        return Ok(ToDetailsDto(wf));
    }

    private const int WorkflowExportFormatVersion = 1;

    [HttpGet("{id:guid}/export")]
    public async Task<ActionResult<WorkflowDefinitionExportDto>> Export([FromRoute] Guid id, CancellationToken ct)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));

        return Ok(new WorkflowDefinitionExportDto(
            ExportFormatVersion: WorkflowExportFormatVersion,
            Name: wf.Name,
            DefinitionJson: wf.DefinitionJson,
            ExportedAtUtc: DateTime.UtcNow,
            SourceWorkflowDefinitionId: wf.WorkflowDefinitionId));
    }

    [HttpPost("import")]
    public async Task<ActionResult<WorkflowDefinitionDetailsDto>> Import([FromBody] ImportWorkflowRequest req, CancellationToken ct)
    {
        if (req.ExportFormatVersion is not null && req.ExportFormatVersion != WorkflowExportFormatVersion)
        {
            return Problem(
                StatusCodes.Status400BadRequest,
                "workflow_import_format_unsupported",
                $"ExportFormatVersion {req.ExportFormatVersion} is not supported. Use {WorkflowExportFormatVersion} or omit the field for a plain name+definition import.",
                ErrorDetail.Single("$.exportFormatVersion", "workflow_import_format_unsupported", "Unsupported export format version."));
        }

        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(
                StatusCodes.Status400BadRequest,
                "name_missing",
                "Name is required.",
                ErrorDetail.Single("$.name", "name_missing", "Name is required."));

        if (string.IsNullOrWhiteSpace(req.DefinitionJson))
            return Problem(
                StatusCodes.Status400BadRequest,
                "definition_missing",
                "DefinitionJson is required.",
                ErrorDetail.Single("$.definitionJson", "definition_missing", "DefinitionJson is required."));

        var schemaIssues = ValidateWorkflowDefinitionSchema(req.DefinitionJson);
        if (schemaIssues.Count > 0)
            return Problem(
                StatusCodes.Status400BadRequest,
                "workflow_definition_invalid",
                schemaIssues[0].Message,
                ToErrorDetails(schemaIssues));

        var syntaxIssues = ValidateContextVarSyntax(req.DefinitionJson);
        if (syntaxIssues.Count > 0)
            return Problem(
                StatusCodes.Status400BadRequest,
                "context_var_syntax_invalid",
                syntaxIssues[0].Message,
                ToErrorDetails(syntaxIssues));

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
            action: "workflow_imported",
            target: wf.WorkflowDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: null,
            ct: ct);

        return Ok(ToDetailsDto(wf));
    }

    [HttpPut("{id:guid}/schedule")]
    public async Task<ActionResult<WorkflowDefinitionDetailsDto>> SetSchedule([FromRoute] Guid id, [FromBody] SetWorkflowScheduleRequest req, CancellationToken ct)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));

        if (req.Enabled)
        {
            if (string.IsNullOrWhiteSpace(req.Cron))
                return Problem(
                    StatusCodes.Status400BadRequest,
                    "schedule_cron_missing",
                    "Cron is required when schedule is enabled.",
                    ErrorDetail.Single("$.cron", "schedule_cron_missing", "Cron is required when schedule is enabled."));

            if (!WorkflowRestrictedCron.TryParse(req.Cron.Trim(), out var cronError, out var nextFn))
                return Problem(
                    StatusCodes.Status400BadRequest,
                    cronError ?? "schedule_cron_invalid",
                    "Invalid or unsupported schedule cron expression (UTC).",
                    ErrorDetail.Single("$.cron", cronError ?? "schedule_cron_invalid", "Invalid or unsupported schedule cron expression (UTC)."));

            wf.ScheduleEnabled = true;
            wf.ScheduleCron = req.Cron.Trim();
            wf.ScheduleNextDueUtc = nextFn(DateTime.UtcNow);
        }
        else
        {
            wf.ScheduleEnabled = false;
            wf.ScheduleCron = null;
            wf.ScheduleNextDueUtc = null;
        }

        wf.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "workflow_schedule_updated",
            target: wf.WorkflowDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"enabled\":{req.Enabled.ToString().ToLowerInvariant()}}}",
            ct: ct);

        return Ok(ToDetailsDto(wf));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var wf = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == id, ct);
        if (wf is null)
            return Problem(
                StatusCodes.Status404NotFound,
                "workflow_not_found",
                "Workflow not found.",
                ErrorDetail.Single("$.workflowDefinitionId", "workflow_not_found", "Workflow not found."));

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
