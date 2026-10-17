// @ts-check
const { test, expect } = require('@playwright/test');
const { bootGame, watchForErrors } = require('./support');

/**
 * The delivery loop (PLAN I5) asserted where it actually ships: in the browser, through WASM,
 * through window.__GAME. The pure-core suite proves the rule; this proves the rule survives the
 * trip — the autopilot lands a parcel on the deck, a finished run freezes without stopping its
 * clock, and a restart replays the same sky from the launch deck.
 */
test('the autopilot delivers the parcel onto the deck in the browser', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const run = await page.evaluate(async () => {
    await window.__GAME.reset('beef');
    const beforeDelivered = await window.__GAME.delivered();
    const reply = await window.__GAME.deliver(120);
    return {
      beforeDelivered,
      reply,
      delivered: await window.__GAME.delivered(),
      complete: await window.__GAME.complete(),
      stars: Number(await window.__GAME.stars()),
      bounces: Number(await window.__GAME.bounces()),
      x: parseFloat(await window.__GAME.x()),
      altitude: parseFloat(await window.__GAME.altitude()),
      ballast: Number(await window.__GAME.ballast()),
    };
  });

  expect(run.beforeDelivered).toBe('0');
  expect(run.reply).toBe('ok:delivered');
  expect(run.delivered).toBe('1');
  expect(run.complete).toBe('1');
  expect(run.stars).toBeGreaterThanOrEqual(1);
  expect(run.bounces).toBe(0);                                  // the ghost pilot lands clean
  expect(run.x).toBeGreaterThanOrEqual(544);                    // on the deck (600 ± 56)
  expect(run.x).toBeLessThanOrEqual(656);
  expect(run.altitude).toBeCloseTo(38, 1);                      // at rest on the deck plane
  expect(run.ballast).toBeGreaterThanOrEqual(1);                // a delivery is flown with a brake in hand
});

test('a finished run ignores the burner but keeps its clock', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const frozen = await page.evaluate(async () => {
    await window.__GAME.reset('beef');
    await window.__GAME.deliver(120);
    const temperature = parseFloat(await window.__GAME.temperature());
    const tick = Number(await window.__GAME.tick());
    const resultsClock = (await window.__GAME.state()).completedAtTick;
    await window.__GAME.set('heat', 1);
    await window.__GAME.step(600);
    return {
      temperature,
      tick,
      resultsClock,
      temperatureAfter: parseFloat(await window.__GAME.temperature()),
      tickAfter: Number(await window.__GAME.tick()),
      resultsClockAfter: (await window.__GAME.state()).completedAtTick,
      delivered: await window.__GAME.delivered(),
    };
  });

  expect(frozen.temperatureAfter).toBe(frozen.temperature);     // the run is over; the burner is scenery
  expect(frozen.tickAfter - frozen.tick).toBe(600);             // the world's clock keeps its honest time
  expect(frozen.resultsClock).toBeGreaterThan(0);               // the banner's TIME is a real stamp...
  expect(frozen.resultsClockAfter).toBe(frozen.resultsClock);   // ...and it stopped at the landing, not kept climbing
  expect(frozen.delivered).toBe('1');
});

test('a delivery hands the player the next course, and the last one loops home', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const tour = await page.evaluate(async () => {
    await window.__GAME.reset('beef');
    await window.__GAME.deliver(120);
    await window.__GAME.advance();
    const second = await window.__GAME.state();
    await window.__GAME.advance();
    const third = await window.__GAME.state();
    await window.__GAME.advance();
    const looped = await window.__GAME.state();
    return { second, third, looped };
  });

  expect(tour.second.level).toBe('winding-trail');              // R after a delivery flies the next course
  expect(tour.second.delivered).toBe(false);                    // fresh run: nothing won yet...
  expect(tour.second.complete).toBe(false);                     // ...and the flight is live again
  expect(tour.second.ballastHeld).toBe(2);                      // the brake is back in hand
  expect(tour.second.seed).not.toBe('000000000000beef');        // a different sky, not a replay of level one
  expect(tour.third.level).toBe('night-crossing');
  expect(tour.looped.level).toBe('nursery-skies');              // the last course sends you home to fly again
});

test('a restart replays the same sky from the launch deck', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  const restarted = await page.evaluate(async () => {
    const fresh = await window.__GAME.reset('1234');            // "ok:<hash>" of the untouched opening
    await window.__GAME.set('heat', 1);
    await window.__GAME.step(300);
    const flownAltitude = parseFloat(await window.__GAME.altitude());
    const afterRestart = await window.__GAME.restart();
    return {
      fresh,
      flownAltitude,
      afterRestart,
      seed: await window.__GAME.seed(),
      altitude: parseFloat(await window.__GAME.altitude()),
    };
  });

  expect(restarted.flownAltitude).toBeGreaterThan(38);          // the flight really went up...
  expect(restarted.afterRestart).toBe(restarted.fresh);         // ...and R returns literally that opening
  expect(restarted.seed).toBe('0000000000001234');              // same seed: the same sky, not a new roll
  expect(restarted.altitude).toBeCloseTo(38, 1);                // back on the launch deck
});

test('restart works in live play, dropping the player back on the deck', async ({ page }) => {
  watchForErrors(page);
  await bootGame(page);

  // Settle the live game onto the world screen first (reset parks the state on the test bed), then
  // hand time back and fly for a beat before pressing the equivalent of R.
  await page.evaluate(() => window.__GAME.reset('1234'));
  await page.evaluate(() => window.__GAME.screen('world'));
  await expect.poll(() => page.evaluate(() => window.__GAME.screen()), { timeout: 10_000 }).toBe('world');

  const live = await page.evaluate(async () => {
    await window.__GAME.resume();                               // hand time back to the game loop
    await new Promise((resolve) => setTimeout(resolve, 250));   // live frames run: the clock moves
    const flying = Number(await window.__GAME.tick());
    await window.__GAME.restart();
    return { flying, altitude: parseFloat(await window.__GAME.altitude()), screen: await window.__GAME.screen() };
  });

  expect(live.flying).toBeGreaterThan(0);                       // live play was actually playing
  expect(live.altitude).toBeCloseTo(38, 1);                     // restart landed it back on the deck, unpaused
  expect(live.screen).toBe('world');                            // the player stays on the screen they were on
});
