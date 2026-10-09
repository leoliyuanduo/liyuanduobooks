import { defineConfig, devices } from '@playwright/test';

/**
 * End-to-end tests run against the REAL app image (BFF + React build), started with
 * the stubbed upstream:  docker compose -f compose.yaml -f compose.e2e.yaml up --build -d
 * Then:  npm run e2e   (or E2E_BASE_URL=https://… npm run e2e against a deployed environment)
 */
const chromiumPath = process.env['PW_CHROMIUM_PATH'];

export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.e2e.ts',
  fullyParallel: true,
  forbidOnly: Boolean(process.env['CI']),
  retries: process.env['CI'] ? 1 : 0,
  reporter: process.env['CI'] ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://localhost:8080',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    ...(chromiumPath ? { launchOptions: { executablePath: chromiumPath } } : {}),
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 800 } } },
    { name: 'mobile', use: { ...devices['Pixel 7'] } },
  ],
});
