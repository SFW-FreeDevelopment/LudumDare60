// @ts-check
const { test, expect } = require('@playwright/test');
const { bootGame, watchForErrors, runScript, act, megabytes } = require('./support');

/**
 * The WebGL head is the entry. These tests are the "never commit red" gate from AGENTS.md: they
 * assert behaviour through window.__GAME, never by reading pixels.
 */
test('boots in a browser and the harness answers @smoke', async ({ page }, testInfo) => {
  const errors = watchForErrors(page);
  const boot = await bootGame(page);

  expect(boot.bootError, `bridge never reported ready: ${boot.bootError}`).toBeFalsy();
  expect(errors, `console/JS errors during boot: ${errors.join(' | ')}`).toEqual([]);

  const protocol = await page.evaluate(async () => ({
    version: await window.__GAME.version(),
    screen: await window.__GAME.screen(),
    seed: await window.__GAME.seed(),
    hash: await window.__GAME.hash(),
    state: await window.__GAME.state(),
  }));

  expect(protocol.version).toBe('i4');
  expect(protocol.screen).toBe('world');    // live play opens on the world; reset returns to the test bed
  expect(protocol.seed).toMatch(/^[0-9a-f]{16}$/);
  expect(protocol.hash).toMatch(/^[0-9a-f]{16}$/);
  expect(protocol.state.tick).toBeGreaterThanOrEqual(0);
  expect(protocol.state.version).toBe('i4');

  // I5's landing-rule fields ride in the state snapshot, so any watcher can judge a run's end.
  expect(protocol.state.delivered).toBe(false);
  expect(protocol.state.complete).toBe(false);
  expect(protocol.state.stars).toBe(0);
  expect(protocol.state.bounces).toBe(0);

  // Reported for the record; the budget itself is an I12 concern, this is the baseline it compares to.
  console.log(
    `[boot] ${boot.readyAtMs} ms to first frame | ${megabytes(boot.downloadedBytes)} MB over ${boot.requests} requests`,
  );
  const viewport = await page.evaluate(() => `${window.innerWidth}x${window.innerHeight}`);
  require('fs').mkdirSync(require('path').join(__dirname, '..', 'test-results'), { recursive: true });
  require('fs').writeFileSync(
    require('path').join(__dirname, '..', 'test-results', `boot-${testInfo.project.name}.json`),
    JSON.stringify({ ...boot, viewport }, null, 2),
  );
  test.info().annotations.push(
    { type: 'boot', description: `${boot.readyAtMs} ms to first frame` },
    { type: 'payload', description: `${megabytes(boot.downloadedBytes)} MB over ${boot.requests} requests` },
  );
});

test('the simulation advances only from the clock it owns @smoke', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const first = await page.evaluate(() => window.__GAME.tick());
  await page.waitForTimeout(500);
  const later = await page.evaluate(() => window.__GAME.tick());
  expect(Number(later)).toBeGreaterThan(Number(first));

  // A paused harness must stop the game loop dead: scripted runs and live play cannot overlap.
  await page.evaluate(() => window.__GAME.pause());
  const pausedAt = await page.evaluate(() => window.__GAME.tick());
  await page.waitForTimeout(500);
  expect(await page.evaluate(() => window.__GAME.tick())).toBe(pausedAt);
  await page.evaluate(() => window.__GAME.resume());
});

test('identical scripts hash identically, different scripts do not @smoke', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const heatScript = [['set', 'heat'], ['step', 30], ['set', 'heat=0'], ['step', 30]];
  const coastScript = [['step', 60]];

  const heatA = await runScript(page, '1234', heatScript);
  const heatB = await runScript(page, '1234', heatScript);
  const coast = await runScript(page, '1234', coastScript);
  const otherSeed = await runScript(page, 'abcd', heatScript);

  expect(heatA).toBe(heatB);
  expect(heatA).not.toBe(coast);
  expect(heatA).not.toBe(otherSeed);
});

test('the balloon flies the same model in the browser: heat climbs, coast descends', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  // Mirrors BalloonPhysicsTests on the pure core, but through the WebGL bridge: the property that matters
  // is that the browser's WASM floats produce the same flight, not just a similar-looking one.
  const flown = await page.evaluate(async () => {
    await window.__GAME.reset('1234');
    await window.__GAME.set('heat', 1);
    await window.__GAME.step(600);
    const hot = {
      temp: parseFloat(await window.__GAME.temperature()),
      alt: parseFloat(await window.__GAME.altitude()),
    };
    await window.__GAME.set('heat', 0);
    await window.__GAME.step(30);
    const overshootVelocity = parseFloat(await window.__GAME.velocity());
    await window.__GAME.step(1200);
    const cooled = {
      alt: parseFloat(await window.__GAME.altitude()),
      temp: parseFloat(await window.__GAME.temperature()),
    };
    await window.__GAME.set('ballast', 1);
    await window.__GAME.step(1);
    return { hot, overshootVelocity, cooled, ballast: await window.__GAME.ballast() };
  });

  expect(flown.hot.temp).toBeGreaterThan(0.99);
  expect(flown.hot.alt).toBeGreaterThan(300);
  expect(flown.overshootVelocity).toBeGreaterThan(0);          // momentum carries past the closed burner
  expect(flown.cooled.alt).toBeLessThan(flown.hot.alt);        // and cold air brings it back down
  expect(flown.cooled.temp).toBeCloseTo(0.3, 1);               // toward what the air itself is
  expect(flown.ballast).toBe('1');                             // one sandbag, gone for the level
});

test('the wind bands own horizontal travel, and switching levels relaunches', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  // The same model asserted in WindLayerTests, but through WASM: heat alone buys altitude, and the
  // band that altitude buys decides the horizontal velocity. Levels are configuration, so switching
  // one re-launches the flight rather than teleporting a balloon mid-course.
  const flown = await page.evaluate(async () => {
    await window.__GAME.reset('1234');
    await window.__GAME.set('heat', 1);
    await window.__GAME.step(300);
    const drift = parseFloat(await window.__GAME.x());
    const vx = parseFloat(await window.__GAME.velocityX());
    const bad = await window.__GAME.level('nowhere-level');
    const switched = await window.__GAME.level('night-crossing');
    const afterSwitch = parseFloat(await window.__GAME.x());
    const levelNow = await window.__GAME.level();
    return { drift, vx, bad, switched, afterSwitch, levelNow };
  });

  expect(flown.drift).toBeGreaterThan(100);                 // climbing into the + band moved us downwind
  expect(flown.vx).toBeGreaterThan(50);                     // ...and velocity is chasing that band's wind
  expect(flown.bad).toBe('error:unknown-level:nowhere-level');
  expect(flown.switched).toBe('ok:level=night-crossing');
  expect(flown.afterSwitch).toBe(0);                        // switched = relaunched, not teleported
  expect(flown.levelNow).toBe('night-crossing');
});

test('the protocol rejects horizontal control and bad arguments', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const replies = await page.evaluate(async () => ({
    left: await window.__GAME.set('left', 1),
    right: await window.__GAME.set('right', 1),
    badStep: await window.__GAME.step(0),
    unknown: await window.__GAME.raw('not-a-call', ''),
  }));

  expect(replies.left).toBe('error:unknown-key:left');
  expect(replies.right).toBe('error:unknown-key:right');
  expect(replies.badStep).toBe('error:bad-arg:step');
  expect(replies.unknown).toBe('error:unknown-call:not-a-call');
});

test('the harness can put the game on a screen and reports what it actually drew', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  // The request is answered at once, but only the game's own frame loop decides what is on screen, so the
  // assertion waits for a frame rather than assuming the call already switched.
  expect(await act(page, 'screen=style')).toBe('ok:requested=style');
  await expect.poll(() => act(page, 'screen'), { timeout: 10_000 }).toBe('style');

  // An unknown screen is accepted as a request, but the game resolves it to something it can really draw.
  expect(await act(page, 'screen=nowhere')).toBe('ok:requested=nowhere');
  await expect.poll(() => act(page, 'screen'), { timeout: 10_000 }).toBe('scaffold');

  expect(await act(page, 'screen=' + 'x'.repeat(33))).toBe('error:bad-arg:screen');
});

test('the canvas owns the page and the design frame letterboxes inside it', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const geometry = await page.evaluate(async () => {
    const canvas = document.getElementById('theCanvas');
    return {
      canvasWidth: canvas.width,
      canvasHeight: canvas.height,
      innerWidth: window.innerWidth,
      innerHeight: window.innerHeight,
      metrics: await window.__GAME.metrics(),
    };
  });

  // A canvas smaller than the page shows up as a little box inside the LD iframe, which is exactly
  // the bug this test exists for: the head once drew into a 640x360 corner of a 1280x720 page.
  expect(geometry.canvasWidth).toBe(geometry.innerWidth);
  expect(geometry.canvasHeight).toBe(geometry.innerHeight);

  const values = Object.fromEntries(geometry.metrics.split(';').map((pair) => pair.split('=')));
  expect(values.backbuffer).toBe(`${geometry.canvasWidth}x${geometry.canvasHeight}`);
  expect(Number(values.frames)).toBeGreaterThan(0);

  // 640x360 scaled to fit: whole-number scale while it fits, fractional only when the viewport is
  // smaller than the design size (a phone in the entry page).
  const raw = Math.min(geometry.canvasWidth / 640, geometry.canvasHeight / 360);
  const expectedScale = raw >= 1 ? Math.floor(raw) : raw;
  expect(Number(values.scale)).toBeCloseTo(expectedScale, 3);
});
