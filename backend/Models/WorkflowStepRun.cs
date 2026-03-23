namespace LowCodePlatform.Backend.Models;

public sealed class WorkflowStepRun
{
    public Guid WorkflowStepRunId { get; set; }
    public Guid WorkflowRunId { get; set; }

    public string StepKey { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string? StepConfigJson { get; set; }
    public string? OutputJson { get; set; }

    public string State { get; set; } = WorkflowStepRunStates.Pending;

    public int Attempt { get; set; }

    public string? LastErrorCode { get; set; }
    public string? LastErrorMessage { get; set; }

    /// <summary>JSON path within the step config where the error applies (e.g. <c>$.recordId</c> for context var interpolation).</summary>
    public string? LastErrorConfigPath { get; set; }

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public WorkflowRun? WorkflowRun { get; set; }
}

public static class WorkflowStepRunStates
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Canceled = "canceled";
}
