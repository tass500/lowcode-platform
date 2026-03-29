/**
 * Minimal visual builder mutations (iter 58) — keeps JSON as source of truth.
 * Step keys at runtime are 000, 001, … by array index; reordering can break ${000.*} references.
 */

export type BuilderStepType =
  | 'noop'
  | 'delay'
  | 'set'
  | 'map'
  | 'merge'
  | 'foreach'
  | 'switch'
  | 'require'
  | 'domainCommand'
  | 'unstable';

export type BuilderStepPaletteItem = {
  type: BuilderStepType;
  label: string;
};

/** Ordered palette for the Builder UI (labels are short for buttons). */
export const WORKFLOW_BUILDER_PALETTE: readonly BuilderStepPaletteItem[] = [
  { type: 'noop', label: 'noop' },
  { type: 'delay', label: 'delay' },
  { type: 'set', label: 'set' },
  { type: 'map', label: 'map' },
  { type: 'merge', label: 'merge' },
  { type: 'require', label: 'require' },
  { type: 'foreach', label: 'foreach' },
  { type: 'switch', label: 'switch' },
  { type: 'domainCommand', label: 'domainCommand' },
  { type: 'unstable', label: 'unstable' },
];

export function buildMinimalStep(type: BuilderStepType): Record<string, unknown> {
  switch (type) {
    case 'noop':
      return { type: 'noop' };
    case 'delay':
      return { type: 'delay', ms: 100 };
    case 'set':
      return { type: 'set', output: {} };
    case 'map':
      return { type: 'map', mappings: {} };
    case 'merge':
      return { type: 'merge', sources: [] };
    case 'require':
      return { type: 'require', path: '000.example' };
    case 'foreach':
      return { type: 'foreach', items: [], do: { type: 'noop' } };
    case 'switch':
      return {
        type: 'switch',
        value: '000.kind',
        cases: [{ when: 'a', do: { type: 'noop' } }],
        default: { type: 'noop' },
      };
    case 'domainCommand':
      return { type: 'domainCommand', command: 'echo' };
    case 'unstable':
      return { type: 'unstable' };
    default:
      return { type: 'noop' };
  }
}

export function mutateWorkflowDefinitionSteps(rawJson: string, mutator: (steps: unknown[]) => unknown[]): string {
  const parsed = JSON.parse(rawJson) as unknown;
  if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
    throw new Error('Definition must be a JSON object.');
  }
  const obj = parsed as Record<string, unknown>;
  const steps = obj['steps'];
  if (!Array.isArray(steps)) {
    throw new Error('Definition must include a `steps` array.');
  }
  const nextSteps = mutator([...steps]);
  if (!Array.isArray(nextSteps)) {
    throw new Error('steps mutator must return an array.');
  }
  const out = { ...obj, steps: nextSteps };
  return JSON.stringify(out, null, 2);
}

export function appendBuilderStep(rawJson: string, type: BuilderStepType): string {
  return mutateWorkflowDefinitionSteps(rawJson, steps => {
    steps.push(buildMinimalStep(type));
    return steps;
  });
}

export function removeBuilderStepAt(rawJson: string, index: number): string {
  return mutateWorkflowDefinitionSteps(rawJson, steps => {
    if (index < 0 || index >= steps.length) return steps;
    steps.splice(index, 1);
    return steps;
  });
}

export function moveBuilderStep(rawJson: string, fromIndex: number, toIndex: number): string {
  return mutateWorkflowDefinitionSteps(rawJson, steps => {
    if (fromIndex < 0 || fromIndex >= steps.length) return steps;
    if (toIndex < 0 || toIndex >= steps.length) return steps;
    if (fromIndex === toIndex) return steps;
    const [item] = steps.splice(fromIndex, 1);
    steps.splice(toIndex, 0, item);
    return steps;
  });
}

/**
 * Reorder using a "slot" before indices 0..n (n = stepCount means after the last step).
 * Used by builder drag-and-drop: drop above row midpoint → slot = rowIndex; below → slot = rowIndex + 1.
 */
export function moveBuilderStepToSlot(
  rawJson: string,
  fromIndex: number,
  targetSlot: number,
  stepCount: number,
): string {
  const n = stepCount;
  if (n <= 0 || fromIndex < 0 || fromIndex >= n) return rawJson;
  const slot = Math.max(0, Math.min(targetSlot, n));
  const insertIndex = fromIndex < slot ? slot - 1 : slot;
  if (insertIndex === fromIndex) return rawJson;
  return moveBuilderStep(rawJson, fromIndex, insertIndex);
}

export function parseBuilderStepSummaries(rawJson: string): Array<{ index: number; type: string }> {
  try {
    const parsed = JSON.parse(rawJson) as { steps?: unknown };
    const steps = parsed?.steps;
    if (!Array.isArray(steps)) return [];
    return steps.map((s, index) => {
      if (s !== null && typeof s === 'object' && !Array.isArray(s)) {
        const o = s as Record<string, unknown>;
        const t = o['type'];
        return { index, type: typeof t === 'string' ? t : '?' };
      }
      return { index, type: '?' };
    });
  } catch {
    return [];
  }
}
