/**
 * Context variable path suggestions for workflow definition JSON (authoring UX).
 */

export function domainCommandOutputKeys(command: string): string[] {
  const c = command.trim().toLowerCase();
  if (c === 'entityrecord.createbyentityname') return ['entityDefinitionId', 'entityRecordId'];
  if (c === 'entityrecord.updatebyid' || c === 'entityrecord.deletebyid') return ['entityRecordId'];
  if (c === 'entityrecord.upsertbyentityname') return ['action', 'entityDefinitionId', 'entityRecordId'];
  return [];
}

/**
 * Suggested `${...}` paths from a parsed `steps` array (best-effort static analysis).
 */
export function buildContextVarSuggestionsFromSteps(steps: unknown): string[] {
  if (!Array.isArray(steps)) return [];

  const suggestions: string[] = [];
  const push = (s: string) => {
    if (s) suggestions.push(s);
  };

  for (let i = 0; i < steps.length; i += 1) {
    const key = String(i).padStart(3, '0');
    push(key);

    const step = steps[i];
    if (!step || typeof step !== 'object' || Array.isArray(step)) continue;
    const s = step as Record<string, unknown>;
    const type = String(s['type'] ?? '').toLowerCase();

    if (type === 'set' && s['output'] && typeof s['output'] === 'object' && !Array.isArray(s['output'])) {
      for (const k of Object.keys(s['output'] as object)) {
        if (k) push(`${key}.${k}`);
      }
    }

    if (type === 'map' && s['mappings'] && typeof s['mappings'] === 'object' && !Array.isArray(s['mappings'])) {
      for (const k of Object.keys(s['mappings'] as object)) {
        if (k) push(`${key}.${k}`);
      }
    }

    if (type === 'domaincommand') {
      const cmd = String(s['command'] ?? '').trim();
      if (cmd) {
        for (const o of domainCommandOutputKeys(cmd)) {
          push(`${key}.${o}`);
        }
      }
    }

    if (type === 'foreach') {
      push('foreach.index');
      push('foreach.item');
      let asVar = 'item';
      if (typeof s['as'] === 'string' && s['as'].trim()) asVar = s['as'].trim();
      push(asVar);

      const doNode = s['do'];
      if (doNode && typeof doNode === 'object' && !Array.isArray(doNode)) {
        const d = doNode as Record<string, unknown>;
        const innerType = String(d['type'] ?? '').toLowerCase();
        // Inner step key for first iteration is `${parentKey}.000` (see engine).
        const innerPrefix = `${key}.000`;
        if (innerType === 'map' && d['mappings'] && typeof d['mappings'] === 'object' && !Array.isArray(d['mappings'])) {
          push(innerPrefix);
          for (const mk of Object.keys(d['mappings'] as object)) {
            if (mk) push(`${innerPrefix}.${mk}`);
          }
        }
        if (innerType === 'set' && d['output'] && typeof d['output'] === 'object' && !Array.isArray(d['output'])) {
          push(innerPrefix);
          for (const ok of Object.keys(d['output'] as object)) {
            if (ok) push(`${innerPrefix}.${ok}`);
          }
        }
      }
    }
  }

  return Array.from(new Set(suggestions)).sort((a, b) => a.localeCompare(b));
}

export function buildContextVarSuggestionsFromDefinitionJson(definitionJson: string): string[] {
  try {
    const parsed: unknown = JSON.parse(definitionJson);
    if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) return [];
    const steps = (parsed as { steps?: unknown }).steps;
    return buildContextVarSuggestionsFromSteps(steps);
  } catch {
    return [];
  }
}
