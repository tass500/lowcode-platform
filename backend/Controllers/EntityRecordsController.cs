using System.Text.Json;
using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/entities/{entityId:guid}/records")]
[NoStoreNoCache]
[Authorize(Policy = "tenant_user")]
public sealed class EntityRecordsController : ControllerBase
{
    private readonly Data.PlatformDbContext _db;
    private readonly AuditService _audit;
    private readonly TenantRegistryService _tenants;
    private readonly TenantContext _tenant;

    public EntityRecordsController(Data.PlatformDbContext db, AuditService audit, TenantRegistryService tenants, TenantContext tenant)
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

    public sealed record UpsertEntityRecordRequest(string DataJson);

    private static EntityRecordDetailsDto ToDetails(Models.EntityRecord r)
        => new(
            EntityRecordId: r.EntityRecordId,
            EntityDefinitionId: r.EntityDefinitionId,
            CreatedAtUtc: r.CreatedAtUtc,
            UpdatedAtUtc: r.UpdatedAtUtc,
            DataJson: r.DataJson);

    private static EntityRecordListItemDto ToListItem(Models.EntityRecord r)
        => new(
            EntityRecordId: r.EntityRecordId,
            EntityDefinitionId: r.EntityDefinitionId,
            CreatedAtUtc: r.CreatedAtUtc,
            UpdatedAtUtc: r.UpdatedAtUtc,
            DataJson: r.DataJson);

    private static bool IsValidJsonObject(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet]
    public async Task<ActionResult<EntityRecordListResponse>> List([FromRoute] Guid entityId, CancellationToken ct)
    {
        var exists = await _db.EntityDefinitions.AnyAsync(x => x.EntityDefinitionId == entityId, ct);
        if (!exists)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var items = await _db.EntityRecords
            .Where(x => x.EntityDefinitionId == entityId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenByDescending(x => x.EntityRecordId)
            .Select(x => new EntityRecordListItemDto(
                x.EntityRecordId,
                x.EntityDefinitionId,
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.DataJson))
            .ToListAsync(ct);

        return Ok(new EntityRecordListResponse(ServerTimeUtc: DateTime.UtcNow, Items: items));
    }

    [HttpGet("{recordId:guid}")]
    public async Task<ActionResult<EntityRecordDetailsDto>> Get([FromRoute] Guid entityId, [FromRoute] Guid recordId, CancellationToken ct)
    {
        var record = await _db.EntityRecords
            .FirstOrDefaultAsync(x => x.EntityDefinitionId == entityId && x.EntityRecordId == recordId, ct);

        if (record is null)
        {
            var entityExists = await _db.EntityDefinitions.AnyAsync(x => x.EntityDefinitionId == entityId, ct);
            if (!entityExists)
                return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

            return Problem(StatusCodes.Status404NotFound, "record_not_found", "Record not found.");
        }

        return Ok(ToDetails(record));
    }

    [HttpPost]
    public async Task<ActionResult<EntityRecordDetailsDto>> Create([FromRoute] Guid entityId, [FromBody] UpsertEntityRecordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.DataJson))
            return Problem(StatusCodes.Status400BadRequest, "data_missing", "DataJson is required.");

        var dataJson = req.DataJson.Trim();
        if (!IsValidJsonObject(dataJson))
            return Problem(StatusCodes.Status400BadRequest, "data_invalid", "DataJson must be a JSON object.");

        var entityExists = await _db.EntityDefinitions.AnyAsync(x => x.EntityDefinitionId == entityId, ct);
        if (!entityExists)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var record = new Models.EntityRecord
        {
            EntityRecordId = Guid.NewGuid(),
            EntityDefinitionId = entityId,
            DataJson = dataJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.EntityRecords.Add(record);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "entity_record_created",
            target: record.EntityRecordId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"entityDefinitionId\":\"{entityId}\"}}",
            ct: ct);

        return Ok(ToDetails(record));
    }

    [HttpPut("{recordId:guid}")]
    public async Task<ActionResult<EntityRecordDetailsDto>> Update([FromRoute] Guid entityId, [FromRoute] Guid recordId, [FromBody] UpsertEntityRecordRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.DataJson))
            return Problem(StatusCodes.Status400BadRequest, "data_missing", "DataJson is required.");

        var dataJson = req.DataJson.Trim();
        if (!IsValidJsonObject(dataJson))
            return Problem(StatusCodes.Status400BadRequest, "data_invalid", "DataJson must be a JSON object.");

        var entityExists = await _db.EntityDefinitions.AnyAsync(x => x.EntityDefinitionId == entityId, ct);
        if (!entityExists)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var record = await _db.EntityRecords
            .FirstOrDefaultAsync(x => x.EntityDefinitionId == entityId && x.EntityRecordId == recordId, ct);

        if (record is null)
            return Problem(StatusCodes.Status404NotFound, "record_not_found", "Record not found.");

        record.DataJson = dataJson;
        record.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "entity_record_updated",
            target: record.EntityRecordId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"entityDefinitionId\":\"{entityId}\"}}",
            ct: ct);

        return Ok(ToDetails(record));
    }

    [HttpDelete("{recordId:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid entityId, [FromRoute] Guid recordId, CancellationToken ct)
    {
        var entityExists = await _db.EntityDefinitions.AnyAsync(x => x.EntityDefinitionId == entityId, ct);
        if (!entityExists)
            return Problem(StatusCodes.Status404NotFound, "entity_not_found", "Entity not found.");

        var record = await _db.EntityRecords
            .FirstOrDefaultAsync(x => x.EntityDefinitionId == entityId && x.EntityRecordId == recordId, ct);

        if (record is null)
            return Problem(StatusCodes.Status404NotFound, "record_not_found", "Record not found.");

        _db.EntityRecords.Remove(record);
        await _db.SaveChangesAsync(ct);

        var tenantId = await TryResolveTenantIdAsync(ct);
        await _audit.WriteAsync(
            actor: "system",
            action: "entity_record_deleted",
            target: recordId.ToString(),
            installationId: null,
            tenantId: tenantId,
            traceId: TraceIdMiddleware.GetTraceId(HttpContext),
            detailsJson: $"{{\"entityDefinitionId\":\"{entityId}\"}}",
            ct: ct);

        return Ok();
    }

    private async Task<Guid?> TryResolveTenantIdAsync(CancellationToken ct)
    {
        var t = await _tenants.FindBySlugAsync(_tenant.Slug, ct);
        return t?.TenantId;
    }
}
