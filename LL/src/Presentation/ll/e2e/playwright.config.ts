import { defineConfig, devices } from '@playwright/test';

/**
 * The Grimoire snapshot and accessibility run (ANGULAR_DESIGN_SYSTEM_PLAN.md, step 5): `npm run grimoire:snapshots`.
 *
 * It starts the dev server on port 4300 (or uses one already running there), opens every story of the /grimoire
 * showcase at 1600×900 and compares it with the baseline in grimoire/__snapshots__/<platform>/. Baselines are per
 * platform because each system draws text a little differently: the first run on a new platform writes its set.
 * `npm run grimoire:snapshots:update` rewrites the baselines after an intended change.
 */
const port = Number(process.env['GRIMOIRE_PORT'] ?? 4300);
const baseURL = process.env['GRIMOIRE_BASE_URL'] ?? `http://localhost:${port}`;

export default defineConfig({
  testDir: './grimoire',
  outputDir: '../test-results/grimoire',
  snapshotPathTemplate: '{testDir}/__snapshots__/{platform}/{arg}{ext}',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 15 * 60 * 1000,
  updateSnapshots: 'missing',
  reporter: [
    ['list'],
    [
      'html',
      { outputFolder: '../test-results/grimoire-report', open: 'never' },
    ],
  ],
  expect: {
    toHaveScreenshot: {
      animations: 'disabled',
      caret: 'hide',
      scale: 'css',
      maxDiffPixels: 0,
    },
  },
  use: {
    baseURL,
    viewport: { width: 1600, height: 900 },
    deviceScaleFactor: 1,
    colorScheme: 'dark',
    locale: 'en-GB',
    timezoneId: 'Europe/Copenhagen',
    reducedMotion: 'reduce',
    trace: 'retain-on-failure',
    launchOptions: process.env['GRIMOIRE_CHROMIUM']
      ? { executablePath: process.env['GRIMOIRE_CHROMIUM'] }
      : {},
  },
  projects: [
    {
      name: 'chromium',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1600, height: 900 },
      },
    },
  ],
  webServer: process.env['GRIMOIRE_BASE_URL']
    ? undefined
    : {
        command: `npx ng serve --port ${port} --live-reload false --hmr false`,
        url: `${baseURL}/grimoire`,
        reuseExistingServer: true,
        timeout: 5 * 60 * 1000,
        stdout: 'ignore',
        stderr: 'pipe',
      },
});
