import { test, expect } from '@playwright/test';
import { API_BASE, injectLowCodeSession, mintDevToken } from './seeded-helpers';

const MINIMAL_DEF = '{"steps":[{"type":"noop"}]}';

test.describe('Workflow details (seeded)', () => {
  test('shows name for workflow created via API', async ({ page, request }) => {
    const wfName = `e2e-seeded-wf-${Date.now()}`;

    const accessToken = await mintDevToken(request);

    const createResp = await request.post(`${API_BASE}/api/workflows`, {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      data: { name: wfName, definitionJson: MINIMAL_DEF },
    });
    expect(createResp.ok(), await createResp.text()).toBeTruthy();
    const created = (await createResp.json()) as { workflowDefinitionId: string };
    const id = created.workflowDefinitionId;
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );

    await injectLowCodeSession(page, accessToken);

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
