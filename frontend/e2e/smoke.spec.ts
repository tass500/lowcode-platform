import { test, expect } from '@playwright/test';

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
});
