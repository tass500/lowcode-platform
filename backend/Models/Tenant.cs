namespace LowCodePlatform.Backend.Models;

public sealed class Tenant
{
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;

    public string? ConnectionStringSecretRef { get; set; }
    public string? ConnectionString { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
