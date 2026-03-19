namespace LowCodePlatform.Backend.Models;

public sealed class EntityRecord
{
    public Guid EntityRecordId { get; set; }
    public Guid EntityDefinitionId { get; set; }

    public string DataJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public EntityDefinition? EntityDefinition { get; set; }
}
