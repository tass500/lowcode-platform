import { test, expect } from '@playwright/test';
import { API_BASE, injectLowCodeSession, mintDevToken } from './seeded-helpers';

const MINIMAL_DEF = '{"steps":[{"type":"noop"}]}';

test.describe('Workflow run details (seeded)', () => {
  test('shows run id for workflow run started via API', async ({ page, request }) => {
    const accessToken = await mintDevToken(request);

    const wfName = `e2e-seeded-run-wf-${Date.now()}`;
    const createWf = await request.post(`${API_BASE}/api/workflows`, {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      data: { name: wfName, definitionJson: MINIMAL_DEF },
    });
    expect(createWf.ok(), await createWf.text()).toBeTruthy();
    const wf = (await createWf.json()) as { workflowDefinitionId: string };
    const wfId = wf.workflowDefinitionId;

    const startRun = await request.post(`${API_BASE}/api/workflows/${wfId}/runs`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    expect(startRun.ok(), await startRun.text()).toBeTruthy();
    const started = (await startRun.json()) as { workflowRunId: string };
    const runId = started.workflowRunId;
    expect(runId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );

    await injectLowCodeSession(page, accessToken);

    const getRun = page.waitForResponse(
      (r) =>
        r.request().method() === 'GET' &&
        r.url().includes(`/api/workflows/runs/${runId}`),
    );

    await page.goto(`/lowcode/runs/${runId}`);

    const detailsResp = await getRun;
    expect(detailsResp.status(), await detailsResp.text()).toBe(200);

    await expect(page.getByRole('heading', { name: 'Workflow run' })).toBeVisible();
    await expect(page.getByRole('main')).toContainText(runId, { timeout: 15_000 });
    await expect(page.getByRole('link', { name: '← Workflows' })).toBeVisible();
  });
});
