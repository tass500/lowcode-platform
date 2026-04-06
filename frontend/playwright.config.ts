import { defineConfig, devices } from '@playwright/test';
import * as path from 'path';

/**
 * Set `PW_NO_WEBSERVER=1` when backend + frontend are already running (e.g. CI script starts
 * both processes, or local dev terminals). Otherwise Playwright spawns `dotnet run` + `ng serve`.
 */
const noWebServer = !!process.env.PW_NO_WEBSERVER;

/** When unset (local dev), reuse already-running servers to save time. CI sets CI=true. */
const reuseExistingServer = !process.env.CI;

const backendDir = path.join(__dirname, '..', 'backend');

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI
    ? 'github'
    : [
        ['list'],
        ['html', { open: 'never', outputFolder: 'playwright-report' }],
      ],
  timeout: 60_000,
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    ...devices['Desktop Chrome'],
  },
  webServer: noWebServer
    ? undefined
    : [
        {
          command: 'dotnet run --launch-profile http',
          cwd: backendDir,
          url: 'http://127.0.0.1:5002/health',
          reuseExistingServer,
          timeout: 600_000,
        },
        {
          command: 'npm run start',
          url: 'http://localhost:4200',
          reuseExistingServer,
          timeout: 600_000,
        },
      ],
});
