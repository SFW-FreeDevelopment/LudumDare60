// Shared helpers for driving the WebGL build. Everything here is about waiting on real signals
// (first drawn frame, live frame counter) instead of sleeping, because WASM boot time is
// machine-dependent and a fixed sleep would make the suite flaky on someone else's laptop.
const path = require('path');

const SHOTS_DIR = path.join(__dirname, '..', 'docs', 'screenshots');

// Windows can briefly deny an image open while the app's preview/indexer holds it.
// Retry only file access errors; pixel differences and gameplay failures still fail immediately.
async function retryImageFile(action) {
  for (let attempt = 0; ; attempt++) {
    try { return await action(); }
    catch (error) {
      if (attempt >= 4 || !/UNKNOWN: unknown error, open|EBUSY|EPERM|EACCES/.test(String(error))) throw error;
      await new Promise(resolve => setTimeout(resolve, 100 * (attempt + 1)));
    }
  }
}

async function matchShot(page, name) {
  const {expect} = require('@playwright/test');
  await retryImageFile(() => expect(page).toHaveScreenshot(name, {timeout:60_000}));
}

async function reviewShot(page, options) {
  const frame = await page.evaluate(() => window.__GAME.frames());
  await page.waitForFunction(async start => Number(await window.__GAME.frames()) >= Number(start) + 8, frame);
  return retryImageFile(() => page.screenshot(options));
}

/** Collects console errors and uncaught exceptions; a WebGL failure shows up here first. */
function watchForErrors(page) {
  const errors = [];
  page.on('console', (message) => {
    if (message.type() === 'error') errors.push(`console: ${message.text()}`);
  });
  page.on('pageerror', (error) => errors.push(`pageerror: ${error.message}`));
  return errors;
}

/** Loads the page and resolves once the game has drawn its first frame. */
async function bootGame(page) {
  await page.goto('/index.html', { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => window.__GAME && window.__GAME.ready === true, null, { timeout: 180_000 });

  const boot = await page.evaluate(() => {
    const nav = performance.getEntriesByType('navigation')[0];
    const resources = performance.getEntriesByType('resource');
    const bytes =
      resources.reduce((total, entry) => total + (entry.transferSize || 0), 0) +
      ((nav && nav.transferSize) || 0);
    return {
      readyAtMs: window.__GAME.readyAtMs,
      bootError: window.__GAME.bootError,
      downloadedBytes: bytes,
      requests: resources.length + 1,
    };
  });
  return boot;
}

/** Bytes actually transferred for a boot, in MB, as it would be reported to a player. */
function megabytes(bytes) {
  return (bytes / (1024 * 1024)).toFixed(2);
}

/** Runs a script of harness calls and returns the final hash. Deterministic by construction. */
async function runScript(page, seed, script) {
  await page.evaluate((s) => window.__GAME.reset(s), seed);
  for (const [call, arg] of script) {
    await page.evaluate(([c, a]) => window.__GAME[c](a), [call, arg]);
  }
  return await page.evaluate(() => window.__GAME.hash());
}

/**
 * Makes one harness call written as `name` or `name=argument`, which keeps the specs readable:
 * `act(page, 'screen=style')` reads like the protocol it drives.
 */
async function act(page, call) {
  return await page.evaluate(async (spec) => {
    const index = spec.indexOf('=');
    return index < 0
      ? await window.__GAME[spec]()
      : await window.__GAME[spec.slice(0, index)](spec.slice(index + 1));
  }, call);
}

/**
 * Clicks one design pixel of the canvas. Menu hit-testing is written in design coordinates and
 * Playwright clicks in page coordinates; the letterbox math lives in DesignCanvas, so this just
 * replays the scale the renderer itself reported. The hold is a real human-scale press: a
 * zero-duration synthetic click can fall between two 60 Hz updates and never be seen.
 */
async function clickDesign(page, designX, designY, { delay = 120 } = {}) {
  const geo = await page.evaluate(async () => {
    const canvas = document.getElementById('theCanvas');
    const values = Object.fromEntries((await window.__GAME.metrics()).split(';').map((p) => p.split('=')));
    const rect = canvas.getBoundingClientRect();
    return { w: canvas.width, h: canvas.height, scale: parseFloat(values.scale), left: rect.left, top: rect.top };
  });
  const ox = (geo.w - 640 * geo.scale) / 2;
  const oy = (geo.h - 360 * geo.scale) / 2;
  await page.mouse.click(geo.left + ox + designX * geo.scale, geo.top + oy + designY * geo.scale, { delay });
}

module.exports = { bootGame, watchForErrors, runScript, act, clickDesign, megabytes, SHOTS_DIR, matchShot, reviewShot };
