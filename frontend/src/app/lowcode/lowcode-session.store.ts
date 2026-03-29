export type LowCodeSession = {
  /** May be empty after OIDC if the id token had no configured tenant claim; set manually on the auth page. */
  tenantSlug: string;
  accessToken: string;
  refreshToken?: string;
  oidcTokenEndpoint?: string;
  oidcClientId?: string;
};

const KEY = 'lcp.lowcode.session.v1';

/** Tab-scoped storage limits exposure vs localStorage; OIDC libs typically use the same. */
function readStoredRaw(): string | null {
  try {
    const fromSession = sessionStorage.getItem(KEY);
    if (fromSession != null) return fromSession;
    const legacy = localStorage.getItem(KEY);
    if (legacy != null) {
      try {
        sessionStorage.setItem(KEY, legacy);
        localStorage.removeItem(KEY);
      } catch {
        // ignore migration failure
      }
      return legacy;
    }
    return null;
  } catch {
    return null;
  }
}

export function getLowCodeSession(): LowCodeSession | null {
  try {
    const raw = readStoredRaw();
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
      sessionStorage.removeItem(KEY);
      localStorage.removeItem(KEY);
      return;
    }
    const payload = JSON.stringify(session);
    // Low-code dev UI: browser-held OAuth tokens (same threat model as public SPA + angular-oauth2-oidc).
    // Prefer sessionStorage (tab-scoped). Production-hardened apps should use a BFF with httpOnly cookies.
    // codeql[js/clear-text-storage-of-sensitive-data]: internal dev session only; no server-side secret; BFF is the long-term pattern.
    sessionStorage.setItem(KEY, payload);
    try {
      localStorage.removeItem(KEY);
    } catch {
      // ignore
    }
  } catch {
    // ignore
  }
}
