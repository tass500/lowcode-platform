namespace LowCodePlatform.Backend.Contracts;

public sealed record WorkflowDefinitionListItemDto(
    Guid WorkflowDefinitionId,
    string Name,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record WorkflowDefinitionDetailsDto(
    Guid WorkflowDefinitionId,
    string Name,
    string DefinitionJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record WorkflowListResponse(DateTime ServerTimeUtc, List<WorkflowDefinitionListItemDto> Items);
