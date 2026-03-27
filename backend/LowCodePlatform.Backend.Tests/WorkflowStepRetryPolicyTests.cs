using LowCodePlatform.Backend.Services;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class WorkflowStepRetryPolicyTests
{
    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(2, 1, 0)]
    [InlineData(2, 2, 100)]
    [InlineData(3, 3, 200)]
    public void GetInterAttemptDelayMs_exponential_backoff(int maxAttempts, int attemptNumber, int expectedDelay)
    {
        var r = new WorkflowStepRetryPolicy.RetrySettings(
            MaxAttempts: maxAttempts,
            DelayMs: 100,
            BackoffFactor: 2,
            MaxDelayMs: null);
        Assert.Equal(expectedDelay, WorkflowStepRetryPolicy.GetInterAttemptDelayMs(r, attemptNumber));
    }

    [Fact]
    public void GetInterAttemptDelayMs_caps_at_maxDelayMs()
    {
        var r = new WorkflowStepRetryPolicy.RetrySettings(
            MaxAttempts: 5,
            DelayMs: 100,
            BackoffFactor: 10,
            MaxDelayMs: 150);
        Assert.Equal(0, WorkflowStepRetryPolicy.GetInterAttemptDelayMs(r, 1));
        Assert.Equal(100, WorkflowStepRetryPolicy.GetInterAttemptDelayMs(r, 2));
        Assert.Equal(150, WorkflowStepRetryPolicy.GetInterAttemptDelayMs(r, 3));
    }

    [Fact]
    public void Parse_reads_retry_and_timeout()
    {
        var json = "{\"type\":\"noop\",\"retry\":{\"maxAttempts\":4,\"delayMs\":50,\"backoffFactor\":1.5},\"timeoutMs\":5000}";
        var p = WorkflowStepRetryPolicy.Parse(json);
        Assert.Equal(4, p.Retry.MaxAttempts);
        Assert.Equal(50, p.Retry.DelayMs);
        Assert.Equal(1.5, p.Retry.BackoffFactor);
        Assert.Equal(5000, p.TimeoutMs);
    }
}
