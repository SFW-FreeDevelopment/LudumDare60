// @ts-check
const { defineConfig, devices } = require('@playwright/test');

// One WebGL boot per test file is expensive (WASM download + compile), so tests are grouped rather
// than spread: a single spec file per concern, and workers stay at 1 because four simultaneous
// Chromium WASM boots on one machine make every timing measurement meaningless.
const port = Number(process.env.PORT || 4173);

module.exports = defineConfig({
  testDir: './tests',
  timeout: 240_000,
  expect: {
    timeout: 180_000,
    toHaveScreenshot: { maxDiffPixelRatio: 0.01 },
  },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'playwright-report' }]],
  outputDir: 'test-results',
  // Snapshot names carry the project (viewport) but not the OS: the pixel budgets here are our own
  // regression net, reviewed on every initiative, not cross-platform golden files.
  snapshotPathTemplate: '{testDir}/{testFileDir}/{testFileName}-snapshots/{arg}{-projectName}{ext}',
  use: {
    baseURL: `http://localhost:${port}`,
    headless: true,
    trace: 'off',
    video: 'off',
    screenshot: 'off',
  },
  projects: [
    {
      name: 'desktop',
      use: { viewport: { width: 1280, height: 720 } },
    },
    {
      name: 'narrow',
      use: { viewport: { width: 420, height: 760 } },
    },
  ],
  webServer: {
    command: `node tools/serve.mjs`,
    url: `http://localhost:${port}/index.html`,
    reuseExistingServer: true,
    timeout: 60_000,
    stdout: 'ignore',
  },
});
