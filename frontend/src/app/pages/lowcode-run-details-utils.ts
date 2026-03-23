/**
 * Helpers for workflow run details (original vs interpolated step config).
 */

/** True if original and resolved JSON strings differ (after trim). */
export function stepConfigsDiffer(
  originalStepConfigJson: string | null | undefined,
  stepConfigJson: string | null | undefined,
): boolean {
  const a = String(originalStepConfigJson ?? '').trim();
  const b = String(stepConfigJson ?? '').trim();
  if (!a && !b) return false;
  return a !== b;
}
