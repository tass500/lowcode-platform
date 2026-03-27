using System.Text.Json;

namespace LowCodePlatform.Backend.Services;

/// <summary>
/// Step-level retry / backoff and optional timeout parsed from workflow step JSON
/// (<c>retry</c> object, <c>timeoutMs</c> on the step root). Used by <see cref="WorkflowRunnerService"/>.
/// </summary>
public static class WorkflowStepRetryPolicy
{
    public sealed record RetrySettings(int MaxAttempts, int DelayMs, double BackoffFactor, int? MaxDelayMs);

    public sealed record ExecutionSettings(RetrySettings Retry, int? TimeoutMs);

    public static ExecutionSettings Parse(string? stepConfigJson)
    {
        var retry = ParseRetrySettings(stepConfigJson);
        if (string.IsNullOrWhiteSpace(stepConfigJson))
            return new ExecutionSettings(Retry: retry, TimeoutMs: null);

        try
        {
            using var doc = JsonDocument.Parse(stepConfigJson);
            var root = doc.RootElement;

            int? timeoutMs = null;
            if (root.TryGetProperty("timeoutMs", out var timeoutEl)
                && timeoutEl.TryGetInt32(out var parsed)
                && parsed >= 1)
            {
                timeoutMs = parsed;
            }

            return new ExecutionSettings(Retry: retry, TimeoutMs: timeoutMs);
        }
        catch
        {
            return new ExecutionSettings(Retry: retry, TimeoutMs: null);
        }
    }

    /// <summary>
    /// Delay before attempt <paramref name="attemptNumber"/> (1-based). First attempt is never delayed.
    /// </summary>
    public static int GetInterAttemptDelayMs(RetrySettings policy, int attemptNumber)
    {
        if (policy.MaxAttempts <= 1)
            return 0;
        if (policy.DelayMs <= 0)
            return 0;
        if (attemptNumber <= 1)
            return 0;

        var exponent = attemptNumber - 2;
        var raw = policy.DelayMs * Math.Pow(policy.BackoffFactor, exponent);
        var ms = raw > int.MaxValue ? int.MaxValue : (int)Math.Round(raw);

        if (policy.MaxDelayMs is not null)
            ms = Math.Min(ms, policy.MaxDelayMs.Value);

        return Math.Max(0, ms);
    }

    private static RetrySettings ParseRetrySettings(string? stepConfigJson)
    {
        if (string.IsNullOrWhiteSpace(stepConfigJson))
            return new RetrySettings(MaxAttempts: 1, DelayMs: 0, BackoffFactor: 1, MaxDelayMs: null);

        try
        {
            using var doc = JsonDocument.Parse(stepConfigJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("retry", out var retryEl) || retryEl.ValueKind != JsonValueKind.Object)
                return new RetrySettings(MaxAttempts: 1, DelayMs: 0, BackoffFactor: 1, MaxDelayMs: null);

            var maxAttempts = 1;
            if (retryEl.TryGetProperty("maxAttempts", out var maxEl) && maxEl.TryGetInt32(out var parsedMax) && parsedMax >= 1)
                maxAttempts = parsedMax;

            var delayMs = 0;
            if (retryEl.TryGetProperty("delayMs", out var delayEl) && delayEl.TryGetInt32(out var parsedDelay) && parsedDelay >= 0)
                delayMs = parsedDelay;

            var backoffFactor = 1d;
            if (retryEl.TryGetProperty("backoffFactor", out var factorEl) && factorEl.TryGetDouble(out var parsedFactor) && parsedFactor >= 1)
                backoffFactor = parsedFactor;

            int? maxDelayMs = null;
            if (retryEl.TryGetProperty("maxDelayMs", out var maxDelayEl) && maxDelayEl.TryGetInt32(out var parsedMaxDelay) && parsedMaxDelay >= 0)
                maxDelayMs = parsedMaxDelay;

            return new RetrySettings(MaxAttempts: maxAttempts, DelayMs: delayMs, BackoffFactor: backoffFactor, MaxDelayMs: maxDelayMs);
        }
        catch
        {
            return new RetrySettings(MaxAttempts: 1, DelayMs: 0, BackoffFactor: 1, MaxDelayMs: null);
        }
    }
}
