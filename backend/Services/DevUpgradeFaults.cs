using System.Collections.Concurrent;

namespace LowCodePlatform.Backend.Services;

public sealed class DevUpgradeFaults
{
    private readonly ConcurrentDictionary<string, byte> _failOnce = new(StringComparer.OrdinalIgnoreCase);

    private static string Key(Guid upgradeRunId, string stepKey) => $"{upgradeRunId:N}:{stepKey}";

    public void RequestFailOnce(Guid upgradeRunId, string stepKey)
    {
        if (string.IsNullOrWhiteSpace(stepKey))
            throw new ArgumentException("stepKey is required", nameof(stepKey));

        _failOnce[Key(upgradeRunId, stepKey.Trim())] = 1;
    }

    public bool ConsumeShouldFail(Guid upgradeRunId, string stepKey)
    {
        if (string.IsNullOrWhiteSpace(stepKey))
            return false;

        return _failOnce.TryRemove(Key(upgradeRunId, stepKey.Trim()), out _);
    }
}
