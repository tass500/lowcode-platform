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
                obj[kv.Key] = RewriteNode(kv.Value, context, step);
            }
            return obj;
        }

        if (node is JsonArray arr)
        {
            for (var i = 0; i < arr.Count; i += 1)
            {
                if (arr[i] is null)
                    continue;
                arr[i] = RewriteNode(arr[i]!, context, step);
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
                    step.LastErrorMessage = $"Context variable not found: '{path}'.";
                    throw new InvalidOperationException(step.LastErrorMessage);
                }

                return resolved.DeepClone();
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
                    step.LastErrorMessage = $"Context variable not found: '{path}'.";
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
        step.Attempt += 1;
        step.State = WorkflowStepRunStates.Running;
        step.StartedAtUtc = DateTime.UtcNow;
        step.LastErrorCode = null;
        step.LastErrorMessage = null;
        step.OutputJson = null;

        try
        {
            InterpolateStepConfigJson(step, context);

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

                case "noop":
                    break;

                default:
                    step.LastErrorCode = "workflow_step_type_not_supported";
                    step.LastErrorMessage = $"Unsupported workflow step type: '{step.StepType}'.";
                    throw new InvalidOperationException(step.LastErrorMessage);
            }

            step.State = WorkflowStepRunStates.Succeeded;
            step.FinishedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            step.State = WorkflowStepRunStates.Failed;
            step.FinishedAtUtc = DateTime.UtcNow;
            step.LastErrorCode ??= "workflow_step_failed";
            step.LastErrorMessage ??= ex.Message;

            run.ErrorCode = step.LastErrorCode;
            run.ErrorMessage = step.LastErrorMessage;
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
