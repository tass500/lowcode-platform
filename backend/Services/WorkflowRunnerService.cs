using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

public sealed class WorkflowRunnerService
{
    private static readonly Regex ContextVarRegex = new(@"\$\{([^}]+)\}", RegexOptions.Compiled);

    private sealed record RetryPolicy(int MaxAttempts, int DelayMs, double BackoffFactor, int? MaxDelayMs);

    private sealed record StepPolicy(RetryPolicy Retry, int? TimeoutMs);

    private readonly PlatformDbContext _db;

    public WorkflowRunnerService(PlatformDbContext db)
    {
        _db = db;
    }

    private static void ExecuteRequireAsync(WorkflowStepRun step, JsonObject context)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "require_config_missing";
            step.LastErrorMessage = "require step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "require_path_missing";
            step.LastErrorMessage = "require step requires a string 'path'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var path = (pathEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            step.LastErrorCode = "require_path_missing";
            step.LastErrorMessage = "require step requires a non-empty 'path'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var value = ResolveContextPath(context, path);
        if (value is null)
        {
            step.LastErrorCode = "require_failed";
            step.LastErrorMessage = $"Required path not found: '{path}'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (root.TryGetProperty("equals", out var equalsEl))
        {
            if (equalsEl.ValueKind != JsonValueKind.String)
            {
                step.LastErrorCode = "require_equals_invalid";
                step.LastErrorMessage = "require step field 'equals' must be a string when provided.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            var expected = equalsEl.GetString() ?? string.Empty;
            var actual = value is JsonValue jv ? jv.ToJsonString().Trim('"') : value.ToJsonString();

            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                step.LastErrorCode = "require_failed";
                step.LastErrorMessage = $"Required path '{path}' did not equal expected value.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }
    }

    private static StepPolicy ParseStepPolicy(string? stepConfigJson)
    {
        var retry = ParseRetryPolicy(stepConfigJson);
        if (string.IsNullOrWhiteSpace(stepConfigJson))
            return new StepPolicy(Retry: retry, TimeoutMs: null);

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

            return new StepPolicy(Retry: retry, TimeoutMs: timeoutMs);
        }
        catch
        {
            return new StepPolicy(Retry: retry, TimeoutMs: null);
        }
    }

    private static void ExecuteSetAsync(WorkflowStepRun step)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "set_config_missing";
            step.LastErrorMessage = "set step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("output", out var outputEl))
        {
            step.OutputJson = outputEl.GetRawText();
            return;
        }

        if (root.TryGetProperty("outputJson", out var outputJsonEl))
        {
            if (outputJsonEl.ValueKind != JsonValueKind.String)
            {
                step.LastErrorCode = "set_output_json_invalid";
                step.LastErrorMessage = "set step field 'outputJson' must be a string when provided.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            var raw = (outputJsonEl.GetString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                step.LastErrorCode = "set_output_missing";
                step.LastErrorMessage = "set step requires 'output' or non-empty 'outputJson'.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            try
            {
                var node = JsonNode.Parse(raw);
                if (node is null)
                {
                    step.LastErrorCode = "set_output_json_invalid";
                    step.LastErrorMessage = "set step field 'outputJson' must contain valid JSON.";
                    throw new InvalidOperationException(step.LastErrorMessage);
                }

                step.OutputJson = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
                return;
            }
            catch
            {
                step.LastErrorCode = "set_output_json_invalid";
                step.LastErrorMessage = "set step field 'outputJson' must contain valid JSON.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }

        step.LastErrorCode = "set_output_missing";
        step.LastErrorMessage = "set step requires 'output' or 'outputJson'.";
        throw new InvalidOperationException(step.LastErrorMessage);
    }

    private static RetryPolicy ParseRetryPolicy(string? stepConfigJson)
    {
        if (string.IsNullOrWhiteSpace(stepConfigJson))
            return new RetryPolicy(MaxAttempts: 1, DelayMs: 0, BackoffFactor: 1, MaxDelayMs: null);

        try
        {
            using var doc = JsonDocument.Parse(stepConfigJson);
            var root = doc.RootElement;
            if (!root.TryGetProperty("retry", out var retryEl) || retryEl.ValueKind != JsonValueKind.Object)
                return new RetryPolicy(MaxAttempts: 1, DelayMs: 0, BackoffFactor: 1, MaxDelayMs: null);

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

            return new RetryPolicy(MaxAttempts: maxAttempts, DelayMs: delayMs, BackoffFactor: backoffFactor, MaxDelayMs: maxDelayMs);
        }
        catch
        {
            return new RetryPolicy(MaxAttempts: 1, DelayMs: 0, BackoffFactor: 1, MaxDelayMs: null);
        }
    }

    private static int GetRetryDelayMs(RetryPolicy policy, int attemptNumber)
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

    private static void ExecuteUnstableAsync(WorkflowStepRun step)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "unstable_config_missing";
            step.LastErrorMessage = "unstable step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        var failTimes = 0;
        if (root.TryGetProperty("failTimes", out var ftEl))
        {
            if (!ftEl.TryGetInt32(out failTimes) || failTimes < 0)
            {
                step.LastErrorCode = "unstable_fail_times_invalid";
                step.LastErrorMessage = "unstable step field 'failTimes' must be a non-negative integer when provided.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }

        if (step.Attempt <= failTimes)
        {
            step.LastErrorCode = "unstable_failed";
            step.LastErrorMessage = "unstable step failed (deterministic transient).";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (root.TryGetProperty("output", out var outputEl))
        {
            step.OutputJson = outputEl.GetRawText();
            return;
        }

        step.OutputJson = "{\"ok\":true}";
    }

    private static void ExecuteMapAsync(WorkflowStepRun step, JsonObject context)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "map_config_missing";
            step.LastErrorMessage = "map step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("mappings", out var mappingsEl) || mappingsEl.ValueKind != JsonValueKind.Object)
        {
            step.LastErrorCode = "map_mappings_missing";
            step.LastErrorMessage = "map step requires an object 'mappings'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var output = new JsonObject();
        foreach (var prop in mappingsEl.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.String)
            {
                step.LastErrorCode = "map_mapping_invalid";
                step.LastErrorMessage = "map step 'mappings' values must be strings (context paths).";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            var path = (prop.Value.GetString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                step.LastErrorCode = "map_mapping_invalid";
                step.LastErrorMessage = "map step 'mappings' values must be non-empty strings (context paths).";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            var resolved = ResolveContextPath(context, path);
            if (resolved is null)
            {
                step.LastErrorCode = "map_source_not_found";
                step.LastErrorMessage = $"Context path not found for mapping '{prop.Name}': '{path}'.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            output[prop.Name] = JsonNode.Parse(resolved.ToJsonString());
        }

        step.OutputJson = output.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private async Task ExecuteForeachAsync(WorkflowStepRun step, JsonObject context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "foreach_config_missing";
            step.LastErrorMessage = "foreach step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("items", out var itemsEl))
        {
            step.LastErrorCode = "foreach_items_missing";
            step.LastErrorMessage = "foreach step requires 'items' (string context path or inline array).";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (!root.TryGetProperty("do", out var doEl) || doEl.ValueKind != JsonValueKind.Object)
        {
            step.LastErrorCode = "foreach_do_missing";
            step.LastErrorMessage = "foreach step requires an object 'do' (a single inner step definition).";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var asVar = "item";
        if (root.TryGetProperty("as", out var asEl))
        {
            if (asEl.ValueKind != JsonValueKind.String)
            {
                step.LastErrorCode = "foreach_as_invalid";
                step.LastErrorMessage = "foreach step field 'as' must be a string when provided.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            var raw = (asEl.GetString() ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(raw))
                asVar = raw;
        }

        JsonNode? itemsNode;
        if (itemsEl.ValueKind == JsonValueKind.String)
        {
            var path = (itemsEl.GetString() ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                step.LastErrorCode = "foreach_items_invalid";
                step.LastErrorMessage = "foreach step field 'items' must be a non-empty context path string or an inline array.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            itemsNode = ResolveContextPath(context, path);
            if (itemsNode is null)
            {
                step.LastErrorCode = "foreach_items_not_found";
                step.LastErrorMessage = $"Context path not found for foreach items: '{path}'.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }
        else if (itemsEl.ValueKind == JsonValueKind.Array)
        {
            itemsNode = JsonNode.Parse(itemsEl.GetRawText());
        }
        else
        {
            step.LastErrorCode = "foreach_items_invalid";
            step.LastErrorMessage = "foreach step field 'items' must be a string (context path) or inline array.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (itemsNode is not JsonArray arr)
        {
            step.LastErrorCode = "foreach_items_invalid";
            step.LastErrorMessage = "foreach step 'items' must resolve to a JSON array.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (!doEl.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "foreach_do_type_missing";
            step.LastErrorMessage = "foreach step 'do' must contain a string 'type'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var innerType = (typeEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(innerType))
        {
            step.LastErrorCode = "foreach_do_type_missing";
            step.LastErrorMessage = "foreach step 'do' must contain a non-empty string 'type'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        context.TryGetPropertyValue("foreach", out var priorForeach);
        context.TryGetPropertyValue(asVar, out var priorAs);

        var results = new JsonArray();
        for (var i = 0; i < arr.Count; i += 1)
        {
            ct.ThrowIfCancellationRequested();

            var currentItem = arr[i];
            var itemNode = currentItem is null ? null : JsonNode.Parse(currentItem.ToJsonString());
            var foreachObj = new JsonObject
            {
                ["item"] = itemNode,
                ["index"] = i,
            };

            context["foreach"] = foreachObj;
            context[asVar] = itemNode is null ? null : JsonNode.Parse(itemNode.ToJsonString());

            var innerStep = new WorkflowStepRun
            {
                WorkflowStepRunId = Guid.Empty,
                WorkflowRunId = step.WorkflowRunId,
                StepKey = $"{step.StepKey}.{i:000}",
                StepType = innerType,
                StepConfigJson = doEl.GetRawText(),
                State = WorkflowStepRunStates.Pending,
            };

            try
            {
                InterpolateStepConfigJson(innerStep, context);
                await ExecuteStepBodyAsync(innerStep, context, ct);

                if (string.IsNullOrWhiteSpace(innerStep.OutputJson))
                {
                    results.Add(null);
                }
                else
                {
                    try
                    {
                        results.Add(JsonNode.Parse(innerStep.OutputJson));
                    }
                    catch
                    {
                        results.Add(JsonValue.Create(innerStep.OutputJson));
                    }
                }
            }
            catch (Exception ex)
            {
                step.LastErrorCode = innerStep.LastErrorCode ?? "foreach_inner_failed";
                step.LastErrorMessage = innerStep.LastErrorMessage ?? ex.Message;
                throw;
            }
        }

        if (priorForeach is null)
            context.Remove("foreach");
        else
            context["foreach"] = priorForeach;

        if (priorAs is null)
            context.Remove(asVar);
        else
            context[asVar] = priorAs;

        step.OutputJson = results.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static void ExecuteMergeAsync(WorkflowStepRun step, JsonObject context)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "merge_config_missing";
            step.LastErrorMessage = "merge step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("sources", out var sourcesEl) || sourcesEl.ValueKind != JsonValueKind.Array)
        {
            step.LastErrorCode = "merge_sources_missing";
            step.LastErrorMessage = "merge step requires an array 'sources'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var output = new JsonObject();
        foreach (var srcEl in sourcesEl.EnumerateArray())
        {
            JsonNode? resolved;

            if (srcEl.ValueKind == JsonValueKind.String)
            {
                var path = (srcEl.GetString() ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(path))
                {
                    step.LastErrorCode = "merge_source_invalid";
                    step.LastErrorMessage = "merge step 'sources' items must be non-empty strings (context paths) or inline objects.";
                    throw new InvalidOperationException(step.LastErrorMessage);
                }

                resolved = ResolveContextPath(context, path);
                if (resolved is null)
                {
                    step.LastErrorCode = "merge_source_not_found";
                    step.LastErrorMessage = $"Context path not found for merge source: '{path}'.";
                    throw new InvalidOperationException(step.LastErrorMessage);
                }
            }
            else if (srcEl.ValueKind == JsonValueKind.Object)
            {
                resolved = JsonNode.Parse(srcEl.GetRawText());
            }
            else
            {
                step.LastErrorCode = "merge_source_invalid";
                step.LastErrorMessage = "merge step 'sources' items must be strings (context paths) or inline objects.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            if (resolved is not JsonObject srcObj)
            {
                step.LastErrorCode = "merge_source_invalid";
                step.LastErrorMessage = "merge step sources must resolve to JSON objects.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            foreach (var kv in srcObj)
            {
                if (kv.Value is null)
                    output[kv.Key] = null;
                else
                    output[kv.Key] = JsonNode.Parse(kv.Value.ToJsonString());
            }
        }

        step.OutputJson = output.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static void InterpolateStepConfigJson(WorkflowStepRun step, JsonObject context)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
            return;

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(step.StepConfigJson);
        }
        catch
        {
            step.LastErrorCode = "context_var_config_invalid";
            step.LastErrorMessage = "Failed to parse step config JSON for context variable interpolation.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (root is null)
            return;

        var rewritten = RewriteNode(root, context, step);
        step.StepConfigJson = rewritten.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static JsonNode RewriteNode(JsonNode node, JsonObject context, WorkflowStepRun step)
    {
        if (node is JsonObject obj)
        {
            foreach (var kv in obj.ToList())
            {
                if (kv.Value is null)
                    continue;
                var rewritten = RewriteNode(kv.Value, context, step);
                if (!ReferenceEquals(rewritten, kv.Value))
                    obj[kv.Key] = rewritten;
            }
            return obj;
        }

        if (node is JsonArray arr)
        {
            for (var i = 0; i < arr.Count; i += 1)
            {
                if (arr[i] is null)
                    continue;
                var rewritten = RewriteNode(arr[i]!, context, step);
                if (!ReferenceEquals(rewritten, arr[i]))
                    arr[i] = rewritten;
            }
            return arr;
        }

        if (node is JsonValue val)
        {
            string? s;
            try
            {
                s = val.GetValue<string?>();
            }
            catch
            {
                return node;
            }

            if (string.IsNullOrEmpty(s))
                return node;

            var trimmed = s.Trim();
            var exact = ContextVarRegex.Match(trimmed);
            if (exact.Success && exact.Index == 0 && exact.Length == trimmed.Length)
            {
                var path = exact.Groups[1].Value.Trim();
                var resolved = ResolveContextPath(context, path);
                if (resolved is null)
                {
                    step.LastErrorCode = "context_var_not_found";
                    var available = string.Join(", ", context.Select(kv => kv.Key).OrderBy(x => x));
                    step.LastErrorMessage = $"Context variable not found: '{path}'. Available top-level keys: [{available}].";
                    throw new InvalidOperationException(step.LastErrorMessage);
                }

                return JsonNode.Parse(resolved.ToJsonString()) ?? resolved;
            }

            if (!ContextVarRegex.IsMatch(s))
                return node;

            var replaced = ContextVarRegex.Replace(s, m =>
            {
                var path = m.Groups[1].Value.Trim();
                var resolved = ResolveContextPath(context, path);
                if (resolved is null)
                {
                    step.LastErrorCode = "context_var_not_found";
                    var available = string.Join(", ", context.Select(kv => kv.Key).OrderBy(x => x));
                    step.LastErrorMessage = $"Context variable not found: '{path}'. Available top-level keys: [{available}].";
                    throw new InvalidOperationException(step.LastErrorMessage);
                }

                if (resolved is JsonValue jv)
                    return jv.ToJsonString().Trim('"');

                return resolved.ToJsonString();
            });

            return JsonValue.Create(replaced) ?? node;
        }

        return node;
    }

    private static JsonNode? ResolveContextPath(JsonObject context, string path)
    {
        JsonNode? current = context;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is JsonObject obj)
            {
                if (!obj.TryGetPropertyValue(part, out current))
                    return null;
                continue;
            }

            if (current is JsonArray arr)
            {
                if (!int.TryParse(part, out var idx))
                    return null;
                if (idx < 0 || idx >= arr.Count)
                    return null;
                current = arr[idx];
                continue;
            }

            return null;
        }

        return current;
    }

    public async Task<WorkflowRun> StartAsync(WorkflowDefinition wf, string traceId, CancellationToken ct)
    {
        var run = new WorkflowRun
        {
            WorkflowRunId = Guid.NewGuid(),
            WorkflowDefinitionId = wf.WorkflowDefinitionId,
            State = WorkflowRunStates.Running,
            StartedAtUtc = DateTime.UtcNow,
            TraceId = traceId,
        };

        run.Steps = BuildSteps(wf);

        var context = new JsonObject();

        _db.WorkflowRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        try
        {
            foreach (var step in run.Steps.OrderBy(x => x.StepKey))
            {
                await ExecuteStepAsync(run, step, context, ct);
                await _db.SaveChangesAsync(ct);

                if (step.State == WorkflowStepRunStates.Succeeded && !string.IsNullOrWhiteSpace(step.OutputJson))
                {
                    try
                    {
                        context[step.StepKey] = JsonNode.Parse(step.OutputJson);
                    }
                    catch
                    {
                        // ignore invalid output json; step succeeded but context won't have it
                    }
                }

                if (step.State == WorkflowStepRunStates.Failed)
                {
                    run.State = WorkflowRunStates.Failed;
                    run.ErrorCode = step.LastErrorCode;
                    run.ErrorMessage = step.LastErrorMessage;
                    run.FinishedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return run;
                }

                if (step.State == WorkflowStepRunStates.Canceled)
                {
                    run.State = WorkflowRunStates.Canceled;
                    run.ErrorCode = step.LastErrorCode;
                    run.ErrorMessage = step.LastErrorMessage;
                    run.FinishedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    return run;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            run.State = WorkflowRunStates.Canceled;
            run.ErrorCode = "canceled";
            run.ErrorMessage = "Canceled.";
            run.FinishedAtUtc = now;

            foreach (var step in run.Steps.Where(x => x.State is WorkflowStepRunStates.Pending or WorkflowStepRunStates.Running))
            {
                step.State = WorkflowStepRunStates.Canceled;
                step.FinishedAtUtc ??= now;
                step.LastErrorCode ??= "canceled";
                step.LastErrorMessage ??= "Canceled.";
            }

            try
            {
                await _db.SaveChangesAsync(CancellationToken.None);
            }
            catch
            {
                // ignore
            }

            throw;
        }

        run.State = WorkflowRunStates.Succeeded;
        run.FinishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return run;
    }

    private static List<WorkflowStepRun> BuildSteps(WorkflowDefinition wf)
    {
        var steps = new List<WorkflowStepRun>();

        try
        {
            using var doc = JsonDocument.Parse(wf.DefinitionJson);
            if (doc.RootElement.TryGetProperty("steps", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                var i = 0;
                foreach (var s in arr.EnumerateArray())
                {
                    var type = s.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String
                        ? t.GetString() ?? "noop"
                        : "noop";

                    steps.Add(new WorkflowStepRun
                    {
                        WorkflowStepRunId = Guid.NewGuid(),
                        StepKey = $"{i:000}",
                        StepType = type.Trim(),
                        StepConfigJson = s.GetRawText(),
                        State = WorkflowStepRunStates.Pending,
                    });

                    i += 1;
                }
            }
        }
        catch
        {
            // fall back to single noop
        }

        if (steps.Count == 0)
        {
            steps.Add(new WorkflowStepRun
            {
                WorkflowStepRunId = Guid.NewGuid(),
                StepKey = "000",
                StepType = "noop",
                StepConfigJson = "{}",
                State = WorkflowStepRunStates.Pending,
            });
        }

        return steps;
    }

    private async Task ExecuteStepAsync(WorkflowRun run, WorkflowStepRun step, JsonObject context, CancellationToken ct)
    {
        var policy = ParseStepPolicy(step.StepConfigJson);

        var startedAt = DateTime.UtcNow;
        step.StartedAtUtc = startedAt;

        Exception? lastEx = null;
        for (var attemptNumber = 1; attemptNumber <= policy.Retry.MaxAttempts; attemptNumber += 1)
        {
            ct.ThrowIfCancellationRequested();

            var delayMs = GetRetryDelayMs(policy.Retry, attemptNumber);
            if (delayMs > 0)
                await Task.Delay(delayMs, ct);

            step.Attempt += 1;
            step.State = WorkflowStepRunStates.Running;
            step.LastErrorCode = null;
            step.LastErrorMessage = null;
            step.OutputJson = null;

            CancellationToken attemptCt = ct;
            CancellationTokenSource? timeoutCts = null;
            if (policy.TimeoutMs is not null)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(policy.TimeoutMs.Value);
                attemptCt = timeoutCts.Token;
            }

            try
            {
                InterpolateStepConfigJson(step, context);

                await ExecuteStepBodyAsync(step, context, attemptCt);

                step.State = WorkflowStepRunStates.Succeeded;
                step.FinishedAtUtc = DateTime.UtcNow;
                return;
            }
            catch (OperationCanceledException oce)
            {
                lastEx = oce;

                if (ct.IsCancellationRequested)
                {
                    step.State = WorkflowStepRunStates.Canceled;
                    step.FinishedAtUtc = DateTime.UtcNow;
                    step.LastErrorCode ??= "canceled";
                    step.LastErrorMessage ??= "Canceled.";

                    run.ErrorCode = step.LastErrorCode;
                    run.ErrorMessage = step.LastErrorMessage;
                    return;
                }

                // timeout (linked token canceled)
                step.LastErrorCode ??= "workflow_step_timed_out";
                step.LastErrorMessage ??= $"Step timed out after {policy.TimeoutMs} ms.";

                if (attemptNumber < policy.Retry.MaxAttempts)
                    continue;

                step.State = WorkflowStepRunStates.Failed;
                step.FinishedAtUtc = DateTime.UtcNow;
                run.ErrorCode = step.LastErrorCode;
                run.ErrorMessage = step.LastErrorMessage;
                return;
            }
            catch (Exception ex)
            {
                lastEx = ex;

                if (attemptNumber < policy.Retry.MaxAttempts)
                    continue;

                step.State = WorkflowStepRunStates.Failed;
                step.FinishedAtUtc = DateTime.UtcNow;
                step.LastErrorCode ??= "workflow_step_failed";
                step.LastErrorMessage ??= ex.Message;

                run.ErrorCode = step.LastErrorCode;
                run.ErrorMessage = step.LastErrorMessage;
                return;
            }
            finally
            {
                timeoutCts?.Dispose();
            }
        }

        step.State = WorkflowStepRunStates.Failed;
        step.FinishedAtUtc = DateTime.UtcNow;
        step.LastErrorCode ??= "workflow_step_failed";
        step.LastErrorMessage ??= lastEx?.Message ?? "Unknown error.";

        run.ErrorCode = step.LastErrorCode;
        run.ErrorMessage = step.LastErrorMessage;
    }

    private async Task ExecuteStepBodyAsync(WorkflowStepRun step, JsonObject context, CancellationToken ct)
    {
        switch (step.StepType.ToLowerInvariant())
        {
            case "delay":
            {
                var ms = 100;
                if (!string.IsNullOrWhiteSpace(step.StepConfigJson))
                {
                    using var doc = JsonDocument.Parse(step.StepConfigJson);
                    if (doc.RootElement.TryGetProperty("ms", out var v) && v.TryGetInt32(out var parsed) && parsed >= 0)
                        ms = parsed;
                }

                await Task.Delay(ms, ct);
                break;
            }

            case "set":
            {
                ExecuteSetAsync(step);
                break;
            }

            case "map":
            {
                ExecuteMapAsync(step, context);
                break;
            }

            case "merge":
            {
                ExecuteMergeAsync(step, context);
                break;
            }

            case "foreach":
            {
                await ExecuteForeachAsync(step, context, ct);
                break;
            }

            case "switch":
            {
                await ExecuteSwitchAsync(step, context, ct);
                break;
            }

            case "require":
            {
                ExecuteRequireAsync(step, context);
                break;
            }

            case "domaincommand":
            {
                await ExecuteDomainCommandAsync(step, ct);
                break;
            }

            case "unstable":
            {
                ExecuteUnstableAsync(step);
                break;
            }

            case "noop":
                break;

            default:
                step.LastErrorCode = "workflow_step_type_not_supported";
                step.LastErrorMessage = $"Unsupported workflow step type: '{step.StepType}'.";
                throw new InvalidOperationException(step.LastErrorMessage);
        }
    }

    private async Task ExecuteSwitchAsync(WorkflowStepRun step, JsonObject context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "switch_config_missing";
            step.LastErrorMessage = "switch step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("value", out var valueEl) || valueEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "switch_value_missing";
            step.LastErrorMessage = "switch step requires a string 'value' (context path).";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var valuePath = (valueEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(valuePath))
        {
            step.LastErrorCode = "switch_value_missing";
            step.LastErrorMessage = "switch step requires a non-empty string 'value' (context path).";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var valueNode = ResolveContextPath(context, valuePath);
        if (valueNode is null)
        {
            step.LastErrorCode = "switch_value_not_found";
            step.LastErrorMessage = $"Context path not found for switch value: '{valuePath}'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (!root.TryGetProperty("cases", out var casesEl) || casesEl.ValueKind != JsonValueKind.Array)
        {
            step.LastErrorCode = "switch_cases_missing";
            step.LastErrorMessage = "switch step requires an array 'cases'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        JsonElement? defaultDoEl = null;
        if (root.TryGetProperty("default", out var defaultEl))
        {
            if (defaultEl.ValueKind != JsonValueKind.Object)
            {
                step.LastErrorCode = "switch_default_invalid";
                step.LastErrorMessage = "switch step field 'default' must be an object when provided.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            defaultDoEl = defaultEl;
        }

        JsonElement? selectedDoEl = null;
        for (var i = 0; i < casesEl.GetArrayLength(); i += 1)
        {
            var c = casesEl[i];
            if (c.ValueKind != JsonValueKind.Object)
            {
                step.LastErrorCode = "switch_case_invalid";
                step.LastErrorMessage = "switch step 'cases' items must be objects.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            if (!c.TryGetProperty("do", out var doEl) || doEl.ValueKind != JsonValueKind.Object)
            {
                step.LastErrorCode = "switch_do_missing";
                step.LastErrorMessage = "switch step cases require an object 'do' (a single inner step definition).";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            if (!c.TryGetProperty("when", out var whenEl) && !c.TryGetProperty("equals", out whenEl))
            {
                step.LastErrorCode = "switch_when_missing";
                step.LastErrorMessage = "switch step cases require a 'when' (or 'equals') field.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            JsonNode? whenNode;
            try
            {
                whenNode = JsonNode.Parse(whenEl.GetRawText());
            }
            catch
            {
                step.LastErrorCode = "switch_when_invalid";
                step.LastErrorMessage = "switch step case field 'when' must be valid JSON.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            if (JsonNode.DeepEquals(valueNode, whenNode))
            {
                selectedDoEl = doEl;
                break;
            }
        }

        if (selectedDoEl is null)
        {
            if (defaultDoEl is null)
            {
                step.LastErrorCode = "switch_no_match";
                step.LastErrorMessage = "switch step had no matching case and no default branch.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            selectedDoEl = defaultDoEl;
        }

        if (!selectedDoEl.Value.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "switch_do_type_missing";
            step.LastErrorMessage = "switch inner step must contain a string 'type'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var innerType = (typeEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(innerType))
        {
            step.LastErrorCode = "switch_do_type_missing";
            step.LastErrorMessage = "switch inner step must contain a non-empty string 'type'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var innerStep = new WorkflowStepRun
        {
            WorkflowStepRunId = Guid.Empty,
            WorkflowRunId = step.WorkflowRunId,
            StepKey = $"{step.StepKey}.branch",
            StepType = innerType,
            StepConfigJson = selectedDoEl.Value.GetRawText(),
            State = WorkflowStepRunStates.Pending,
        };

        try
        {
            InterpolateStepConfigJson(innerStep, context);
            await ExecuteStepBodyAsync(innerStep, context, ct);
            step.OutputJson = innerStep.OutputJson;
        }
        catch (Exception ex)
        {
            step.LastErrorCode = innerStep.LastErrorCode ?? "switch_inner_failed";
            step.LastErrorMessage = innerStep.LastErrorMessage ?? ex.Message;
            throw;
        }
    }

    private async Task ExecuteDomainCommandAsync(WorkflowStepRun step, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(step.StepConfigJson))
        {
            step.LastErrorCode = "domain_command_config_missing";
            step.LastErrorMessage = "domainCommand step requires a JSON config.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        using var doc = JsonDocument.Parse(step.StepConfigJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "domain_command_missing";
            step.LastErrorMessage = "domainCommand step requires a string 'command' field.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var command = (cmdEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            step.LastErrorCode = "domain_command_missing";
            step.LastErrorMessage = "domainCommand step requires a non-empty 'command'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        switch (command.ToLowerInvariant())
        {
            case "echo":
                // Demo command: does nothing besides succeeding.
                return;

            case "entityrecord.createbyentityname":
                await ExecuteCreateRecordByEntityNameAsync(root, step, ct);
                return;

            case "entityrecord.updatebyid":
                await ExecuteUpdateRecordByIdAsync(root, step, ct);
                return;

            case "entityrecord.deletebyid":
                await ExecuteDeleteRecordByIdAsync(root, step, ct);
                return;

            case "entityrecord.upsertbyentityname":
                await ExecuteUpsertRecordByEntityNameAsync(root, step, ct);
                return;

            default:
                step.LastErrorCode = "domain_command_unknown";
                step.LastErrorMessage = $"Unknown domain command: '{command}'.";
                throw new InvalidOperationException(step.LastErrorMessage);
        }
    }

    private async Task ExecuteCreateRecordByEntityNameAsync(JsonElement root, WorkflowStepRun step, CancellationToken ct)
    {
        if (!root.TryGetProperty("entityName", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "entity_name_missing";
            step.LastErrorMessage = "entityRecord.createByEntityName requires a string 'entityName'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var entityName = (nameEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(entityName))
        {
            step.LastErrorCode = "entity_name_missing";
            step.LastErrorMessage = "entityRecord.createByEntityName requires a non-empty 'entityName'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var dataJson = "{}";
        if (root.TryGetProperty("data", out var dataEl))
        {
            if (dataEl.ValueKind == JsonValueKind.Object)
                dataJson = dataEl.GetRawText();
            else if (dataEl.ValueKind == JsonValueKind.String)
                dataJson = dataEl.GetString() ?? "{}";
            else
            {
                step.LastErrorCode = "entity_record_data_invalid";
                step.LastErrorMessage = "entityRecord.createByEntityName requires 'data' to be an object or string.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }

        var entity = await _db.EntityDefinitions.FirstOrDefaultAsync(x => x.Name == entityName, ct);
        if (entity is null)
        {
            entity = new EntityDefinition
            {
                EntityDefinitionId = Guid.NewGuid(),
                Name = entityName,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            _db.EntityDefinitions.Add(entity);
        }

        var record = new EntityRecord
        {
            EntityRecordId = Guid.NewGuid(),
            EntityDefinitionId = entity.EntityDefinitionId,
            DataJson = dataJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.EntityRecords.Add(record);
        step.OutputJson = $"{{\"entityDefinitionId\":\"{entity.EntityDefinitionId}\",\"entityRecordId\":\"{record.EntityRecordId}\"}}";
        await Task.CompletedTask;
    }

    private async Task ExecuteUpdateRecordByIdAsync(JsonElement root, WorkflowStepRun step, CancellationToken ct)
    {
        if (!root.TryGetProperty("recordId", out var idEl))
        {
            step.LastErrorCode = "entity_record_id_missing";
            step.LastErrorMessage = "entityRecord.updateById requires 'recordId'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        Guid recordId;
        if (idEl.ValueKind == JsonValueKind.String)
        {
            var raw = (idEl.GetString() ?? string.Empty).Trim();
            if (!Guid.TryParse(raw, out recordId))
            {
                step.LastErrorCode = "entity_record_id_invalid";
                step.LastErrorMessage = "entityRecord.updateById requires a valid GUID 'recordId'.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }
        else
        {
            step.LastErrorCode = "entity_record_id_invalid";
            step.LastErrorMessage = "entityRecord.updateById requires a string GUID 'recordId'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var dataJson = "{}";
        if (root.TryGetProperty("data", out var dataEl))
        {
            if (dataEl.ValueKind == JsonValueKind.Object)
                dataJson = dataEl.GetRawText();
            else if (dataEl.ValueKind == JsonValueKind.String)
                dataJson = dataEl.GetString() ?? "{}";
            else
            {
                step.LastErrorCode = "entity_record_data_invalid";
                step.LastErrorMessage = "entityRecord.updateById requires 'data' to be an object or string.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }

        var record = await _db.EntityRecords.FirstOrDefaultAsync(x => x.EntityRecordId == recordId, ct);
        if (record is null)
        {
            step.LastErrorCode = "entity_record_not_found";
            step.LastErrorMessage = "Record not found.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        record.DataJson = dataJson;
        record.UpdatedAtUtc = DateTime.UtcNow;

        step.OutputJson = $"{{\"entityRecordId\":\"{record.EntityRecordId}\"}}";
    }

    private async Task ExecuteDeleteRecordByIdAsync(JsonElement root, WorkflowStepRun step, CancellationToken ct)
    {
        if (!root.TryGetProperty("recordId", out var idEl))
        {
            step.LastErrorCode = "entity_record_id_missing";
            step.LastErrorMessage = "entityRecord.deleteById requires 'recordId'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        Guid recordId;
        if (idEl.ValueKind == JsonValueKind.String)
        {
            var raw = (idEl.GetString() ?? string.Empty).Trim();
            if (!Guid.TryParse(raw, out recordId))
            {
                step.LastErrorCode = "entity_record_id_invalid";
                step.LastErrorMessage = "entityRecord.deleteById requires a valid GUID 'recordId'.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }
        else
        {
            step.LastErrorCode = "entity_record_id_invalid";
            step.LastErrorMessage = "entityRecord.deleteById requires a string GUID 'recordId'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var record = await _db.EntityRecords.FirstOrDefaultAsync(x => x.EntityRecordId == recordId, ct);
        if (record is null)
        {
            step.LastErrorCode = "entity_record_not_found";
            step.LastErrorMessage = "Record not found.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        _db.EntityRecords.Remove(record);
        step.OutputJson = $"{{\"entityRecordId\":\"{record.EntityRecordId}\"}}";
    }

    private async Task ExecuteUpsertRecordByEntityNameAsync(JsonElement root, WorkflowStepRun step, CancellationToken ct)
    {
        if (!root.TryGetProperty("entityName", out var nameEl) || nameEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "entity_name_missing";
            step.LastErrorMessage = "entityRecord.upsertByEntityName requires a string 'entityName'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var entityName = (nameEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(entityName))
        {
            step.LastErrorCode = "entity_name_missing";
            step.LastErrorMessage = "entityRecord.upsertByEntityName requires a non-empty 'entityName'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        if (!root.TryGetProperty("uniqueKey", out var uniqueKeyEl) || uniqueKeyEl.ValueKind != JsonValueKind.String)
        {
            step.LastErrorCode = "entity_record_unique_key_missing";
            step.LastErrorMessage = "entityRecord.upsertByEntityName requires a string 'uniqueKey'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var uniqueKey = (uniqueKeyEl.GetString() ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(uniqueKey))
        {
            step.LastErrorCode = "entity_record_unique_key_missing";
            step.LastErrorMessage = "entityRecord.upsertByEntityName requires a non-empty 'uniqueKey'.";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var dataJson = "{}";
        JsonElement? dataObj = null;
        if (root.TryGetProperty("data", out var dataEl))
        {
            if (dataEl.ValueKind == JsonValueKind.Object)
            {
                dataJson = dataEl.GetRawText();
                dataObj = dataEl;
            }
            else if (dataEl.ValueKind == JsonValueKind.String)
            {
                dataJson = dataEl.GetString() ?? "{}";
            }
            else
            {
                step.LastErrorCode = "entity_record_data_invalid";
                step.LastErrorMessage = "entityRecord.upsertByEntityName requires 'data' to be an object or string.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }
        }

        if (string.IsNullOrWhiteSpace(dataJson))
            dataJson = "{}";

        string? uniqueValue = null;
        if (root.TryGetProperty("uniqueValue", out var uniqueValueEl))
        {
            if (uniqueValueEl.ValueKind != JsonValueKind.String)
            {
                step.LastErrorCode = "entity_record_unique_value_invalid";
                step.LastErrorMessage = "entityRecord.upsertByEntityName field 'uniqueValue' must be a string when provided.";
                throw new InvalidOperationException(step.LastErrorMessage);
            }

            uniqueValue = (uniqueValueEl.GetString() ?? string.Empty).Trim();
        }

        if (string.IsNullOrWhiteSpace(uniqueValue) && dataObj.HasValue)
        {
            if (dataObj.Value.TryGetProperty(uniqueKey, out var inDataEl) && inDataEl.ValueKind == JsonValueKind.String)
                uniqueValue = (inDataEl.GetString() ?? string.Empty).Trim();
        }

        if (string.IsNullOrWhiteSpace(uniqueValue))
        {
            step.LastErrorCode = "entity_record_unique_value_missing";
            step.LastErrorMessage = "entityRecord.upsertByEntityName requires a non-empty uniqueValue (either explicit or via data[uniqueKey]).";
            throw new InvalidOperationException(step.LastErrorMessage);
        }

        var entity = await _db.EntityDefinitions.FirstOrDefaultAsync(x => x.Name == entityName, ct);
        if (entity is null)
        {
            entity = new EntityDefinition
            {
                EntityDefinitionId = Guid.NewGuid(),
                Name = entityName,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            _db.EntityDefinitions.Add(entity);
        }

        // NOTE: naive upsert implementation - scans records and parses JSON. Good enough for now; can be optimized later.
        var records = await _db.EntityRecords
            .Where(x => x.EntityDefinitionId == entity.EntityDefinitionId)
            .ToListAsync(ct);

        EntityRecord? existing = null;
        foreach (var r in records)
        {
            if (string.IsNullOrWhiteSpace(r.DataJson))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(r.DataJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    continue;

                if (doc.RootElement.TryGetProperty(uniqueKey, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    var candidate = (v.GetString() ?? string.Empty).Trim();
                    if (string.Equals(candidate, uniqueValue, StringComparison.Ordinal))
                    {
                        existing = r;
                        break;
                    }
                }
            }
            catch
            {
                // ignore invalid record json
            }
        }

        if (existing is null)
        {
            var record = new EntityRecord
            {
                EntityRecordId = Guid.NewGuid(),
                EntityDefinitionId = entity.EntityDefinitionId,
                DataJson = dataJson,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            _db.EntityRecords.Add(record);
            step.OutputJson = $"{{\"action\":\"created\",\"entityDefinitionId\":\"{entity.EntityDefinitionId}\",\"entityRecordId\":\"{record.EntityRecordId}\"}}";
            return;
        }

        existing.DataJson = dataJson;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        step.OutputJson = $"{{\"action\":\"updated\",\"entityDefinitionId\":\"{entity.EntityDefinitionId}\",\"entityRecordId\":\"{existing.EntityRecordId}\"}}";
    }
}
