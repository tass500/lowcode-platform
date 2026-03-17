using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/admin/audit")]
[NoStoreNoCache]
public sealed class AdminAuditController : ControllerBase
{
    private readonly Data.PlatformDbContext _db;
    private readonly InstallationService _installation;

    private const int ExportMaxLimit = 10_000;

    public AdminAuditController(Data.PlatformDbContext db, InstallationService installation)
    {
        _db = db;
        _installation = installation;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message, List<ErrorDetail>? details = null)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow,
            Details: details));

    [HttpGet]
    public async Task<ActionResult<AuditListResponse>> List(
        [FromQuery] int take = 50,
        [FromQuery] int skip = 0,
        [FromQuery] string? actor = null,
        [FromQuery] string? actionContains = null,
        [FromQuery] string? traceId = null,
        [FromQuery] DateTime? sinceUtc = null,
        CancellationToken ct = default)
    {
        if (take <= 0 || take > 200)
            return Problem(StatusCodes.Status400BadRequest, "take_invalid", "take must be between 1 and 200.");

        if (skip < 0 || skip > 50_000)
            return Problem(StatusCodes.Status400BadRequest, "skip_invalid", "skip must be between 0 and 50000.");

        if (sinceUtc.HasValue && sinceUtc.Value.Kind != DateTimeKind.Utc)
            return Problem(StatusCodes.Status400BadRequest, "since_utc_invalid", "sinceUtc must be a UTC timestamp (e.g. 2026-03-08T07:00:00Z).");

        Models.Installation inst;
        try
        {
            inst = await _installation.GetDefaultAsync(ct);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "installation_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, "installation_missing", "Installation not found.");
        }

        var q = _db.AuditLogs.AsQueryable();

        q = q.Where(x => x.InstallationId == inst.InstallationId);

        if (!string.IsNullOrWhiteSpace(actor))
            q = q.Where(x => x.Actor == actor);

        if (!string.IsNullOrWhiteSpace(traceId))
            q = q.Where(x => x.TraceId == traceId);

        if (!string.IsNullOrWhiteSpace(actionContains))
            q = q.Where(x => EF.Functions.Like(x.Action, $"%{actionContains}%"));

        if (sinceUtc.HasValue)
            q = q.Where(x => x.TimestampUtc >= sinceUtc.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.TimestampUtc)
            .ThenByDescending(x => x.AuditLogId)
            .Skip(skip)
            .Take(take)
            .Select(x => new AuditLogItemDto(
                x.AuditLogId,
                DateTime.SpecifyKind(x.TimestampUtc, DateTimeKind.Utc),
                x.Actor,
                x.Action,
                x.Target,
                x.TraceId,
                x.DetailsJson))
            .ToListAsync(ct);

        return Ok(new AuditListResponse(ServerTimeUtc: DateTime.UtcNow, Items: items, TotalCount: totalCount));
    }

    [HttpGet("export")]
    public async Task<ActionResult<AuditListResponse>> Export(
        [FromQuery] int max = 5_000,
        [FromQuery] string? actor = null,
        [FromQuery] string? actionContains = null,
        [FromQuery] string? traceId = null,
        [FromQuery] DateTime? sinceUtc = null,
        CancellationToken ct = default)
    {
        if (max <= 0 || max > 50_000)
            return Problem(StatusCodes.Status400BadRequest, "max_invalid", "max must be between 1 and 50000.");

        if (max > ExportMaxLimit)
            return Problem(StatusCodes.Status413PayloadTooLarge, "export_too_large", $"max exceeds server limit ({ExportMaxLimit}). Narrow filters or reduce max.");

        if (sinceUtc.HasValue && sinceUtc.Value.Kind != DateTimeKind.Utc)
            return Problem(StatusCodes.Status400BadRequest, "since_utc_invalid", "sinceUtc must be a UTC timestamp (e.g. 2026-03-08T07:00:00Z).");

        Models.Installation inst;
        try
        {
            inst = await _installation.GetDefaultAsync(ct);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "installation_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, "installation_missing", "Installation not found.");
        }

        var q = _db.AuditLogs.AsQueryable();

        q = q.Where(x => x.InstallationId == inst.InstallationId);

        if (!string.IsNullOrWhiteSpace(actor))
            q = q.Where(x => x.Actor == actor);

        if (!string.IsNullOrWhiteSpace(traceId))
            q = q.Where(x => x.TraceId == traceId);

        if (!string.IsNullOrWhiteSpace(actionContains))
            q = q.Where(x => EF.Functions.Like(x.Action, $"%{actionContains}%"));

        if (sinceUtc.HasValue)
            q = q.Where(x => x.TimestampUtc >= sinceUtc.Value);

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.TimestampUtc)
            .ThenByDescending(x => x.AuditLogId)
            .Take(max)
            .Select(x => new AuditLogItemDto(
                x.AuditLogId,
                DateTime.SpecifyKind(x.TimestampUtc, DateTimeKind.Utc),
                x.Actor,
                x.Action,
                x.Target,
                x.TraceId,
                x.DetailsJson))
            .ToListAsync(ct);

        return Ok(new AuditListResponse(ServerTimeUtc: DateTime.UtcNow, Items: items, TotalCount: totalCount));
    }
}
