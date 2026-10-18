const { test, expect } = require('@playwright/test');
const path = require('path');
const { bootGame, act, clickDesign, watchForErrors, SHOTS_DIR } = require('./support');
const pairs = text => Object.fromEntries(text.split(';').map(p => p.split('=')));
const audio = async page => pairs(await act(page, 'audio'));
const mix = page => page.evaluate(async () => Object.fromEntries((await window.__GAME.raw('audioMix')).split(';').map(p => p.split('='))));

test('all volume endpoints keep independent audio buses healthy', async ({page}, info) => {
  const errors = watchForErrors(page);
  await bootGame(page);
  await clickDesign(page, 320, 172);
  await expect.poll(() => act(page, 'screen')).toBe('settings');
  for (let volume = 0; volume <= 10; volume++) {
    await act(page, `settings=music=${volume}`);
    await act(page, `settings=sfx=${volume}`);
    await expect.poll(async () => Number((await mix(page)).music)).toBe(volume / 10);
    await expect.poll(async () => Number((await mix(page)).burner)).toBe(Number((volume * .06).toFixed(2)));
    expect((await audio(page)).broken).toBe('0');
  }
  // The actual right-arrow UI must also safely cross the old failing 8→9 boundary.
  await act(page, 'settings=music=8');
  await page.keyboard.press('ArrowRight', {delay:100});
  await page.waitForTimeout(60); // let Update observe key-up before the next press of the same key
  await expect.poll(() => act(page, 'settings')).toContain('music=9;');
  await page.keyboard.press('ArrowRight', {delay:100});
  await expect.poll(() => act(page, 'settings')).toContain('music=10;');
  expect((await audio(page)).broken).toBe('0');
  await page.screenshot({path:path.join(SHOTS_DIR, `16-audio-settings-${info.project.name}.png`)});

  await act(page, 'settings=music=0');
  await act(page, 'settings=sfx=10');
  await act(page, 'reset=1616');
  await act(page, 'screen=world');
  await expect.poll(() => act(page, 'screen')).toBe('world');
  await page.evaluate(async()=>{await window.__GAME.set('heat',1);await window.__GAME.step(120);});
  await expect.poll(async () => (await audio(page)).burner).toBe('playing');
  expect((await audio(page)).music).toBe('stopped');
  await act(page, 'settings=sfx=0');
  await expect.poll(async () => (await audio(page)).burner).toBe('stopped');
  await act(page, 'settings=music=10');
  await expect.poll(async () => (await audio(page)).music).toBe('playing');
  await act(page, 'settings=sfx=10');
  await expect.poll(async () => (await audio(page)).burner).toBe('playing');
  await page.evaluate(async()=>{await window.__GAME.set('ballast',1);await window.__GAME.step(1);});
  await expect.poll(async () => (await audio(page)).broken).toBe('0');
  expect(errors).toEqual([]);
});
