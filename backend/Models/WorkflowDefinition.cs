namespace LowCodePlatform.Backend.Models;

public sealed class WorkflowDefinition
{
    public Guid WorkflowDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";

    /// <summary>SHA-256 (hex) of the inbound webhook secret; null if inbound trigger is disabled.</summary>
    public string? InboundTriggerSecretSha256Hex { get; set; }

    /// <summary>When true, <see cref="WorkflowScheduleHostedService"/> may start runs per <see cref="ScheduleCron"/>.</summary>
    public bool ScheduleEnabled { get; set; }

    /// <summary>
    /// Restricted 5-field cron (UTC): <c>* * * * *</c>, <c>*/N * * * *</c> (1≤N≤59),
    /// <c>M * * * *</c> (hourly at minute M), <c>M H * * *</c> (daily at H:M). Day/month/dow must be <c>*</c>.
    /// </summary>
    public string? ScheduleCron { get; set; }

    /// <summary>Next UTC minute boundary when a scheduled run should be started (strictly in the future after last advance).</summary>
    public DateTime? ScheduleNextDueUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
