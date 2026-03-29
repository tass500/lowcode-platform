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

/// <summary>Portable workflow package for iter 61 import/export. <see cref="ExportFormatVersion"/> must stay backward-compatible.</summary>
public sealed record WorkflowDefinitionExportDto(
    int ExportFormatVersion,
    string Name,
    string DefinitionJson,
    DateTime ExportedAtUtc,
    Guid SourceWorkflowDefinitionId);

public sealed record ImportWorkflowRequest(
    string Name,
    string DefinitionJson,
    int? ExportFormatVersion);
