using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Data;

/// <summary>
/// SQL Server–specific EF migration chain for the tenant platform model (see <c>Data/Migrations/PlatformSqlServer</c>).
/// Runtime resolution uses <see cref="PlatformDatabaseProvider.CreatePlatformDbContext"/> so controllers keep injecting <see cref="PlatformDbContext"/>.
/// </summary>
public sealed class PlatformSqlServerDbContext : PlatformDbContext
{
    public PlatformSqlServerDbContext(DbContextOptions<PlatformSqlServerDbContext> options) : base(options)
    {
    }
}
