namespace LowCodePlatform.Backend.Models;

public sealed class UpgradeRun
{
    public Guid UpgradeRunId { get; set; }
    public Guid InstallationId { get; set; }

    public string TargetVersion { get; set; } = string.Empty;

    public string State { get; set; } = UpgradeRunStates.Pending;

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public Installation? Installation { get; set; }
    public List<UpgradeRunStep> Steps { get; set; } = new();
}

public static class UpgradeRunStates
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Canceled = "canceled";
}
