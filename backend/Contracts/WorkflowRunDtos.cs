namespace LowCodePlatform.Backend.Contracts;

public sealed record WorkflowStepRunDto(
    Guid WorkflowStepRunId,
    string StepKey,
    string StepType,
    string? OriginalStepConfigJson,
    string? StepConfigJson,
    string? OutputJson,
    string State,
    int Attempt,
    string? LastErrorCode,
    string? LastErrorMessage,
    string? LastErrorConfigPath,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc);

public sealed record WorkflowRunDetailsDto(
    Guid WorkflowRunId,
    Guid WorkflowDefinitionId,
    string State,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string TraceId,
    string? ErrorCode,
    string? ErrorMessage,
    IEnumerable<WorkflowStepRunDto> Steps);

public sealed record StartWorkflowRunResponse(DateTime ServerTimeUtc, Guid WorkflowRunId);

public sealed record WorkflowRunListItemDto(
    Guid WorkflowRunId,
    Guid WorkflowDefinitionId,
    string State,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string TraceId,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record WorkflowRunListResponse(DateTime ServerTimeUtc, List<WorkflowRunListItemDto> Items);

/// <summary>Tenant-wide run list item (includes workflow definition name).</summary>
public sealed record TenantWorkflowRunListItemDto(
    Guid WorkflowRunId,
    Guid WorkflowDefinitionId,
    string WorkflowName,
    string State,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string TraceId,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record TenantWorkflowRunListResponse(
    DateTime ServerTimeUtc,
    List<TenantWorkflowRunListItemDto> Items,
    int TotalCount);
