namespace LowCodePlatform.Backend.Models;

public sealed class Tenant
{
    public Guid TenantId { get; set; }
    public string Slug { get; set; } = string.Empty;

    public string? ConnectionStringSecretRef { get; set; }
    public string? ConnectionString { get; set; }

    /// <summary>SHA-256 hex (64 chars) of UTF-8 tenant API key; null if disabled.</summary>
    public string? TenantApiKeySha256Hex { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
