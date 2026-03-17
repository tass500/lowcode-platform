export function formatError(e: any, fallback: string): string {
  return e?.error?.message ?? e?.message ?? fallback;
}
