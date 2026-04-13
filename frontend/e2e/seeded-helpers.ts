import type { APIRequestContext, Page } from '@playwright/test';

/**
 * Dev API base: use `localhost` (not `127.0.0.1`) so tenant resolution matches the
 * default tenant when calling Kestrel directly; see TenantResolutionMiddleware.
 */
export const API_BASE = process.env.E2E_API_BASE ?? 'http://localhost:5002';

export const SESSION_KEY = 'lcp.lowcode.session.v1';

export async function mintDevToken(request: APIRequestContext): Promise<string> {
  const tokenResp = await request.post(`${API_BASE}/api/auth/dev-token`, {
    headers: { 'Content-Type': 'application/json' },
    data: { subject: 'e2e-seeded', tenantSlug: 'default', roles: [] },
  });
  if (!tokenResp.ok()) {
    throw new Error(`dev-token failed: ${tokenResp.status()} ${await tokenResp.text()}`);
  }
  const tokenJson = (await tokenResp.json()) as { accessToken: string };
  if (!tokenJson.accessToken) {
    throw new Error('dev-token response missing accessToken');
  }
  return tokenJson.accessToken;
}

export async function injectLowCodeSession(page: Page, accessToken: string): Promise<void> {
  const sessionPayload = JSON.stringify({
    tenantSlug: 'default',
    accessToken,
  });
  await page.addInitScript(
    ({ key, payload }: { key: string; payload: string }) => {
      sessionStorage.setItem(key, payload);
    },
    { key: SESSION_KEY, payload: sessionPayload },
  );
}
