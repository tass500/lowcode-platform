using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Data;

public sealed class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
    {
    }

    public DbSet<Installation> Installations => Set<Installation>();
    public DbSet<UpgradeRun> UpgradeRuns => Set<UpgradeRun>();
    public DbSet<UpgradeRunStep> UpgradeRunSteps => Set<UpgradeRunStep>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Installation>(e =>
        {
            e.ToTable("installation");
            e.HasKey(x => x.InstallationId);
            e.Property(x => x.InstallationId).HasColumnName("installation_id");
            e.Property(x => x.ReleaseChannel).HasColumnName("release_channel");
            e.Property(x => x.CurrentVersion).HasColumnName("current_version");
            e.Property(x => x.SupportedVersion).HasColumnName("supported_version");
            e.Property(x => x.ReleaseDateUtc).HasColumnName("release_date_utc");
            e.Property(x => x.UpgradeWindowDays).HasColumnName("upgrade_window_days");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<UpgradeRun>(e =>
        {
            e.ToTable("upgrade_run");
            e.HasKey(x => x.UpgradeRunId);
            e.Property(x => x.UpgradeRunId).HasColumnName("upgrade_run_id");
            e.Property(x => x.InstallationId).HasColumnName("installation_id");
            e.Property(x => x.TargetVersion).HasColumnName("target_version");
            e.Property(x => x.State).HasColumnName("state");
            e.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
            e.Property(x => x.FinishedAtUtc).HasColumnName("finished_at_utc");
            e.Property(x => x.ErrorCode).HasColumnName("error_code");
            e.Property(x => x.ErrorMessage).HasColumnName("error_message");
            e.Property(x => x.TraceId).HasColumnName("trace_id");

            e.HasMany(x => x.Steps)
                .WithOne(x => x.UpgradeRun)
                .HasForeignKey(x => x.UpgradeRunId);

            e.HasOne(x => x.Installation)
                .WithMany()
                .HasForeignKey(x => x.InstallationId);
        });

        modelBuilder.Entity<UpgradeRunStep>(e =>
        {
            e.ToTable("upgrade_run_step");
            e.HasKey(x => x.UpgradeRunStepId);
            e.Property(x => x.UpgradeRunStepId).HasColumnName("upgrade_run_step_id");
            e.Property(x => x.UpgradeRunId).HasColumnName("upgrade_run_id");
            e.Property(x => x.StepKey).HasColumnName("step_key");
            e.Property(x => x.State).HasColumnName("state");
            e.Property(x => x.Attempt).HasColumnName("attempt");
            e.Property(x => x.NextRetryAtUtc).HasColumnName("next_retry_at_utc");
            e.Property(x => x.LastErrorCode).HasColumnName("last_error_code");
            e.Property(x => x.LastErrorMessage).HasColumnName("last_error_message");
            e.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
            e.Property(x => x.FinishedAtUtc).HasColumnName("finished_at_utc");
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_log");
            e.HasKey(x => x.AuditLogId);
            e.Property(x => x.AuditLogId).HasColumnName("audit_log_id");
            e.Property(x => x.Actor).HasColumnName("actor");
            e.Property(x => x.Action).HasColumnName("action");
            e.Property(x => x.Target).HasColumnName("target");
            e.Property(x => x.InstallationId).HasColumnName("installation_id");
            e.Property(x => x.TenantId).HasColumnName("tenant_id");
            e.Property(x => x.TimestampUtc).HasColumnName("timestamp_utc");
            e.Property(x => x.TraceId).HasColumnName("trace_id");
            e.Property(x => x.DetailsJson).HasColumnName("details_json");
        });
    }
}
