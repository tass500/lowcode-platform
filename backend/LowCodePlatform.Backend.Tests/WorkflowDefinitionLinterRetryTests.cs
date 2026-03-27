using LowCodePlatform.Backend.Services;
using Xunit;

namespace LowCodePlatform.Backend.Tests;

public sealed class WorkflowDefinitionLinterRetryTests
{
    [Fact]
    public void Lint_warns_when_retry_is_not_object()
    {
        var json = "{\"steps\":[{\"type\":\"noop\",\"retry\":\"oops\"}]}";
        var w = WorkflowDefinitionLinter.Lint(json);
        Assert.Contains(w, x => x.Code == "workflow_retry_config_invalid");
    }

    [Fact]
    public void Lint_warns_when_maxAttempts_invalid()
    {
        var json = "{\"steps\":[{\"type\":\"noop\",\"retry\":{\"maxAttempts\":0}}]}";
        var w = WorkflowDefinitionLinter.Lint(json);
        Assert.Contains(w, x => x.Code == "workflow_retry_config_invalid" && x.Message.Contains("maxAttempts"));
    }

    [Fact]
    public void Lint_warns_when_timeoutMs_invalid()
    {
        var json = "{\"steps\":[{\"type\":\"noop\",\"timeoutMs\":0}]}";
        var w = WorkflowDefinitionLinter.Lint(json);
        Assert.Contains(w, x => x.Code == "workflow_step_timeout_invalid");
    }

    [Fact]
    public void Lint_no_warning_for_valid_retry()
    {
        var json = "{\"steps\":[{\"type\":\"noop\",\"retry\":{\"maxAttempts\":3,\"delayMs\":10,\"backoffFactor\":2,\"maxDelayMs\":1000}}]}";
        var w = WorkflowDefinitionLinter.Lint(json);
        Assert.DoesNotContain(w, x => x.Code == "workflow_retry_config_invalid");
        Assert.DoesNotContain(w, x => x.Code == "workflow_step_timeout_invalid");
    }
}
