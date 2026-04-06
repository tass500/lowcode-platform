import { test, expect } from '@playwright/test';

/** Valid UUID unlikely to exist in a fresh CI DB — details pages render shell + API error. */
const MISSING_ID = '00000000-0000-0000-0000-000000000001';

test.describe('Low-code smoke', () => {
  test('root redirects to workflows', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/lowcode\/workflows$/);
    await expect(page.getByRole('heading', { name: 'Workflows' })).toBeVisible();
  });

  test('workflows page renders shell', async ({ page }) => {
    await page.goto('/lowcode/workflows');
    await expect(page.getByRole('heading', { name: 'Workflows' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'New' })).toBeVisible();
  });

  test('entities page renders shell', async ({ page }) => {
    await page.goto('/lowcode/entities');
    await expect(page.getByRole('heading', { name: 'Entities' })).toBeVisible();
    // Toolbar link (avoid strict-mode clash with shell nav + empty-state links named "Workflows").
    await expect(page.getByRole('main').getByRole('link', { name: 'All runs' })).toBeVisible();
  });

  test('workflow runs page renders shell', async ({ page }) => {
    await page.goto('/lowcode/workflow-runs');
    await expect(page.getByRole('heading', { name: 'Workflow runs (tenant)' })).toBeVisible();
    await expect(page.getByRole('main').getByRole('link', { name: 'Entities' })).toBeVisible();
  });

  /** Dev auth shell (no IdP round-trip — no OIDC login flow). */
  test('auth page renders shell', async ({ page }) => {
    await page.goto('/lowcode/auth');
    await expect(page.getByRole('heading', { name: 'Low-code Auth (dev)' })).toBeVisible();
  });

  /** OAuth redirect target; template always exposes back link (loading vs error is async / env-dependent). */
  test('auth callback page renders shell', async ({ page }) => {
    await page.goto('/lowcode/auth/callback');
    await expect(page.getByRole('link', { name: 'Vissza az auth oldalra' })).toBeVisible();
  });

  test('new workflow page renders shell', async ({ page }) => {
    await page.goto('/lowcode/workflows/new');
    await expect(page.getByRole('heading', { name: 'New workflow' })).toBeVisible();
  });

  test('new entity page renders shell', async ({ page }) => {
    await page.goto('/lowcode/entities/new');
    await expect(page.getByRole('heading', { name: 'New entity' })).toBeVisible();
  });

  test('admin tenants page renders shell', async ({ page }) => {
    await page.goto('/lowcode/admin/tenants');
    await expect(page.getByRole('heading', { name: 'Admin / Tenants' })).toBeVisible();
  });

  test('upgrade page renders shell', async ({ page }) => {
    await page.goto('/upgrade');
    // Substring match would also hit "Start upgrade run" / "Upgrade run details" (h3) — use exact.
    await expect(page.getByRole('heading', { name: 'Upgrade', exact: true })).toBeVisible();
  });

  test('workflow details page renders shell (missing id)', async ({ page }) => {
    await page.goto(`/lowcode/workflows/${MISSING_ID}`);
    await expect(page.getByRole('heading', { name: 'Workflow' })).toBeVisible();
    await expect(page.getByRole('link', { name: '← Workflows' })).toBeVisible();
  });

  test('workflow run details page renders shell (missing id)', async ({ page }) => {
    await page.goto(`/lowcode/runs/${MISSING_ID}`);
    await expect(page.getByRole('heading', { name: 'Workflow run' })).toBeVisible();
    await expect(page.getByRole('link', { name: '← Workflows' })).toBeVisible();
  });

  test('entity details page renders shell (missing id)', async ({ page }) => {
    await page.goto(`/lowcode/entities/${MISSING_ID}`);
    await expect(page.getByRole('heading', { name: 'Entity' })).toBeVisible();
    await expect(page.getByRole('link', { name: '← Entities' })).toBeVisible();
  });

  test('entity records page renders shell (missing entity id)', async ({ page }) => {
    await page.goto(`/lowcode/entities/${MISSING_ID}/records`);
    await expect(page.getByRole('heading', { name: 'Entity Records' })).toBeVisible();
    await expect(page.getByRole('link', { name: '← Entities' })).toBeVisible();
  });
});
