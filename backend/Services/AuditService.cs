using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

public sealed class AuditService
{
    private readonly PlatformDbContext _db;

    public AuditService(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(string actor, string action, string target, Guid? installationId, Guid? tenantId, string traceId, string? detailsJson, CancellationToken ct)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            Actor = actor,
            Action = action,
            Target = target,
            InstallationId = installationId,
            TenantId = tenantId,
            TimestampUtc = DateTime.UtcNow,
            TraceId = traceId,
            DetailsJson = detailsJson,
        });

        await _db.SaveChangesAsync(ct);
    }

    public Task<int> CountAsync(CancellationToken ct) => _db.AuditLogs.CountAsync(ct);
}
