using LowCodePlatform.Backend.Models;

namespace LowCodePlatform.Backend.Services;

public sealed record VersionEnforcementStatus(string EnforcementState, int DaysOutOfSupport);

public static class EnforcementStates
{
    public const string Ok = "ok";
    public const string Warn = "warn";
    public const string SoftBlock = "soft_block";
    public const string HardBlock = "hard_block";
}

public sealed class VersionEnforcementService
{
    public VersionEnforcementStatus Evaluate(Installation inst)
    {
        if (string.Equals(inst.CurrentVersion, inst.SupportedVersion, StringComparison.OrdinalIgnoreCase))
            return new VersionEnforcementStatus(EnforcementStates.Ok, 0);

        var days = (int)Math.Floor((DateTime.UtcNow - inst.ReleaseDateUtc).TotalDays);
        var daysOut = Math.Max(0, days);

        if (daysOut < 30)
            return new VersionEnforcementStatus(EnforcementStates.Warn, daysOut);

        if (daysOut < inst.UpgradeWindowDays)
            return new VersionEnforcementStatus(EnforcementStates.SoftBlock, daysOut);

        return new VersionEnforcementStatus(EnforcementStates.HardBlock, daysOut);
    }
}
