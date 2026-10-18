const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,SHOTS_DIR}=require('./support');
const {matchShot,reviewShot}=require('./support');
test('wind ladder follows altitude and approach instruments report real flight',async({page},info)=>{
 await bootGame(page);await act(page,'reset=1818');
 let s=await page.evaluate(()=>window.__GAME.state());expect(s.band).toBe(0);expect(s.heatTrend).toBe('cool');
 await page.evaluate(async()=>{await window.__GAME.set('heat',1);await window.__GAME.step(240);});
 s=await page.evaluate(()=>window.__GAME.state());expect(s.band).toBeGreaterThan(0);expect(s.heatTrend).toBe('heating');
 await page.evaluate(async()=>{await window.__GAME.set('heat',0);await window.__GAME.step(1);});
 expect((await page.evaluate(()=>window.__GAME.state())).heatTrend).toBe('cooling');
 await act(page,'reset=1818');await page.evaluate(()=>window.__GAME.raw('warp','550=100'));
 s=await page.evaluate(()=>window.__GAME.state());expect(s.approach).toBe('gentle');
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page, {path:path.join(SHOTS_DIR,`18-wind-approach-${info.project.name}.png`)});
});
