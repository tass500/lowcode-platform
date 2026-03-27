using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

public sealed class UpgradeOrchestratorHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<UpgradeOrchestratorHostedService> _logger;
    private readonly DevUpgradeFaults _devFaults;

    public UpgradeOrchestratorHostedService(IServiceProvider sp, ILogger<UpgradeOrchestratorHostedService> logger, DevUpgradeFaults devFaults)
    {
        _sp = sp;
        _logger = logger;
        _devFaults = devFaults;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Covers LCP_SKIP_STARTUP_SEED=1 or any startup path that skipped tenant MigrateAsync.
        try
        {
            using var bootScope = _sp.CreateScope();
            var bootDb = bootScope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var bootCs = bootDb.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(bootCs))
                _logger.LogWarning("upgrade_orchestrator_boot_no_connection_string");
            else
            {
                await PlatformTenantDatabaseBootstrap.MigrateOrEnsureCreatedAsync(bootDb, bootCs, stoppingToken);
                await PlatformSqliteSchemaRepair.EnsureUpgradeTablesExistAsync(bootDb, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "upgrade_orchestrator_startup_migrate_failed");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "upgrade_orchestrator_tick_failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var audit = scope.ServiceProvider.GetRequiredService<AuditService>();

        var run = await db.UpgradeRuns
            .Include(x => x.Steps)
            .Where(x => x.State == UpgradeRunStates.Pending || x.State == UpgradeRunStates.Running)
            .OrderBy(x => x.StartedAtUtc ?? DateTime.MaxValue)
            .FirstOrDefaultAsync(ct);

        if (run is null)
            return;

        if (run.State == UpgradeRunStates.Pending)
        {
            run.State = UpgradeRunStates.Running;
            run.StartedAtUtc ??= DateTime.UtcNow;
            await audit.WriteAsync("system", "upgrade_run_started", run.UpgradeRunId.ToString(), run.InstallationId, null, run.TraceId, null, ct);
        }

        var nextStep = run.Steps
            .OrderBy(x => x.StepKey)
            .FirstOrDefault(x => x.State is UpgradeRunStepStates.Pending or UpgradeRunStepStates.Failed);

        if (nextStep is null)
        {
            run.State = UpgradeRunStates.Succeeded;
            run.FinishedAtUtc = DateTime.UtcNow;
            await audit.WriteAsync("system", "upgrade_run_succeeded", run.UpgradeRunId.ToString(), run.InstallationId, null, run.TraceId, null, ct);
            await db.SaveChangesAsync(ct);
            return;
        }

        if (nextStep.State == UpgradeRunStepStates.Failed && nextStep.NextRetryAtUtc.HasValue && nextStep.NextRetryAtUtc.Value > DateTime.UtcNow)
            return;

        if (run.State == UpgradeRunStates.Canceled)
            return;

        await ExecuteStepAsync(db, audit, run, nextStep, _devFaults, ct);

        if (run.Steps.All(x => x.State == UpgradeRunStepStates.Succeeded))
        {
            run.State = UpgradeRunStates.Succeeded;
            run.FinishedAtUtc = DateTime.UtcNow;
            await audit.WriteAsync("system", "upgrade_run_succeeded", run.UpgradeRunId.ToString(), run.InstallationId, null, run.TraceId, null, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task ExecuteStepAsync(PlatformDbContext db, AuditService audit, UpgradeRun run, UpgradeRunStep step, DevUpgradeFaults devFaults, CancellationToken ct)
    {
        step.Attempt += 1;
        step.State = UpgradeRunStepStates.Running;
        step.StartedAtUtc = DateTime.UtcNow;
        step.LastErrorCode = null;
        step.LastErrorMessage = null;
        step.NextRetryAtUtc = null;

        await audit.WriteAsync("system", "upgrade_step_started", step.StepKey, run.InstallationId, null, run.TraceId, null, ct);
        await db.SaveChangesAsync(ct);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), ct);

            var currentRunState = await db.UpgradeRuns
                .Where(x => x.UpgradeRunId == run.UpgradeRunId)
                .Select(x => x.State)
                .FirstOrDefaultAsync(ct);

            if (string.Equals(currentRunState, UpgradeRunStates.Canceled, StringComparison.OrdinalIgnoreCase))
            {
                step.State = UpgradeRunStepStates.Canceled;
                step.FinishedAtUtc = DateTime.UtcNow;
                step.LastErrorCode = "canceled";
                step.LastErrorMessage = "Canceled by admin.";
                await audit.WriteAsync("system", "upgrade_step_canceled", step.StepKey, run.InstallationId, null, run.TraceId, null, ct);
                return;
            }

            if (devFaults.ConsumeShouldFail(run.UpgradeRunId, step.StepKey))
                throw new InvalidOperationException($"dev_forced_failure:{step.StepKey}");

            step.State = UpgradeRunStepStates.Succeeded;
            step.FinishedAtUtc = DateTime.UtcNow;
            await audit.WriteAsync("system", "upgrade_step_succeeded", step.StepKey, run.InstallationId, null, run.TraceId, null, ct);
        }
        catch (Exception ex)
        {
            step.State = UpgradeRunStepStates.Failed;
            step.FinishedAtUtc = DateTime.UtcNow;
            step.LastErrorCode = "upgrade_step_failed";
            step.LastErrorMessage = ex.Message;
            step.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(Math.Min(60, Math.Pow(2, Math.Min(step.Attempt, 6))));

            run.State = UpgradeRunStates.Failed;
            run.ErrorCode = step.LastErrorCode;
            run.ErrorMessage = step.LastErrorMessage;
            run.FinishedAtUtc = DateTime.UtcNow;

            await audit.WriteAsync("system", "upgrade_step_failed", step.StepKey, run.InstallationId, null, run.TraceId, null, ct);
            await audit.WriteAsync("system", "upgrade_run_failed", run.UpgradeRunId.ToString(), run.InstallationId, null, run.TraceId, null, ct);
        }
    }
}
