using LowCodePlatform.Backend.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

/// <summary>
/// Optional integration smoke: requires SQL Server / LocalDB and
/// <c>LCP_TEST_SQLSERVER_MASTER_CONNECTION_STRING</c> (connects to <c>master</c>).
/// Creates a throwaway database, runs <see cref="PlatformSqlServerDbContext"/> migrations, then drops it.
/// Default CI / dev runs without this env → test exits immediately (no-op pass).
/// </summary>
public sealed class PlatformSqlServerMigrationsTests
{
    [Fact]
    public async Task PlatformSqlServer_migrations_apply_to_fresh_database()
    {
        var masterCs = Environment.GetEnvironmentVariable("LCP_TEST_SQLSERVER_MASTER_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(masterCs))
            return;

        var masterBuilder = new SqlConnectionStringBuilder(masterCs)
        {
            InitialCatalog = "master",
        };

        var dbName = $"lcp_smoke_{Guid.NewGuid():N}";
        await using (var conn = new SqlConnection(masterBuilder.ConnectionString))
        {
            await conn.OpenAsync();
            await using var create = conn.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{dbName}]";
            await create.ExecuteNonQueryAsync();
        }

        var tenantBuilder = new SqlConnectionStringBuilder(masterBuilder.ConnectionString)
        {
            InitialCatalog = dbName,
        };

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<PlatformSqlServerDbContext>();
            PlatformDatabaseProvider.ConfigurePlatformDbContext(optionsBuilder, tenantBuilder.ConnectionString);
            await using (var db = new PlatformSqlServerDbContext(optionsBuilder.Options))
            {
                await db.Database.MigrateAsync();
                var applied = await db.Database.GetAppliedMigrationsAsync();
                Assert.NotEmpty(applied);
            }
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await using var conn = new SqlConnection(masterBuilder.ConnectionString);
            await conn.OpenAsync();
            await using var drop = conn.CreateCommand();
            drop.CommandText = $"""
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{dbName}];
                """;
            try
            {
                await drop.ExecuteNonQueryAsync();
            }
            catch
            {
                // Best-effort cleanup; failing smoke should still surface from MigrateAsync/assert.
            }
        }
    }
}
