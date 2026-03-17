using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

public sealed class InstallationService
{
    private readonly PlatformDbContext _db;

    public InstallationService(PlatformDbContext db)
    {
        _db = db;
    }

    public async Task<Installation> EnsureDefaultInstallationAsync(CancellationToken ct)
    {
        var inst = await _db.Installations.OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (inst is not null)
            return inst;

        inst = new Installation
        {
            InstallationId = Guid.NewGuid(),
            ReleaseChannel = "stable",
            CurrentVersion = "0.1.0",
            SupportedVersion = "0.1.0",
            ReleaseDateUtc = DateTime.UtcNow,
            UpgradeWindowDays = 60,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.Installations.Add(inst);
        await _db.SaveChangesAsync(ct);
        return inst;
    }

    public async Task<Installation> GetDefaultAsync(CancellationToken ct)
    {
        var inst = await _db.Installations.OrderBy(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (inst is null)
            throw new InvalidOperationException("installation_missing");
        return inst;
    }
}
