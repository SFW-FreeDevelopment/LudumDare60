// @ts-check
const { test, expect } = require('@playwright/test');
const { bootGame, watchForErrors, act, clickDesign } = require('./support');

/**
 * I7's flow gate: Playwright drives title → levels → pause → settings → credits (PLAN's "done when",
 * behaviour side — the picture side lives in screenshots.spec.js). Screens are requested through the
 * harness because that is the head-agnostic door; where a real device is the point (pointer presses,
 * key presses) the test uses the real device.
 */
const screenOf = (page) => page.evaluate(() => window.__GAME.screen());

test('the menu flow walks title, levels, pause, settings and credits @smoke', async ({ page }) => {
  const errors = watchForErrors(page);
  await bootGame(page);
  expect(await screenOf(page)).toBe('menu');

  // Title → choose course.
  expect(await act(page, 'screen=levels')).toBe('ok:requested=levels');
  await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('levels');

  // A course picked from level select is the course that flies.
  expect(await act(page, 'level=winding-trail')).toBe('ok:level=winding-trail');
  await act(page, 'screen=world');
  await expect.poll(() => screenOf(page)).toBe('world');
  expect(await act(page, 'level')).toBe('winding-trail');

  // Pause holds the flight: while the panel is up, the clock must be frozen.
  await act(page, 'screen=pause');
  await expect.poll(() => screenOf(page)).toBe('pause');
  const frozen = Number(await act(page, 'tick'));
  await page.waitForTimeout(400);
  expect(Number(await act(page, 'tick'))).toBe(frozen);

  // Resuming lets time go again.
  await act(page, 'screen=world');
  await expect.poll(() => screenOf(page)).toBe('world');
  await page.waitForTimeout(400);
  expect(Number(await act(page, 'tick'))).toBeGreaterThan(frozen);

  // Settings: read the mirrored line, change assist, and the flight in the air feels it at once.
  await act(page, 'screen=settings');
  await expect.poll(() => screenOf(page)).toBe('settings');
  expect(await act(page, 'settings')).toBe('music=8;sfx=8;assist=0;motion=0');
  expect(await act(page, 'settings=assist=1')).toBe('ok:music=8;sfx=8;assist=1;motion=0');
  expect((await page.evaluate(() => window.__GAME.state())).windAssist).toBe(true);
  expect((await page.evaluate(() => window.__GAME.state())).settings).toBe('music=8;sfx=8;assist=1;motion=0');
  expect(await act(page, 'settings=assist=0')).toBe('ok:music=8;sfx=8;assist=0;motion=0');

  // Credits: reachable, and walks back out to the title.
  await act(page, 'screen=credits');
  await expect.poll(() => screenOf(page)).toBe('credits');
  await act(page, 'screen=menu');
  await expect.poll(() => screenOf(page)).toBe('menu');

  expect(errors, `console/JS errors during the flow: ${errors.join(' | ')}`).toEqual([]);
});

test('the title yields to a real pointer press on SETTINGS and walks back', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  // SETTINGS slat: design rect x 220..420, y 164..180 → press its middle.
  await clickDesign(page, 320, 172);
  await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('settings');

  // The BACK TO TITLE slat (last row: y = 92 + 5*26 = 222..238).
  await clickDesign(page, 320, 230);
  await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('menu');
});

test('a real key press walks the title menu', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  // Focus starts on FLY; one Down lands on CHOOSE COURSE; Enter confirms — the keyboard-only
  // promise (AGENTS.md) asserted through the WASM head's own keyboard pipeline.
  await page.locator('canvas').click({ position: { x: 2, y: 2 }, delay: 40 }); // give the canvas focus
  await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('menu');  // a stray click must not fly

  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('Enter');
  await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('levels');

  await page.keyboard.press('Escape');
  await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('menu');
});

test('the voyage flies all three courses start to finish, charting each one home @smoke', async ({ page }) => {
  // PLAN I10's "done when", end to end in WebGL: deliver a course, press the R the panel just
  // advertised, and the world map opens under the balloon; Enter flies the next isle. After the
  // last course the chart wraps — the voyage never dead-ends.
  const errors = watchForErrors(page);
  await bootGame(page);

  await act(page, 'reset=606');
  await act(page, 'screen=world');
  await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('world');

  await page.locator('canvas').click({ position: { x: 2, y: 2 }, delay: 30 }); // keyboard needs the canvas focused; a stray click here is only one puff of heat

  for (const id of ['nursery-skies', 'winding-trail', 'night-crossing']) {
    expect(await act(page, 'level')).toBe(id);
    expect(await act(page, 'deliver')).toBe('ok:delivered');
    expect(await act(page, 'delivered')).toBe('1');

    await act(page, 'resume');                     // the R handler is live only on a live world
    await page.keyboard.press('KeyR');             // the panel's own key, through the browser pipeline
    await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('worldmap');
    await page.keyboard.press('Enter');            // FLY ON
    await expect.poll(() => screenOf(page), { timeout: 10_000 }).toBe('world');
  }

  // Wrapping the chart: home from the last course, the first course waits again.
  expect(await act(page, 'level')).toBe('nursery-skies');
  expect(await act(page, 'hearts')).toBe('3');     // fresh sky, fresh hearts — a wrap is a launch, not a carry-over
  expect(errors, `console/JS errors during the voyage: ${errors.join(' | ')}`).toEqual([]);
});
