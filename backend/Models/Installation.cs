namespace LowCodePlatform.Backend.Models;

public sealed class Installation
{
    public Guid InstallationId { get; set; }
    public string ReleaseChannel { get; set; } = "stable";
    public string CurrentVersion { get; set; } = "0.0.0";
    public string SupportedVersion { get; set; } = "0.0.0";
    public DateTime ReleaseDateUtc { get; set; } = DateTime.UtcNow;
    public int UpgradeWindowDays { get; set; } = 60;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
