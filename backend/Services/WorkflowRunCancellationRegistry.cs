using System.Collections.Concurrent;

namespace LowCodePlatform.Backend.Services;

/// <summary>
/// Cooperative cancellation for in-process <see cref="WorkflowRunnerService.StartAsync"/> runs
/// (HTTP-started or scheduled). <see cref="TryCancel"/> signals the linked <see cref="CancellationToken"/> used by step execution.
/// </summary>
public sealed class WorkflowRunCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _byRunId = new();

    public bool TryRegister(Guid workflowRunId, CancellationTokenSource runCts)
        => _byRunId.TryAdd(workflowRunId, runCts);

    public void DisposeRegistration(Guid workflowRunId)
    {
        if (_byRunId.TryRemove(workflowRunId, out var cts))
            cts.Dispose();
    }

    /// <summary>Returns false if the run is not currently registered (finished or unknown).</summary>
    public bool TryCancel(Guid workflowRunId)
    {
        if (!_byRunId.TryGetValue(workflowRunId, out var cts))
            return false;

        try
        {
            cts.Cancel();
        }
        catch
        {
            // ignore
        }

        return true;
    }
}
