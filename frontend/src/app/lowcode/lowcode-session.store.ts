export type LowCodeSession = {
  tenantSlug: string;
  accessToken: string;
};

const KEY = 'lcp.lowcode.session.v1';

export function getLowCodeSession(): LowCodeSession | null {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<LowCodeSession>;
    if (!parsed || typeof parsed !== 'object') return null;
    if (!parsed.tenantSlug || !parsed.accessToken) return null;
    return { tenantSlug: String(parsed.tenantSlug), accessToken: String(parsed.accessToken) };
  } catch {
    return null;
  }
}

export function setLowCodeSession(session: LowCodeSession | null): void {
  try {
    if (!session) {
      localStorage.removeItem(KEY);
      return;
    }
    localStorage.setItem(KEY, JSON.stringify(session));
  } catch {
    // ignore
  }
}
