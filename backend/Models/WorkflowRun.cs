namespace LowCodePlatform.Backend.Models;

public sealed class WorkflowRun
{
    public Guid WorkflowRunId { get; set; }
    public Guid WorkflowDefinitionId { get; set; }

    public string State { get; set; } = WorkflowRunStates.Pending;

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    public string TraceId { get; set; } = string.Empty;

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public WorkflowDefinition? WorkflowDefinition { get; set; }
    public List<WorkflowStepRun> Steps { get; set; } = new();
}

public static class WorkflowRunStates
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Canceled = "canceled";
}
