const { test, expect } = require('@playwright/test');
const path = require('path');
const {bootGame, act, SHOTS_DIR, watchForErrors} = require('./support');
const {matchShot,reviewShot}=require('./support');
test('every ballast loadout can land and a missed course returns safely',async({page},info)=>{
 const errors=watchForErrors(page); await bootGame(page);
 for(const bags of [2,1,0]){
  await act(page,'reset=1717');
  await act(page,'step=1'); // collect the launch parcel before testing the final approach
  await page.evaluate(()=>window.__GAME.raw('warp','550=100'));
  for(let i=2;i>bags;i--)await page.evaluate(async()=>{await window.__GAME.set('ballast',1);await window.__GAME.step(1);});
  await act(page,'step=600');
  const state=await page.evaluate(()=>window.__GAME.state());
  expect(state.delivered,`landing with ${bags} bags`).toBe(true);
  expect(state.bounces).toBe(0);expect(state.ballastHeld).toBe(bags);
 }
 await act(page,'reset=1717');await page.evaluate(()=>window.__GAME.raw('warp','850=150'));
 await act(page,'step=1');let state=await page.evaluate(()=>window.__GAME.state());
 expect(state.recoveries).toBe(1);expect(state.hearts).toBe(3);expect(state.x).toBeLessThan(600);
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page, {path:path.join(SHOTS_DIR,`17-flight-recovery-${info.project.name}.png`)});
 expect(errors).toEqual([]);
});
