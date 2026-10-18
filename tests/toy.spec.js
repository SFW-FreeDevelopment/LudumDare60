const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
test('Toy Isles teaches a carrying band and brings its teddy home with hazards active',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');await act(page,'reset=2727');await act(page,'level=winding-trail');await act(page,'step=1');
 let state=await page.evaluate(()=>window.__GAME.state());expect(state.cargo).toBe('teddy');expect(state.windDirection).toBe(-1);expect(state.hint).toBe('RISE FOR > WIND');
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`27-toy-launch-${info.project.name}.png`)});
 await page.evaluate(async()=>{await window.__GAME.set('heat',1);for(let i=0;i<40;i++){await window.__GAME.step(10);if((await window.__GAME.state()).altitude>=140)break;}await window.__GAME.set('heat',0);});
 state=await page.evaluate(()=>window.__GAME.state());expect(state.altitude).toBeGreaterThanOrEqual(140);expect(state.windDirection).toBe(1);
 await page.evaluate(()=>window.__GAME.raw('warp','570=100'));
 await reviewShot(page,{path:path.join(SHOTS_DIR,`27-toy-arrival-${info.project.name}.png`)});
 expect(await page.evaluate(()=>window.__GAME.raw('art'))).toContain('landmark=toy-blocks');
 await act(page,'reset=2727');await act(page,'level=winding-trail');expect(await page.evaluate(()=>window.__GAME.raw('fly','120'))).toBe('ok:delivered');
 state=await page.evaluate(()=>window.__GAME.state());expect(state.cargoStatus).toBe('delivered');expect(state.hearts).toBeGreaterThan(0);
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`27-toy-home-${info.project.name}.png`)});expect(errors).toEqual([]);
});
