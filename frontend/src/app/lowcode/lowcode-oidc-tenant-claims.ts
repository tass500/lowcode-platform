export function pickTenantFromClaims(
  claims: Record<string, unknown> | null | undefined,
  sources: string[]
): string | null {
  if (!claims) return null;
  for (const key of sources) {
    const v = claims[key];
    if (typeof v === 'string' && v.trim()) return v.trim();
  }
  return null;
}
