using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using LowCodePlatform.Backend.Contracts;

namespace LowCodePlatform.Backend.Services;

/// <summary>
/// Best-effort static analysis for workflow definitions (warnings only; never throws).
/// </summary>
public static class WorkflowDefinitionLinter
{
    private static readonly HashSet<string> SupportedStepTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "noop",
        "delay",
        "set",
        "map",
        "merge",
        "foreach",
        "switch",
        "require",
        "domainCommand",
        "unstable",
    };

    private static readonly Regex ContextVarRegex = new(@"\$\{(?<path>[^}]+)\}", RegexOptions.Compiled);

    /// <summary>Known misspellings of common context paths → suggested fix (iter 41).</summary>
    private static readonly (string Wrong, string Hint)[] ContextPathLikelyTypos =
    {
        ("foreach.indx", "foreach.index"),
        ("foreach.itm", "foreach.item"),
        ("foreach.itme", "foreach.item"),
        ("forech.index", "foreach.index"),
        ("forech.item", "foreach.item"),
    };

    public static List<WorkflowLintWarningDto> Lint(string definitionJson)
    {
        var warnings = new List<WorkflowLintWarningDto>();

        try
        {
            using var doc = JsonDocument.Parse(definitionJson);
            if (!doc.RootElement.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != JsonValueKind.Array)
                return warnings;

            var stepKeys = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < stepsEl.GetArrayLength(); i += 1)
                stepKeys.Add($"{i:000}");

            foreach (var stepEl in stepsEl.EnumerateArray())
            {
                if (stepEl.ValueKind != JsonValueKind.Object)
                    continue;

                if (stepEl.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
                {
                    var type = (typeEl.GetString() ?? string.Empty).Trim();
                    if (!string.IsNullOrWhiteSpace(type) && !SupportedStepTypes.Contains(type))
                    {
                        warnings.Add(new WorkflowLintWarningDto(
                            Code: "workflow_step_type_unknown",
                            Message: $"Unknown workflow step type: '{type}'."));
                    }
                }

                foreach (var m in ContextVarRegex.Matches(stepEl.GetRawText()).Cast<Match>())
                {
                    var path = m.Groups["path"].Value;
                    if (path.Length < 3)
                        continue;

                    var stepKey = path[..3];
                    if (stepKey.All(char.IsDigit) && !stepKeys.Contains(stepKey))
                    {
                        warnings.Add(new WorkflowLintWarningDto(
                            Code: "context_var_step_not_found",
                            Message: $"Context variable references missing step '{stepKey}': '${{{path}}}'."));
                    }
                }
            }

            var referencedPaths = CollectReferencedPaths(definitionJson);
            AppendLikelyTypoWarnings(warnings, referencedPaths);
            AppendUnusedStepOutputWarnings(warnings, stepsEl, referencedPaths);
        }
        catch
        {
            // ignore lint failures
        }

        return warnings;
    }

    private static HashSet<string> CollectReferencedPaths(string definitionJson)
    {
        var refs = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in ContextVarRegex.Matches(definitionJson))
        {
            var inner = m.Groups["path"].Value.Trim();
            if (inner.Length > 0)
                refs.Add(inner);
        }

        return refs;
    }

    private static void AppendLikelyTypoWarnings(List<WorkflowLintWarningDto> warnings, HashSet<string> referencedPaths)
    {
        foreach (var path in referencedPaths)
        {
            foreach (var (wrong, hint) in ContextPathLikelyTypos)
            {
                if (string.Equals(path, wrong, StringComparison.Ordinal))
                {
                    warnings.Add(new WorkflowLintWarningDto(
                        Code: "workflow_context_likely_typo",
                        Message: $"Context variable '${{{path}}}' looks like a typo; did you mean '{hint}'?"));
                    break;
                }
            }
        }
    }

    private static void AppendUnusedStepOutputWarnings(
        List<WorkflowLintWarningDto> warnings,
        JsonElement stepsEl,
        HashSet<string> referencedPaths)
    {
        var i = 0;
        foreach (var stepEl in stepsEl.EnumerateArray())
        {
            if (stepEl.ValueKind != JsonValueKind.Object)
            {
                i += 1;
                continue;
            }

            var stepKey = $"{i:000}";
            foreach (var produced in GetProducedContextPaths(stepKey, stepEl))
            {
                if (!IsProducedPathUsed(produced, stepKey, referencedPaths))
                {
                    warnings.Add(new WorkflowLintWarningDto(
                        Code: "workflow_step_output_unused",
                        Message: $"Step '{stepKey}' output '{produced}' is never referenced by a context variable."));
                }
            }

            i += 1;
        }
    }

    private static bool IsProducedPathUsed(string producedPath, string stepKey, HashSet<string> referencedPaths)
    {
        if (referencedPaths.Contains(producedPath))
            return true;
        if (referencedPaths.Contains(stepKey))
            return true;
        foreach (var r in referencedPaths)
        {
            if (r.StartsWith(producedPath + ".", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    /// <summary>Static paths a step may write to runtime context when it succeeds (set/map/domainCommand).</summary>
    private static IEnumerable<string> GetProducedContextPaths(string stepKey, JsonElement stepEl)
    {
        if (!stepEl.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String)
            yield break;

        var type = (typeEl.GetString() ?? string.Empty).Trim();
        switch (type.ToLowerInvariant())
        {
            case "set":
                if (stepEl.TryGetProperty("output", out var outEl) && outEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in outEl.EnumerateObject())
                        yield return $"{stepKey}.{p.Name}";
                }

                break;
            case "map":
                if (stepEl.TryGetProperty("mappings", out var mapEl) && mapEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in mapEl.EnumerateObject())
                        yield return $"{stepKey}.{p.Name}";
                }

                break;
            case "domaincommand":
                if (!stepEl.TryGetProperty("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String)
                    yield break;
                var cmd = (cmdEl.GetString() ?? string.Empty).Trim();
                foreach (var k in DomainCommandOutputKeys(cmd))
                    yield return $"{stepKey}.{k}";
                break;
        }
    }

    private static IEnumerable<string> DomainCommandOutputKeys(string command)
    {
        var c = command.Trim().ToLowerInvariant();
        if (c == "entityrecord.createbyentityname")
        {
            yield return "entityDefinitionId";
            yield return "entityRecordId";
        }
        else if (c is "entityrecord.updatebyid" or "entityrecord.deletebyid")
        {
            yield return "entityRecordId";
        }
        else if (c == "entityrecord.upsertbyentityname")
        {
            yield return "action";
            yield return "entityDefinitionId";
            yield return "entityRecordId";
        }
    }
}
