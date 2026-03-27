namespace LowCodePlatform.Backend.Models;

public sealed class WorkflowDefinition
{
    public Guid WorkflowDefinitionId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string DefinitionJson { get; set; } = "{}";

    /// <summary>SHA-256 (hex) of the inbound webhook secret; null if inbound trigger is disabled.</summary>
    public string? InboundTriggerSecretSha256Hex { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
