/**
 * Pure helpers for workflow lint warning presentation (grouping, sorting).
 * Keeps UI components thin and testable.
 */

export type WorkflowLintWarning = { code: string; message: string };

export type LintWarningGroup = {
  code: string;
  count: number;
  messages: string[];
};

export function groupLintWarningsByCode(
  warnings: WorkflowLintWarning[] | null | undefined
): LintWarningGroup[] {
  const map = new Map<string, string[]>();
  for (const w of warnings ?? []) {
    const code = String(w?.code ?? '').trim() || 'unknown';
    const msg = String(w?.message ?? '');
    if (!map.has(code)) map.set(code, []);
    map.get(code)!.push(msg);
  }
  return [...map.entries()]
    .sort((a, b) => a[0].localeCompare(b[0]))
    .map(([code, messages]) => ({ code, count: messages.length, messages }));
}
