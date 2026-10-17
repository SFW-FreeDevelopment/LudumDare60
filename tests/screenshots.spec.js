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
  // Live play boots on the start screen, so a scaffold pose has to ask for the scaffold — and wait for
  // the game to confirm it is drawing it before anything is captured.
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('scaffold');
}

test('the start screen shows the controls @shot @screens', async ({ page }, testInfo) => {
  const errors = watchForErrors(page);
  await bootGame(page);

  // The boot frame is the menu and the menu holds the clock, so this is a byte-stable capture
  // with no reset, seed or step at all — exactly what the player sees first.
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('menu');

  const name = `04-menu`;
  await expect(page).toHaveScreenshot(`${name}.png`, { timeout: 60_000 });

  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  const buffer = await page.screenshot({ path: path.join(SHOTS_DIR, `${name}-${testInfo.project.name}.png`) });
  expect(buffer.length).toBeGreaterThan(1000);
  expect(errors, `console/JS errors while rendering: ${errors.join(' | ')}`).toEqual([]);
});

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

test('a delivered run shows its banner — the win screen at a glance @shot @screens', async ({ page }, testInfo) => {
  watchForErrors(page);
  await bootGame(page);

  // The delivery pose: the autopilot flies the whole run inside the bridge (it steps directly, so
  // being paused does not stop it), and the world screen then draws the finished state — banner,
  // stars and all — from that state alone.
  await page.evaluate(() => window.__GAME.reset('beef'));
  await page.evaluate(() => window.__GAME.screen('world'));
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('world');
  expect(await page.evaluate(() => window.__GAME.deliver(120))).toBe('ok:delivered');

  // Let a few real frames draw the finished world before capturing it.
  await page.waitForTimeout(300);

  const name = `03-delivery`;
  await expect(page).toHaveScreenshot(`${name}.png`, { timeout: 60_000 });

  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  const buffer = await page.screenshot({ path: path.join(SHOTS_DIR, `${name}-${testInfo.project.name}.png`) });
  expect(buffer.length).toBeGreaterThan(1000);
});

async function poseUiScreen(page, id) {
  await page.evaluate((screen) => window.__GAME.screen(screen), id);
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe(id);
}

/** Captures one UI screen by id under both names the review copies use. */
async function shootUi(page, testInfo, name, id) {
  const errors = watchForErrors(page);
  await bootGame(page);
  await poseUiScreen(page, id);

  await expect(page).toHaveScreenshot(`${name}.png`, { timeout: 60_000 });
  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  const buffer = await page.screenshot({ path: path.join(SHOTS_DIR, `${name}-${testInfo.project.name}.png`) });
  expect(buffer.length).toBeGreaterThan(1000);
  expect(errors, `console/JS errors on ${id}: ${errors.join(' | ')}`).toEqual([]);
}

test('the level select screen matches its snapshot @shot @screens', async ({ page }, testInfo) => {
  await shootUi(page, testInfo, '05-levels', 'levels');
});

test('the settings screen matches its snapshot @shot @screens', async ({ page }, testInfo) => {
  await shootUi(page, testInfo, '06-settings', 'settings');
});

test('the credits screen matches its snapshot @shot @screens', async ({ page }, testInfo) => {
  await shootUi(page, testInfo, '07-credits', 'credits');
});

test('the pause screen matches its snapshot @shot @screens', async ({ page }, testInfo) => {
  const errors = watchForErrors(page);
  await bootGame(page);

  // The pause pose: a real flight, held. The balloon must be somewhere mid-course under the veil so
  // the screen proves the world keeps its pose while paused — not just a panel over launch.
  await page.evaluate(() => window.__GAME.reset('1234'));
  await page.evaluate(() => window.__GAME.screen('world'));
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('world');
  await page.evaluate(async () => {
    await window.__GAME.set('heat', 1);
    await window.__GAME.step(240);                 // airborne over the carrying band...
    await window.__GAME.set('heat', 0);
  });
  await poseUiScreen(page, 'pause');

  const name = '08-pause';
  await expect(page).toHaveScreenshot(`${name}.png`, { timeout: 60_000 });
  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  const buffer = await page.screenshot({ path: path.join(SHOTS_DIR, `${name}-${testInfo.project.name}.png`) });
  expect(buffer.length).toBeGreaterThan(1000);
  expect(errors, `console/JS errors while paused: ${errors.join(' | ')}`).toEqual([]);
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
