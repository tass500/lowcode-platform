namespace LowCodePlatform.Backend.Models;

public sealed class EntityDefinition
{
    public Guid EntityDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<FieldDefinition> Fields { get; set; } = new();
}
