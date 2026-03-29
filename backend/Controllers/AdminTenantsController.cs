using LowCodePlatform.Backend.Contracts;
using LowCodePlatform.Backend.Middleware;
using LowCodePlatform.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LowCodePlatform.Backend.Controllers;

[ApiController]
[Route("api/admin/tenants")]
[NoStoreNoCache]
[Authorize(Policy = "admin")]
public sealed class AdminTenantsController : ControllerBase
{
    private readonly TenantMigrationService _migration;
    private readonly TenantRegistryService _registry;

    public AdminTenantsController(TenantMigrationService migration, TenantRegistryService registry)
    {
        _migration = migration;
        _registry = registry;
    }

    private ObjectResult Problem(int statusCode, string errorCode, string message)
        => StatusCode(statusCode, new ErrorResponse(
            ErrorCode: errorCode,
            Message: message,
            TraceId: TraceIdMiddleware.GetTraceId(HttpContext),
            TimestampUtc: DateTime.UtcNow));

    public sealed record CreateTenantRequest(
        string Slug,
        string? ConnectionStringSecretRef,
        string? ConnectionString);

    public sealed record CreateTenantResponse(
        Guid TenantId,
        string Slug,
        string? ConnectionStringSecretRef,
        TenantMigrationResult Migration);

    public sealed record TenantListItemDto(
        Guid TenantId,
        string Slug,
        string? ConnectionStringSecretRef,
        string? ConnectionString,
        bool TenantApiKeyConfigured,
        DateTime CreatedAtUtc);

    public sealed record TenantListResponse(DateTime ServerTimeUtc, List<TenantListItemDto> Items);

    [HttpGet]
    public async Task<ActionResult<TenantListResponse>> List(CancellationToken ct)
    {
        var items = await _registry.ListAsync(ct);

        return Ok(new TenantListResponse(
            ServerTimeUtc: DateTime.UtcNow,
            Items: items
                .Select(x => new TenantListItemDto(
                    x.TenantId,
                    x.Slug,
                    x.ConnectionStringSecretRef,
                    x.ConnectionString,
                    !string.IsNullOrWhiteSpace(x.TenantApiKeySha256Hex),
                    x.CreatedAtUtc))
                .ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<CreateTenantResponse>> Create([FromBody] CreateTenantRequest req, CancellationToken ct)
    {
        try
        {
            var tenant = await _registry.CreateAsync(req.Slug, req.ConnectionStringSecretRef, req.ConnectionString, ct);
            var migration = await _migration.EnsureTenantDatabaseAsync(tenant, ct);

            if (!migration.Succeeded)
                return Problem(StatusCodes.Status500InternalServerError, "tenant_migration_failed", migration.Error ?? "migration_failed");

            return Ok(new CreateTenantResponse(
                TenantId: tenant.TenantId,
                Slug: tenant.Slug,
                ConnectionStringSecretRef: tenant.ConnectionStringSecretRef,
                Migration: migration));
        }
        catch (InvalidOperationException ex) when (
            string.Equals(ex.Message, "tenant_slug_missing", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ex.Message, "tenant_slug_invalid", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ex.Message, "tenant_slug_already_exists", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ex.Message, "tenant_connection_string_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status400BadRequest, ex.Message, ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(StatusCodes.Status500InternalServerError, "tenant_create_failed", ex.Message);
        }
    }

    public sealed record SetTenantApiKeyRequest(string? ApiKey);

    public sealed record TenantApiKeyProvisionedResponse(DateTime ServerTimeUtc, string ApiKey);

    [HttpPost("{slug}/tenant-api-key")]
    public async Task<ActionResult<TenantApiKeyProvisionedResponse>> ProvisionTenantApiKey(
        [FromRoute] string slug,
        [FromBody] SetTenantApiKeyRequest? body,
        CancellationToken ct)
    {
        try
        {
            var key = await _registry.ProvisionTenantApiKeyAsync(slug, body?.ApiKey, ct);
            return Ok(new TenantApiKeyProvisionedResponse(ServerTimeUtc: DateTime.UtcNow, ApiKey: key));
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "tenant_not_found", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, ex.Message, ex.Message);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "tenant_slug_missing", StringComparison.OrdinalIgnoreCase)
                                                   || string.Equals(ex.Message, "tenant_api_key_too_short", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status400BadRequest, ex.Message, ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(StatusCodes.Status500InternalServerError, "tenant_api_key_provision_failed", ex.Message);
        }
    }

    [HttpDelete("{slug}/tenant-api-key")]
    public async Task<ActionResult> ClearTenantApiKey([FromRoute] string slug, CancellationToken ct)
    {
        try
        {
            await _registry.ClearTenantApiKeyAsync(slug, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "tenant_not_found", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status404NotFound, ex.Message, ex.Message);
        }
        catch (InvalidOperationException ex) when (string.Equals(ex.Message, "tenant_slug_missing", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(StatusCodes.Status400BadRequest, ex.Message, ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(StatusCodes.Status500InternalServerError, "tenant_api_key_clear_failed", ex.Message);
        }
    }

    [HttpPost("migrate")]
    public async Task<ActionResult<List<TenantMigrationResult>>> Migrate(CancellationToken ct)
    {
        try
        {
            var res = await _migration.EnsureTenantDatabasesAsync(ct);
            return Ok(res);
        }
        catch (Exception ex)
        {
            return Problem(StatusCodes.Status500InternalServerError, "tenant_migration_failed", ex.Message);
        }
    }
}
