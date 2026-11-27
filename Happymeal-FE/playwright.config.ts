import { defineConfig, devices } from '@playwright/test';

const port = Number(process.env['HAPPYMEAL_E2E_PORT'] ?? 4300);
if (!Number.isInteger(port) || port < 1024 || port > 65535) {
  throw new Error('HAPPYMEAL_E2E_PORT must be an integer between 1024 and 65535.');
}
const baseURL = `http://localhost:${port}`;

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  workers: 2,
  reporter: 'list',
  use: { baseURL, trace: 'retain-on-failure' },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'] } },
    {
      name: 'mobile',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 390, height: 844 },
        isMobile: true,
        hasTouch: true,
      },
    },
  ],
  webServer: {
    command: `npm start -- --host localhost --port ${port}`,
    url: baseURL,
    reuseExistingServer: false,
    timeout: 60_000,
  },
});
