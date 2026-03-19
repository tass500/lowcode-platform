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
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<WorkflowStepRun> WorkflowStepRuns => Set<WorkflowStepRun>();
    public DbSet<EntityDefinition> EntityDefinitions => Set<EntityDefinition>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<EntityRecord> EntityRecords => Set<EntityRecord>();

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

        modelBuilder.Entity<WorkflowDefinition>(e =>
        {
            e.ToTable("workflow_definition");
            e.HasKey(x => x.WorkflowDefinitionId);
            e.Property(x => x.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.DefinitionJson).HasColumnName("definition_json");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            e.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<WorkflowRun>(e =>
        {
            e.ToTable("workflow_run");
            e.HasKey(x => x.WorkflowRunId);
            e.Property(x => x.WorkflowRunId).HasColumnName("workflow_run_id");
            e.Property(x => x.WorkflowDefinitionId).HasColumnName("workflow_definition_id");
            e.Property(x => x.State).HasColumnName("state");
            e.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
            e.Property(x => x.FinishedAtUtc).HasColumnName("finished_at_utc");
            e.Property(x => x.TraceId).HasColumnName("trace_id");
            e.Property(x => x.ErrorCode).HasColumnName("error_code");
            e.Property(x => x.ErrorMessage).HasColumnName("error_message");

            e.HasOne(x => x.WorkflowDefinition)
                .WithMany()
                .HasForeignKey(x => x.WorkflowDefinitionId);

            e.HasMany(x => x.Steps)
                .WithOne(x => x.WorkflowRun)
                .HasForeignKey(x => x.WorkflowRunId);

            e.HasIndex(x => x.WorkflowDefinitionId);
            e.HasIndex(x => x.State);
        });

        modelBuilder.Entity<WorkflowStepRun>(e =>
        {
            e.ToTable("workflow_step_run");
            e.HasKey(x => x.WorkflowStepRunId);
            e.Property(x => x.WorkflowStepRunId).HasColumnName("workflow_step_run_id");
            e.Property(x => x.WorkflowRunId).HasColumnName("workflow_run_id");
            e.Property(x => x.StepKey).HasColumnName("step_key");
            e.Property(x => x.StepType).HasColumnName("step_type");
            e.Property(x => x.StepConfigJson).HasColumnName("step_config_json");
            e.Property(x => x.State).HasColumnName("state");
            e.Property(x => x.Attempt).HasColumnName("attempt");
            e.Property(x => x.LastErrorCode).HasColumnName("last_error_code");
            e.Property(x => x.LastErrorMessage).HasColumnName("last_error_message");
            e.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
            e.Property(x => x.FinishedAtUtc).HasColumnName("finished_at_utc");

            e.HasIndex(x => x.WorkflowRunId);
            e.HasIndex(x => x.State);
        });

        modelBuilder.Entity<EntityDefinition>(e =>
        {
            e.ToTable("entity_definition");
            e.HasKey(x => x.EntityDefinitionId);
            e.Property(x => x.EntityDefinitionId).HasColumnName("entity_definition_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            e.HasMany(x => x.Fields)
                .WithOne(x => x.Entity)
                .HasForeignKey(x => x.EntityDefinitionId);

            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<FieldDefinition>(e =>
        {
            e.ToTable("field_definition");
            e.HasKey(x => x.FieldDefinitionId);
            e.Property(x => x.FieldDefinitionId).HasColumnName("field_definition_id");
            e.Property(x => x.EntityDefinitionId).HasColumnName("entity_definition_id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.FieldType).HasColumnName("field_type");
            e.Property(x => x.IsRequired).HasColumnName("is_required");
            e.Property(x => x.MaxLength).HasColumnName("max_length");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            e.HasIndex(x => x.EntityDefinitionId);
            e.HasIndex(x => new { x.EntityDefinitionId, x.Name }).IsUnique();
        });

        modelBuilder.Entity<EntityRecord>(e =>
        {
            e.ToTable("entity_record");
            e.HasKey(x => x.EntityRecordId);
            e.Property(x => x.EntityRecordId).HasColumnName("entity_record_id");
            e.Property(x => x.EntityDefinitionId).HasColumnName("entity_definition_id");
            e.Property(x => x.DataJson).HasColumnName("data_json");
            e.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

            e.HasOne(x => x.EntityDefinition)
                .WithMany()
                .HasForeignKey(x => x.EntityDefinitionId);

            e.HasIndex(x => x.EntityDefinitionId);
            e.HasIndex(x => x.UpdatedAtUtc);
        });
    }
}
