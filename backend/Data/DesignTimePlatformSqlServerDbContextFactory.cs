using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LowCodePlatform.Backend.Data;

/// <summary>
/// Design-time only: <c>dotnet ef migrations add --context PlatformSqlServerDbContext --output-dir Data/Migrations/PlatformSqlServer</c>.
/// Set <c>LCP_PLATFORM_SQLSERVER_DESIGN_TIME_CONNECTION_STRING</c> for your SQL Server / LocalDB; defaults to LocalDB.
/// </summary>
public sealed class DesignTimePlatformSqlServerDbContextFactory : IDesignTimeDbContextFactory<PlatformSqlServerDbContext>
{
    public PlatformSqlServerDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("LCP_PLATFORM_SQLSERVER_DESIGN_TIME_CONNECTION_STRING")
                 ?? "Server=(localdb)\\mssqllocaldb;Database=LcpPlatformSqlServerDesign;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<PlatformSqlServerDbContext>();
        PlatformDatabaseProvider.ConfigurePlatformDbContext(optionsBuilder, cs);
        return new PlatformSqlServerDbContext(optionsBuilder.Options);
    }
}
