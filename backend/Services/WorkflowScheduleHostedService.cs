using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

/// <summary>
/// Per-tenant tick: starts workflow runs when <see cref="WorkflowDefinition.ScheduleNextDueUtc"/> is due (UTC).
/// </summary>
public sealed class WorkflowScheduleHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<WorkflowScheduleHostedService> _logger;

    public WorkflowScheduleHostedService(IServiceProvider sp, ILogger<WorkflowScheduleHostedService> logger)
    {
        _sp = sp;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.Equals(Environment.GetEnvironmentVariable("LCP_WORKFLOW_SCHEDULE_DISABLED"), "1", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("workflow_schedule_disabled_via_env");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAllTenantsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "workflow_schedule_tick_failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task TickAllTenantsAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var registry = scope.ServiceProvider.GetRequiredService<TenantRegistryService>();
        var tenants = await registry.ListAsync(ct);

        foreach (var tenant in tenants)
            await TickTenantAsync(tenant.Slug, ct);
    }

    private async Task TickTenantAsync(string tenantSlug, CancellationToken ct)
    {
        List<(Guid WorkflowDefinitionId, DateTime NextDueUtc)> due = new();

        using (var scope = _sp.CreateScope())
        {
            var tenantCtx = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantCtx.Slug = tenantSlug;

            var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var now = DateTime.UtcNow;

            var rows = await db.WorkflowDefinitions.AsNoTracking()
                .Where(x => x.ScheduleEnabled && x.ScheduleCron != null && x.ScheduleNextDueUtc != null && x.ScheduleNextDueUtc <= now)
                .Select(x => new { x.WorkflowDefinitionId, x.ScheduleNextDueUtc })
                .ToListAsync(ct);

            foreach (var r in rows)
            {
                if (r.ScheduleNextDueUtc is { } dueUtc)
                    due.Add((r.WorkflowDefinitionId, dueUtc));
            }
        }

        foreach (var item in due)
        {
            if (ct.IsCancellationRequested)
                break;

            _ = Task.Run(() => RunScheduledWorkflowAsync(tenantSlug, item.WorkflowDefinitionId), CancellationToken.None);
        }
    }

    private async Task RunScheduledWorkflowAsync(string tenantSlug, Guid workflowDefinitionId)
    {
        try
        {
            using var scope = _sp.CreateScope();
            var tenantCtx = scope.ServiceProvider.GetRequiredService<TenantContext>();
            tenantCtx.Slug = tenantSlug;

            var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var runner = scope.ServiceProvider.GetRequiredService<WorkflowRunnerService>();
            var audit = scope.ServiceProvider.GetRequiredService<AuditService>();
            var registry = scope.ServiceProvider.GetRequiredService<TenantRegistryService>();

            var wf = await db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.WorkflowDefinitionId == workflowDefinitionId, CancellationToken.None);
            if (wf is null || !wf.ScheduleEnabled || string.IsNullOrWhiteSpace(wf.ScheduleCron))
                return;

            if (!WorkflowRestrictedCron.TryParse(wf.ScheduleCron, out _, out var nextFn))
                return;

            var now = DateTime.UtcNow;
            if (wf.ScheduleNextDueUtc is null || wf.ScheduleNextDueUtc > now)
                return;

            var nextDue = nextFn(now);
            wf.ScheduleNextDueUtc = nextDue;
            wf.UpdatedAtUtc = now;
            await db.SaveChangesAsync(CancellationToken.None);

            var traceId = $"schedule:{workflowDefinitionId:N}:{Guid.NewGuid():N}";
            var run = await runner.StartAsync(wf, traceId, CancellationToken.None);

            var tenantRecord = await registry.FindBySlugAsync(tenantSlug, CancellationToken.None);
            await audit.WriteAsync(
                actor: "system",
                action: "workflow_run_started",
                target: run.WorkflowRunId.ToString(),
                installationId: null,
                tenantId: tenantRecord?.TenantId,
                traceId: traceId,
                detailsJson: $"{{\"workflowDefinitionId\":\"{wf.WorkflowDefinitionId}\",\"state\":\"{run.State}\",\"trigger\":\"schedule\"}}",
                ct: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "workflow_schedule_run_failed {WorkflowDefinitionId} tenant {TenantSlug}", workflowDefinitionId, tenantSlug);
        }
    }
}
