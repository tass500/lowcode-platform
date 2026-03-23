/**
 * Read-only workflow definition viewer helpers (iter 44 — viewer v2).
 */

export type WorkflowViewerStepCard = {
  index: number;
  /** 3-digit step key e.g. "000" */
  stepKey: string;
  type: string;
  title: string;
  /** One-line summary under the title */
  subtitle: string | null;
  /** Extra line for control-flow steps (foreach / switch) */
  branchPreview: string | null;
};

function trunc(s: string, max: number): string {
  const t = s.trim();
  if (t.length <= max) return t;
  return `${t.slice(0, max - 1)}…`;
}

function str(v: unknown): string {
  if (v === null || v === undefined) return '';
  if (typeof v === 'string') return v;
  if (typeof v === 'number' || typeof v === 'boolean') return String(v);
  return '';
}

function innerStepType(doNode: unknown): string {
  if (!doNode || typeof doNode !== 'object' || Array.isArray(doNode)) return '?';
  const t = (doNode as { type?: unknown }).type;
  return typeof t === 'string' && t.trim() ? t.trim() : '?';
}

/**
 * Build viewer cards from parsed `steps` array items (each item is a step object).
 */
export function buildWorkflowViewerStepCards(steps: unknown): WorkflowViewerStepCard[] {
  if (!Array.isArray(steps)) return [];

  return steps.map((raw, index) => {
    const stepKey = String(index).padStart(3, '0');
    const s = raw && typeof raw === 'object' && !Array.isArray(raw) ? (raw as Record<string, unknown>) : {};
    const typeRaw = typeof s['type'] === 'string' ? (s['type'] as string).trim() : '';
    const type = typeRaw || 'noop';
    const t = type.toLowerCase();

    let title = type;
    let subtitle: string | null = null;
    let branchPreview: string | null = null;

    switch (t) {
      case 'delay': {
        title = 'delay';
        const ms = s['ms'];
        subtitle = typeof ms === 'number' && Number.isFinite(ms) ? `${ms} ms` : 'duration not set';
        const to = s['timeoutMs'];
        if (typeof to === 'number' && Number.isFinite(to))
          subtitle = `${subtitle} · timeout ${to} ms`;
        break;
      }
      case 'require': {
        title = 'require';
        const path = str(s['path']);
        subtitle = path ? `path: ${trunc(path, 80)}` : 'path not set';
        break;
      }
      case 'domaincommand': {
        title = 'domainCommand';
        const cmd = str(s['command']);
        subtitle = cmd ? trunc(cmd, 96) : 'command not set';
        const extras: string[] = [];
        const en = str(s['entityName']);
        if (en) extras.push(`entityName: ${trunc(en, 40)}`);
        const rn = str(s['recordId']);
        if (rn) extras.push(`recordId: ${trunc(rn, 48)}`);
        if (extras.length) subtitle = `${subtitle} · ${extras.join(' · ')}`;
        break;
      }
      case 'set': {
        title = 'set';
        const out = s['output'];
        if (out && typeof out === 'object' && !Array.isArray(out)) {
          const keys = Object.keys(out as object);
          subtitle = keys.length ? `output: ${keys.slice(0, 6).join(', ')}${keys.length > 6 ? '…' : ''}` : 'empty output';
        } else if (typeof s['outputJson'] === 'string' && (s['outputJson'] as string).trim())
          subtitle = 'outputJson string';
        else subtitle = 'output not set';
        break;
      }
      case 'map': {
        title = 'map';
        const m = s['mappings'];
        if (m && typeof m === 'object' && !Array.isArray(m)) {
          const keys = Object.keys(m as object);
          subtitle = keys.length ? `${keys.length} mapping(s)` : 'no mappings';
        } else subtitle = 'mappings not set';
        break;
      }
      case 'merge': {
        title = 'merge';
        const sources = s['sources'];
        subtitle = Array.isArray(sources) ? `${sources.length} source(s)` : 'sources not set';
        break;
      }
      case 'foreach': {
        title = 'foreach';
        const items = str(s['items']);
        subtitle = items ? `items: ${trunc(items, 72)}` : 'items not set';
        const inner = innerStepType(s['do']);
        branchPreview = `inner step: ${inner}`;
        break;
      }
      case 'switch': {
        title = 'switch';
        const val = str(s['value']);
        subtitle = val ? `value: ${trunc(val, 72)}` : 'value not set';
        const cases = s['cases'];
        const n = Array.isArray(cases) ? cases.length : 0;
        branchPreview = n ? `${n} case(s)` : 'no cases';
        if (s['default'] !== undefined && s['default'] !== null)
          branchPreview = `${branchPreview ?? ''} · default branch`.trim();
        break;
      }
      case 'noop':
        title = 'noop';
        subtitle = null;
        break;
      default:
        title = type;
        subtitle = null;
    }

    return { index, stepKey, type, title, subtitle, branchPreview };
  });
}

/**
 * Try to find a character index in `definitionJson` to place the caret near step `stepIndex`.
 * Returns -1 if not found (caller should only focus the textarea).
 */
export function findCaretIndexForWorkflowStep(definitionJson: string, stepIndex: number): number {
  let parsed: { steps?: unknown[] };
  try {
    parsed = JSON.parse(definitionJson) as { steps?: unknown[] };
  } catch {
    return -1;
  }

  const steps = parsed?.steps;
  if (!Array.isArray(steps) || stepIndex < 0 || stepIndex >= steps.length) return -1;

  const step = steps[stepIndex];
  const needle = JSON.stringify(step);
  if (!needle) return -1;

  let idx = definitionJson.indexOf(needle);
  if (idx >= 0) return idx;

  // Minified vs pretty: try normalize by re-stringifying from parsed step
  try {
    const again = JSON.stringify(JSON.parse(needle));
    idx = definitionJson.indexOf(again);
    if (idx >= 0) return idx;
  } catch {
    /* ignore */
  }

  return -1;
}
