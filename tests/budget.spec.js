// @ts-check
const { test, expect } = require('@playwright/test');
const { bootGame, watchForErrors, act, megabytes } = require('./support');

/**
 * I12's budget pass, measured the way the AGENTS gate asks for it: boot-to-title logged, payload
 * logged against its ceiling, and average frame time during live play asserted under 20 ms while a
 * Chromium tab draws the whole world. The rAF sampler rides the same frame cadence the game does,
 * so a long task in the draw path shows up here as a long sample.
 */

test('boot-to-title and payload are logged and inside budget @smoke', async ({ page }) => {
  const errors = watchForErrors(page);
  const boot = await bootGame(page);
  console.log(
    `BOOT-TO-TITLE ${boot.readyAtMs} ms · PAYLOAD ${megabytes(boot.downloadedBytes)} MB over ${boot.requests} requests`
  );
  // Boot timing is recorded; readiness has a generous timeout, never a wall-clock speed assertion.
  expect(boot.downloadedBytes).toBeLessThan(8 * 1024 * 1024);
  expect(errors, `console/JS errors during boot: ${errors.join(' | ')}`).toEqual([]);
});

test('live play holds 60 fps-class frames in the tab @smoke', async ({ page }) => {
  const errors = watchForErrors(page);
  await bootGame(page);
  await act(page, 'reset=707');
  await act(page, 'screen=world');
  await page.evaluate(async () => {
    await window.__GAME.set('heat', 1);   // burner frames, puffs and gauge all drawing
    await window.__GAME.resume();         // live loop: every rAF is a full world frame
  });

  const stats = await page.evaluate(
    () =>
      new Promise((resolve) => {
        /** @type {number[]} */
        const samples = [];
        let prev = performance.now();
        const tick = (t) => {
          samples.push(t - prev);
          prev = t;
          if (samples.length >= 120) {
            const clean = samples.slice(1).sort((a, b) => a - b);   // first delta spans the setup gap
            resolve({
              avg: clean.reduce((a, b) => a + b, 0) / clean.length,
              p95: clean[Math.floor(clean.length * 0.95)],
              worst: clean[clean.length - 1],
            });
          } else {
            requestAnimationFrame(tick);
          }
        };
        requestAnimationFrame(tick);
      })
  );
  console.log(
    `FRAME-TIME avg ${stats.avg.toFixed(2)} ms · p95 ${stats.p95.toFixed(2)} ms · worst ${stats.worst.toFixed(2)} ms (120 live rAF ticks)`
  );
  expect(stats.avg).toBeLessThan(20);
  expect(errors, `console/JS errors during the frame sample: ${errors.join(' | ')}`).toEqual([]);
});
