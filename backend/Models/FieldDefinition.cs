namespace LowCodePlatform.Backend.Models;

public sealed class FieldDefinition
{
    public Guid FieldDefinitionId { get; set; }
    public Guid EntityDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = string.Empty;

    public bool IsRequired { get; set; }
    public int? MaxLength { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public EntityDefinition? Entity { get; set; }
}
