using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Data;

/// <summary>
/// Chooses SQLite vs SQL Server for the tenant platform connection string.
/// SQLite: <c>Data/Migrations/Platform</c> via <see cref="PlatformDbContext"/>.
/// SQL Server: <c>Data/Migrations/PlatformSqlServer</c> via <see cref="PlatformSqlServerDbContext"/> from <see cref="CreatePlatformDbContext"/>.
/// Optional greenfield: <see cref="PlatformTenantDatabaseBootstrap"/> with <c>LCP_SQLSERVER_ENSURE_CREATED=1</c> uses <c>EnsureCreatedAsync</c>.
/// </summary>
public static class PlatformDatabaseProvider
{
    /// <summary>
    /// Returns true when the connection string targets Microsoft SQL Server (not SQLite file paths).
    /// </summary>
    public static bool IsSqlServerConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var cs = connectionString.Trim();

        // Typical SQL Server keys (ADO.NET / Microsoft.Data.SqlClient)
        if (cs.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
            return true;
        if (cs.Contains("Database=", StringComparison.OrdinalIgnoreCase) && cs.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            return true;
        if (cs.Contains("Server=tcp:", StringComparison.OrdinalIgnoreCase))
            return true;

        // "Data Source=" appears in both SQLite (file) and SQL Server (host). Disambiguate:
        if (cs.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
        {
            var rest = cs["Data Source=".Length..].TrimStart();
            // SQLite: *.db, :memory:, or relative path without server-like tokens
            if (rest.EndsWith(".db", StringComparison.OrdinalIgnoreCase) || rest.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
                return false;
            if (rest.Contains('\\') && !rest.Contains('.')) // Windows path without extension — treat as SQLite path
                return false;
            // host\instance or tcp host,1433
            if (rest.Contains('\\') || rest.Contains(',') || rest.Contains('.'))
                return true;
        }

        return false;
    }

    public static void ConfigurePlatformDbContext(DbContextOptionsBuilder options, string connectionString)
    {
        if (IsSqlServerConnectionString(connectionString))
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
            });
        }
        else
        {
            options.UseSqlite(connectionString);
        }
    }

    /// <summary>
    /// Builds the tenant platform context: <see cref="PlatformSqlServerDbContext"/> for SQL Server (SS migration chain),
    /// <see cref="PlatformDbContext"/> for SQLite.
    /// </summary>
    public static PlatformDbContext CreatePlatformDbContext(IServiceProvider serviceProvider, string connectionString)
    {
        if (IsSqlServerConnectionString(connectionString))
        {
            var builder = new DbContextOptionsBuilder<PlatformSqlServerDbContext>();
            builder.UseApplicationServiceProvider(serviceProvider);
            ConfigurePlatformDbContext(builder, connectionString);
            return new PlatformSqlServerDbContext(builder.Options);
        }

        var sqliteBuilder = new DbContextOptionsBuilder<PlatformDbContext>();
        sqliteBuilder.UseApplicationServiceProvider(serviceProvider);
        ConfigurePlatformDbContext(sqliteBuilder, connectionString);
        return new PlatformDbContext(sqliteBuilder.Options);
    }
}
