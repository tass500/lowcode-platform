import { test, expect } from '@playwright/test';

test.describe('Low-code smoke', () => {
  test('workflows page renders shell', async ({ page }) => {
    await page.goto('/lowcode/workflows');
    await expect(page.getByRole('heading', { name: 'Workflows' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'New' })).toBeVisible();
  });
});
