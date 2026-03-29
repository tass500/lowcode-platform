export type LowCodeSession = {
  /** May be empty after OIDC if the id token had no configured tenant claim; set manually on the auth page. */
  tenantSlug: string;
  accessToken: string;
  refreshToken?: string;
  oidcTokenEndpoint?: string;
  oidcClientId?: string;
};

const KEY = 'lcp.lowcode.session.v1';

export function getLowCodeSession(): LowCodeSession | null {
  try {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<LowCodeSession>;
    if (!parsed || typeof parsed !== 'object') return null;
    if (!parsed.accessToken) return null;
    return {
      tenantSlug: parsed.tenantSlug != null ? String(parsed.tenantSlug) : '',
      accessToken: String(parsed.accessToken),
      refreshToken: parsed.refreshToken ? String(parsed.refreshToken) : undefined,
      oidcTokenEndpoint: parsed.oidcTokenEndpoint ? String(parsed.oidcTokenEndpoint) : undefined,
      oidcClientId: parsed.oidcClientId ? String(parsed.oidcClientId) : undefined,
    };
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
