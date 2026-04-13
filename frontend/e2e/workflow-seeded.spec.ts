import { test, expect } from '@playwright/test';

/**
 * Uses backend dev-token + POST /api/workflows, then injects SPA session so the browser
 * loads workflow details with a real row. API calls use `localhost` (not 127.0.0.1) so
 * Development tenant resolution matches the default tenant; see TenantResolutionMiddleware.
 */
const API_BASE = process.env.E2E_API_BASE ?? 'http://localhost:5002';

const SESSION_KEY = 'lcp.lowcode.session.v1';

const MINIMAL_DEF = '{"steps":[{"type":"noop"}]}';

test.describe('Workflow details (seeded)', () => {
  test('shows name for workflow created via API', async ({ page, request }) => {
    const wfName = `e2e-seeded-${Date.now()}`;

    const tokenResp = await request.post(`${API_BASE}/api/auth/dev-token`, {
      headers: { 'Content-Type': 'application/json' },
      data: { subject: 'e2e-workflow-seeded', tenantSlug: 'default', roles: [] },
    });
    expect(tokenResp.ok(), await tokenResp.text()).toBeTruthy();
    const tokenJson = (await tokenResp.json()) as { accessToken: string };
    expect(tokenJson.accessToken).toBeTruthy();

    const createResp = await request.post(`${API_BASE}/api/workflows`, {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${tokenJson.accessToken}`,
      },
      data: { name: wfName, definitionJson: MINIMAL_DEF },
    });
    expect(createResp.ok(), await createResp.text()).toBeTruthy();
    const created = (await createResp.json()) as { workflowDefinitionId: string };
    const id = created.workflowDefinitionId;
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );

    const sessionPayload = JSON.stringify({
      tenantSlug: 'default',
      accessToken: tokenJson.accessToken,
    });

    await page.addInitScript(
      ({ key, payload }: { key: string; payload: string }) => {
        sessionStorage.setItem(key, payload);
      },
      { key: SESSION_KEY, payload: sessionPayload },
    );

    const getWorkflow = page.waitForResponse(
      (r) =>
        r.request().method() === 'GET' &&
        r.url().includes(`/api/workflows/${id}`) &&
        !r.url().includes('/runs'),
    );

    await page.goto(`/lowcode/workflows/${id}`);

    const detailsResp = await getWorkflow;
    expect(detailsResp.status(), await detailsResp.text()).toBe(200);

    await expect(page.getByRole('heading', { name: 'Workflow' })).toBeVisible();
    await expect(page.getByRole('main')).toContainText(wfName, { timeout: 15_000 });
    await expect(page.getByRole('link', { name: '← Workflows' })).toBeVisible();
  });
});
