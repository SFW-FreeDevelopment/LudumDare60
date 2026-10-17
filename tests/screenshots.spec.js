// @ts-check
const { test, expect } = require('@playwright/test');
const fs = require('fs');
const path = require('path');
const { bootGame, watchForErrors, SHOTS_DIR } = require('./support');

/**
 * Visual evidence for the I0 harness. The harness is reset to a fixed seed and stepped to a fixed
 * tick before capture, and the screen draws from state only, so these images are reproducible: a
 * change here is a real rendering change, not WASM jitter. Committed snapshots are the regression
 * net; the copies in docs/screenshots are the ones a human is expected to look at and judge
 * (AGENTS.md: green tests over ugly output is not done).
 */
async function poseForScreenshot(page) {
  await page.evaluate(async () => {
    await window.__GAME.reset('1234');
    await window.__GAME.screen('scaffold');
    await window.__GAME.set('heat', 1);
    await window.__GAME.step(240);
    await window.__GAME.set('heat', 0);
  });
  // Live play boots on the world screen, so a scaffold pose has to ask for the scaffold — and wait for
  // the game to confirm it is drawing it before anything is captured.
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('scaffold');
}

test('scaffold frame matches its snapshot @shot @screens', async ({ page }, testInfo) => {
  const errors = watchForErrors(page);
  await bootGame(page);
  await poseForScreenshot(page);

  const name = `00-scaffold`;
  await expect(page).toHaveScreenshot(`${name}.png`, { timeout: 60_000 });

  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  const buffer = await page.screenshot({ path: path.join(SHOTS_DIR, `${name}-${testInfo.project.name}.png`) });
  expect(buffer.length).toBeGreaterThan(1000);
  expect(errors, `console/JS errors while rendering: ${errors.join(' | ')}`).toEqual([]);
});

test('style sheet matches its snapshot @shot @screens', async ({ page }, testInfo) => {
  const errors = watchForErrors(page);
  await bootGame(page);

  // The style sheet is the palette and the kit in one picture: if this drifts, a visual decision changed,
  // and the reviewer is supposed to see that happen rather than discover it in the game.
  await page.evaluate(async () => {
    await window.__GAME.reset('1234');
    await window.__GAME.screen('style');
    await window.__GAME.step(12);
  });
  // Capture only once the game itself says it is drawing the style sheet, so the image is of this screen
  // and of tick 12, whatever the WASM's scheduling happened to do.
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('style');

  const name = `01-style`;
  await expect(page).toHaveScreenshot(`${name}.png`, { timeout: 60_000 });

  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  const buffer = await page.screenshot({ path: path.join(SHOTS_DIR, `${name}-${testInfo.project.name}.png`) });
  expect(buffer.length).toBeGreaterThan(1000);
  expect(errors, `console/JS errors while rendering: ${errors.join(' | ')}`).toEqual([]);
});

test('world screen renders the scrolling world, and drawing it never writes state @shot @screens', async ({ page }, testInfo) => {
  watchForErrors(page);
  await bootGame(page);

  // Settle onto the world screen first: the switch is applied by the game's own frame, and the state
  // hash includes the screen, so hashing before it lands would race with the switch itself.
  await page.evaluate(() => window.__GAME.reset('1234'));
  await page.evaluate(() => window.__GAME.screen('world'));
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('world');

  const hashBefore = await page.evaluate(async () => {
    await window.__GAME.set('heat', 1);
    await window.__GAME.step(240);    // burner lit: rise through the carrying band...
    await window.__GAME.set('heat', 0);
    await window.__GAME.step(300);    // ...then coast downwind, far enough for the delivery sign to show
    return await window.__GAME.hash();
  });

  // Real frames of the world run while paused — if drawing wrote state, the hash would move here.
  await page.waitForTimeout(300);
  expect(await page.evaluate(() => window.__GAME.hash())).toBe(hashBefore);

  const name = `02-world`;
  await expect(page).toHaveScreenshot(`${name}.png`, { timeout: 60_000 });

  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  const buffer = await page.screenshot({ path: path.join(SHOTS_DIR, `${name}-${testInfo.project.name}.png`) });
  expect(buffer.length).toBeGreaterThan(1000);
});

test('heat visibly drives the balloon and the meter @shot @screens', async ({ page }, testInfo) => {
  watchForErrors(page);
  await bootGame(page);

  const frames = {};
  for (const ticks of [0, 60, 150]) {
    await page.evaluate(async (n) => {
      await window.__GAME.reset('1234');
      await window.__GAME.screen('scaffold');
      await window.__GAME.set('heat', 1);
      await window.__GAME.step(n);
    }, ticks);
    await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('scaffold');
    frames[ticks] = await page.screenshot({
      path: path.join(SHOTS_DIR, `00-scaffold-heat-${String(ticks).padStart(3, '0')}-${testInfo.project.name}.png`),
    });
  }

  // Same seed, more heat => different picture. If these byte-equal, the injected input is not
  // reaching the renderer and the harness is lying to every later test.
  expect(frames[0].equals(frames[150])).toBe(false);
});
