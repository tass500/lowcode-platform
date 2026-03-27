namespace LowCodePlatform.Backend.Contracts;

public sealed record WorkflowLintWarningDto(
    string Code,
    string Message);

public sealed record WorkflowDefinitionListItemDto(
    Guid WorkflowDefinitionId,
    string Name,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record WorkflowDefinitionDetailsDto(
    Guid WorkflowDefinitionId,
    string Name,
    string DefinitionJson,
    List<WorkflowLintWarningDto> LintWarnings,
    bool InboundTriggerConfigured,
    bool ScheduleEnabled,
    string? ScheduleCron,
    DateTime? ScheduleNextDueUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SetWorkflowScheduleRequest(bool Enabled, string? Cron);

public sealed record SetWorkflowInboundTriggerRequest(string Secret);

public sealed record WorkflowInboundTriggerStatusDto(bool InboundTriggerConfigured, DateTime ServerTimeUtc);

public sealed record WorkflowListResponse(DateTime ServerTimeUtc, List<WorkflowDefinitionListItemDto> Items);
