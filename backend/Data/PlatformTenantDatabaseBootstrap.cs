using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Data;

/// <summary>
/// Applies tenant platform database schema: SQLite uses EF migrations; SQL Server can use
/// <c>Database.EnsureCreatedAsync</c> when <c>LCP_SQLSERVER_ENSURE_CREATED=1</c> because existing migrations are SQLite-specific.
/// </summary>
public static class PlatformTenantDatabaseBootstrap
{
    /// <summary>Env var: set to <c>1</c> to create SQL Server schema from the model (dev / greenfield) without SQLite migrations.</summary>
    public const string SqlServerEnsureCreatedEnv = "LCP_SQLSERVER_ENSURE_CREATED";

    public static bool UseEnsureCreatedForSqlServer()
        => string.Equals(Environment.GetEnvironmentVariable(SqlServerEnsureCreatedEnv), "1", StringComparison.OrdinalIgnoreCase);

    public static async Task MigrateOrEnsureCreatedAsync(PlatformDbContext db, string connectionString, CancellationToken ct)
    {
        if (!PlatformDatabaseProvider.IsSqlServerConnectionString(connectionString))
        {
            await db.Database.MigrateAsync(ct);
            return;
        }

        if (UseEnsureCreatedForSqlServer())
        {
            await db.Database.EnsureCreatedAsync(ct);
            return;
        }

        await db.Database.MigrateAsync(ct);
    }
}
