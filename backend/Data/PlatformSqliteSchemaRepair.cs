using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Data;

/// <summary>
/// Dev/test safety valve: some SQLite files were created before <c>upgrade_run</c> existed, or
/// startup <see cref="DatabaseFacade.MigrateAsync"/> was skipped / failed in a way that left
/// tables missing. Idempotent CREATE TABLE IF NOT EXISTS for upgrade tables only.
/// </summary>
public static class PlatformSqliteSchemaRepair
{
    public static async Task EnsureUpgradeTablesExistAsync(PlatformDbContext db, CancellationToken ct)
    {
        var provider = db.Database.ProviderName;
        if (string.IsNullOrEmpty(provider) || !provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            return;

        await db.Database.OpenConnectionAsync(ct);
        try
        {
            var conn = db.Database.GetDbConnection();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='upgrade_run' LIMIT 1;";
                var hasUpgrade = await cmd.ExecuteScalarAsync(ct);
                if (hasUpgrade is not null)
                    return;

                cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name='installation' LIMIT 1;";
                if (await cmd.ExecuteScalarAsync(ct) is null)
                    return;
            }

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "upgrade_run" (
                    "upgrade_run_id" TEXT NOT NULL CONSTRAINT "PK_upgrade_run" PRIMARY KEY,
                    "installation_id" TEXT NOT NULL,
                    "target_version" TEXT NOT NULL,
                    "state" TEXT NOT NULL,
                    "started_at_utc" TEXT NULL,
                    "finished_at_utc" TEXT NULL,
                    "error_code" TEXT NULL,
                    "error_message" TEXT NULL,
                    "trace_id" TEXT NOT NULL,
                    CONSTRAINT "FK_upgrade_run_installation_installation_id" FOREIGN KEY ("installation_id") REFERENCES "installation" ("installation_id") ON DELETE CASCADE
                );
                """,
                ct);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_upgrade_run_installation_id" ON "upgrade_run" ("installation_id");
                """,
                ct);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "upgrade_run_step" (
                    "upgrade_run_step_id" TEXT NOT NULL CONSTRAINT "PK_upgrade_run_step" PRIMARY KEY,
                    "upgrade_run_id" TEXT NOT NULL,
                    "step_key" TEXT NOT NULL,
                    "state" TEXT NOT NULL,
                    "attempt" INTEGER NOT NULL,
                    "next_retry_at_utc" TEXT NULL,
                    "last_error_code" TEXT NULL,
                    "last_error_message" TEXT NULL,
                    "started_at_utc" TEXT NULL,
                    "finished_at_utc" TEXT NULL,
                    CONSTRAINT "FK_upgrade_run_step_upgrade_run_upgrade_run_id" FOREIGN KEY ("upgrade_run_id") REFERENCES "upgrade_run" ("upgrade_run_id") ON DELETE CASCADE
                );
                """,
                ct);

            await db.Database.ExecuteSqlRawAsync(
                """
                CREATE INDEX IF NOT EXISTS "IX_upgrade_run_step_upgrade_run_id" ON "upgrade_run_step" ("upgrade_run_id");
                """,
                ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}
