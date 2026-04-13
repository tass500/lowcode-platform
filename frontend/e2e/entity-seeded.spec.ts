import { test, expect } from '@playwright/test';
import { API_BASE, injectLowCodeSession, mintDevToken } from './seeded-helpers';

test.describe('Entity details (seeded)', () => {
  test('shows name for entity created via API', async ({ page, request }) => {
    const entityName = `e2e-seeded-ent-${Date.now()}`;

    const accessToken = await mintDevToken(request);

    const createResp = await request.post(`${API_BASE}/api/entities`, {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${accessToken}`,
      },
      data: { name: entityName },
    });
    expect(createResp.ok(), await createResp.text()).toBeTruthy();
    const created = (await createResp.json()) as { entityDefinitionId: string };
    const id = created.entityDefinitionId;
    expect(id).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );

    await injectLowCodeSession(page, accessToken);

    const getEntity = page.waitForResponse(
      (r) =>
        r.request().method() === 'GET' &&
        r.url().includes(`/api/entities/${id}`) &&
        !r.url().includes('/fields') &&
        !r.url().includes('/records'),
    );

    await page.goto(`/lowcode/entities/${id}`);

    const detailsResp = await getEntity;
    expect(detailsResp.status(), await detailsResp.text()).toBe(200);

    await expect(page.getByRole('heading', { name: 'Entity' })).toBeVisible();
    // Entity name is the first "Name" textbox (Fields section has another "Name" label).
    await expect(
      page.getByRole('main').locator('form').first().getByRole('textbox', { name: 'Name' }),
    ).toHaveValue(entityName, { timeout: 15_000 });
    await expect(page.getByRole('link', { name: '← Entities' })).toBeVisible();
  });
});
