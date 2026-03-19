using System.Text.Json;
using LowCodePlatform.Backend.Data;
using LowCodePlatform.Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace LowCodePlatform.Backend.Services;

public sealed class WorkflowRunnerService
{
    private readonly PlatformDbContext _db;

    public WorkflowRunnerService(PlatformDbContext db)
    {
        _db = db;
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

        _db.WorkflowRuns.Add(run);
        await _db.SaveChangesAsync(ct);

        foreach (var step in run.Steps.OrderBy(x => x.StepKey))
        {
            await ExecuteStepAsync(run, step, ct);
            await _db.SaveChangesAsync(ct);

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

    private async Task ExecuteStepAsync(WorkflowRun run, WorkflowStepRun step, CancellationToken ct)
    {
        step.Attempt += 1;
        step.State = WorkflowStepRunStates.Running;
        step.StartedAtUtc = DateTime.UtcNow;
        step.LastErrorCode = null;
        step.LastErrorMessage = null;
        step.OutputJson = null;

        try
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
}
