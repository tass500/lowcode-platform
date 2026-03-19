namespace LowCodePlatform.Backend.Contracts;

public sealed record EntityRecordListItemDto(
    Guid EntityRecordId,
    Guid EntityDefinitionId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string DataJson);

public sealed record EntityRecordDetailsDto(
    Guid EntityRecordId,
    Guid EntityDefinitionId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string DataJson);

public sealed record EntityRecordListResponse(DateTime ServerTimeUtc, List<EntityRecordListItemDto> Items);
