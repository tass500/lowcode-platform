using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LowCodePlatform.Backend.Data;

public sealed class DesignTimePlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var cs = "Data Source=tenant-default.db";

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlite(cs)
            .Options;

        return new PlatformDbContext(options);
    }
}
