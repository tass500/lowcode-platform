export type LowCodeSession = {
  /** May be empty after OIDC if the id token had no configured tenant claim; set manually on the auth page. */
  tenantSlug: string;
  accessToken: string;
  refreshToken?: string;
  oidcTokenEndpoint?: string;
  oidcClientId?: string;
};

const KEY = 'lcp.lowcode.session.v1';

/**
 * Single write path for serialized session (OAuth access/refresh tokens, public client id).
 * Tab-scoped sessionStorage; same XSS threat model as typical SPA OIDC (angular-oauth2-oidc, etc.).
 */
function persistSessionPayload(payload: string): void {
  try {
    // Browser session (OAuth access/refresh); dev SPA pattern. See docs/live/oidc-jwt-bearer.md; hardened prod → BFF + httpOnly.
    sessionStorage.setItem(KEY, payload);
  } catch {
    // ignore
  }
}

/** Tab-scoped storage limits exposure vs localStorage; OIDC libs typically use the same. */
function readStoredRaw(): string | null {
  try {
    const fromSession = sessionStorage.getItem(KEY);
    if (fromSession != null) return fromSession;
    const legacy = localStorage.getItem(KEY);
    if (legacy != null) {
      persistSessionPayload(legacy);
      try {
        localStorage.removeItem(KEY);
      } catch {
        // ignore
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
    persistSessionPayload(JSON.stringify(session));
    try {
      localStorage.removeItem(KEY);
    } catch {
      // ignore
    }
  } catch {
    // ignore
  }
}
