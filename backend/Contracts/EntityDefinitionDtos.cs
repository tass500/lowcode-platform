namespace LowCodePlatform.Backend.Contracts;

public sealed record EntityDefinitionListItemDto(
    Guid EntityDefinitionId,
    string Name,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record EntityDefinitionDetailsDto(
    Guid EntityDefinitionId,
    string Name,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    List<FieldDefinitionListItemDto> Fields);

public sealed record FieldDefinitionListItemDto(
    Guid FieldDefinitionId,
    Guid EntityDefinitionId,
    string Name,
    string FieldType,
    bool IsRequired,
    int? MaxLength,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record EntityDefinitionListResponse(DateTime ServerTimeUtc, List<EntityDefinitionListItemDto> Items);
