using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/entities")]
[NoStoreNoCache]
[Authorize(Policy = "tenant_user")]
public sealed class EntityDefinitionsController : ControllerBase
{
    private readonly Data.PlatformDbContext _db;
    private readonly AuditService _audit;
    private readonly TenantRegistryService _tenants;
    private readonly TenantContext _tenant;

    public EntityDefinitionsController(Data.PlatformDbContext db, AuditService audit, TenantRegistryService tenants, TenantContext tenant)
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

    public sealed record CreateEntityRequest(string Name);
    public sealed record UpdateEntityRequest(string Name);

    public sealed record CreateFieldRequest(string Name, string FieldType, bool IsRequired, int? MaxLength);
    public sealed record UpdateFieldRequest(string Name, string FieldType, bool IsRequired, int? MaxLength);

    [HttpGet]
    public async Task<ActionResult<EntityDefinitionListResponse>> List(CancellationToken ct)
    {
        var items = await _db.EntityDefinitions
            .OrderBy(x => x.Name)
            .ThenBy(x => x.EntityDefinitionId)
            .Select(x => new EntityDefinitionListItemDto(
                x.EntityDefinitionId,
                x.Name,
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(ct);

        return Ok(new EntityDefinitionListResponse(ServerTimeUtc: DateTime.UtcNow, Items: items));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EntityDefinitionDetailsDto>> Get([FromRoute] Guid id, CancellationToken ct)
    {
        var entity = await _db.EntityDefinitions
            .Include(x => x.Fields)
            .FirstOrDefaultAsync(x => x.EntityDefinitionId == id, ct);

        if (entity is null)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        return Ok(ToDetailsDto(entity));
    }

    [HttpPost]
    public async Task<ActionResult<EntityDefinitionDetailsDto>> Create([FromBody] CreateEntityRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(StatusCodes.Status400BadRequest, "name_missing", "Name is required.");

        var name = req.Name.Trim();
        if (name.Length > 100)
            return Problem(StatusCodes.Status400BadRequest, "name_too_long", "Name must be at most 100 characters.");

        var exists = await _db.EntityDefinitions.AnyAsync(x => x.Name == name, ct);
        if (exists)
            return Problem(StatusCodes.Status409Conflict, "entity_already_exists", "An entity with the same name already exists.");

        var entity = new Models.EntityDefinition
        {
            EntityDefinitionId = Guid.NewGuid(),
            Name = name,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "entity_definition_created",
            target: entity.EntityDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"name\":\"{entity.Name}\"}}",
            ct: ct);

        return Ok(ToDetailsDto(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<EntityDefinitionDetailsDto>> Update([FromRoute] Guid id, [FromBody] UpdateEntityRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(StatusCodes.Status400BadRequest, "name_missing", "Name is required.");

        var name = req.Name.Trim();
        if (name.Length > 100)
            return Problem(StatusCodes.Status400BadRequest, "name_too_long", "Name must be at most 100 characters.");

        var entity = await _db.EntityDefinitions.Include(x => x.Fields).FirstOrDefaultAsync(x => x.EntityDefinitionId == id, ct);
        if (entity is null)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var exists = await _db.EntityDefinitions.AnyAsync(x => x.EntityDefinitionId != id && x.Name == name, ct);
        if (exists)
            return Problem(StatusCodes.Status409Conflict, "entity_already_exists", "An entity with the same name already exists.");

        entity.Name = name;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "entity_definition_updated",
            target: entity.EntityDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"name\":\"{entity.Name}\"}}",
            ct: ct);

        return Ok(ToDetailsDto(entity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        var entity = await _db.EntityDefinitions.Include(x => x.Fields).FirstOrDefaultAsync(x => x.EntityDefinitionId == id, ct);
        if (entity is null)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        _db.EntityDefinitions.Remove(entity);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "entity_definition_deleted",
            target: id.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: null,
            ct: ct);

        return Ok();
    }

    [HttpPost("{entityId:guid}/fields")]
    public async Task<ActionResult<FieldDefinitionListItemDto>> CreateField([FromRoute] Guid entityId, [FromBody] CreateFieldRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(StatusCodes.Status400BadRequest, "name_missing", "Name is required.");

        if (string.IsNullOrWhiteSpace(req.FieldType))
            return Problem(StatusCodes.Status400BadRequest, "field_type_missing", "FieldType is required.");

        var name = req.Name.Trim();
        var fieldType = req.FieldType.Trim();

        if (name.Length > 100)
            return Problem(StatusCodes.Status400BadRequest, "name_too_long", "Name must be at most 100 characters.");

        if (fieldType.Length > 50)
            return Problem(StatusCodes.Status400BadRequest, "field_type_too_long", "FieldType must be at most 50 characters.");

        if (req.MaxLength.HasValue && req.MaxLength.Value <= 0)
            return Problem(StatusCodes.Status400BadRequest, "max_length_invalid", "MaxLength must be a positive integer.");

        var entity = await _db.EntityDefinitions.Include(x => x.Fields).FirstOrDefaultAsync(x => x.EntityDefinitionId == entityId, ct);
        if (entity is null)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var exists = entity.Fields.Any(x => x.Name == name);
        if (exists)
            return Problem(StatusCodes.Status409Conflict, "field_already_exists", "A field with the same name already exists on this entity.");

        var field = new Models.FieldDefinition
        {
            FieldDefinitionId = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            Name = name,
            FieldType = fieldType,
            IsRequired = req.IsRequired,
            MaxLength = req.MaxLength,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.FieldDefinitions.Add(field);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "field_definition_created",
            target: field.FieldDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"entityDefinitionId\":\"{entityId}\",\"name\":\"{field.Name}\"}}",
            ct: ct);

        return Ok(ToFieldDto(field));
    }

    [HttpPut("{entityId:guid}/fields/{fieldId:guid}")]
    public async Task<ActionResult<FieldDefinitionListItemDto>> UpdateField([FromRoute] Guid entityId, [FromRoute] Guid fieldId, [FromBody] UpdateFieldRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Problem(StatusCodes.Status400BadRequest, "name_missing", "Name is required.");

        if (string.IsNullOrWhiteSpace(req.FieldType))
            return Problem(StatusCodes.Status400BadRequest, "field_type_missing", "FieldType is required.");

        var name = req.Name.Trim();
        var fieldType = req.FieldType.Trim();

        if (name.Length > 100)
            return Problem(StatusCodes.Status400BadRequest, "name_too_long", "Name must be at most 100 characters.");

        if (fieldType.Length > 50)
            return Problem(StatusCodes.Status400BadRequest, "field_type_too_long", "FieldType must be at most 50 characters.");

        if (req.MaxLength.HasValue && req.MaxLength.Value <= 0)
            return Problem(StatusCodes.Status400BadRequest, "max_length_invalid", "MaxLength must be a positive integer.");

        var entity = await _db.EntityDefinitions.Include(x => x.Fields).FirstOrDefaultAsync(x => x.EntityDefinitionId == entityId, ct);
        if (entity is null)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var field = await _db.FieldDefinitions.FirstOrDefaultAsync(x => x.FieldDefinitionId == fieldId && x.EntityDefinitionId == entityId, ct);
        if (field is null)
            return Problem(StatusCodes.Status404NotFound, "field_not_found", "Field not found.");

        var exists = entity.Fields.Any(x => x.FieldDefinitionId != fieldId && x.Name == name);
        if (exists)
            return Problem(StatusCodes.Status409Conflict, "field_already_exists", "A field with the same name already exists on this entity.");

        field.Name = name;
        field.FieldType = fieldType;
        field.IsRequired = req.IsRequired;
        field.MaxLength = req.MaxLength;
        field.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "field_definition_updated",
            target: field.FieldDefinitionId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"entityDefinitionId\":\"{entityId}\",\"name\":\"{field.Name}\"}}",
            ct: ct);

        return Ok(ToFieldDto(field));
    }

    [HttpDelete("{entityId:guid}/fields/{fieldId:guid}")]
    public async Task<ActionResult> DeleteField([FromRoute] Guid entityId, [FromRoute] Guid fieldId, CancellationToken ct)
    {
        var entityExists = await _db.EntityDefinitions.AnyAsync(x => x.EntityDefinitionId == entityId, ct);
        if (!entityExists)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var field = await _db.FieldDefinitions.FirstOrDefaultAsync(x => x.FieldDefinitionId == fieldId && x.EntityDefinitionId == entityId, ct);
        if (field is null)
            return Problem(StatusCodes.Status404NotFound, "field_not_found", "Field not found.");

        _db.FieldDefinitions.Remove(field);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "field_definition_deleted",
            target: fieldId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"entityDefinitionId\":\"{entityId}\"}}",
            ct: ct);

        return Ok();
    }

    private static EntityDefinitionDetailsDto ToDetailsDto(Models.EntityDefinition e)
        => new(
            EntityDefinitionId: e.EntityDefinitionId,
            Name: e.Name,
            CreatedAtUtc: e.CreatedAtUtc,
            UpdatedAtUtc: e.UpdatedAtUtc,
            Fields: e.Fields
                .OrderBy(x => x.Name)
                .ThenBy(x => x.FieldDefinitionId)
                .Select(ToFieldDto)
                .ToList());

    private static FieldDefinitionListItemDto ToFieldDto(Models.FieldDefinition f)
        => new(
            FieldDefinitionId: f.FieldDefinitionId,
            EntityDefinitionId: f.EntityDefinitionId,
            Name: f.Name,
            FieldType: f.FieldType,
            IsRequired: f.IsRequired,
            MaxLength: f.MaxLength,
            CreatedAtUtc: f.CreatedAtUtc,
            UpdatedAtUtc: f.UpdatedAtUtc);

    private async Task<Guid?> TryResolveTenantIdAsync(CancellationToken ct)
    {
        var t = await _tenants.FindBySlugAsync(_tenant.Slug, ct);
        return t?.TenantId;
    }
}
