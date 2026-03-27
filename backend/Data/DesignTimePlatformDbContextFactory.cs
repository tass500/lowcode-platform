using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LowCodePlatform.Backend.Data;

public sealed class DesignTimePlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("LCP_PLATFORM_DESIGN_TIME_CONNECTION_STRING")
                 ?? "Data Source=tenant-default.db";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        PlatformDatabaseProvider.ConfigurePlatformDbContext(optionsBuilder, cs);

        return new PlatformDbContext(optionsBuilder.Options);
    }
}
