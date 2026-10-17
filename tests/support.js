// Shared helpers for driving the WebGL build. Everything here is about waiting on real signals
// (first drawn frame, live frame counter) instead of sleeping, because WASM boot time is
// machine-dependent and a fixed sleep would make the suite flaky on someone else's laptop.
const path = require('path');

const SHOTS_DIR = path.join(__dirname, '..', 'docs', 'screenshots');

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

module.exports = { bootGame, watchForErrors, runScript, act, megabytes, SHOTS_DIR };
