namespace LowCodePlatform.Backend.Models;

public sealed class UpgradeRunStep
{
    public Guid UpgradeRunStepId { get; set; }
    public Guid UpgradeRunId { get; set; }

    public string StepKey { get; set; } = string.Empty;
    public string State { get; set; } = UpgradeRunStepStates.Pending;

    public int Attempt { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }

    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public UpgradeRun? UpgradeRun { get; set; }
}

public static class UpgradeRunStepStates
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Canceled = "canceled";
}
