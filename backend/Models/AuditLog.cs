namespace LowCodePlatform.Backend.Models;

public sealed class AuditLog
{
    public Guid AuditLogId { get; set; }

    public string Actor { get; set; } = "system";
    public string Action { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;

    public Guid? InstallationId { get; set; }
    public Guid? TenantId { get; set; }

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string TraceId { get; set; } = string.Empty;

    public string? DetailsJson { get; set; }
}
