const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
test('Nursery Skies teaches with a wind perch and is delivered with active hazards',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');await act(page,'reset=6060');
 expect((await page.evaluate(()=>window.__GAME.state())).platforms).toBe(1);
 await act(page,'step=1');await page.evaluate(()=>window.__GAME.raw('warp','185=170'));
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`20-nursery-perch-${info.project.name}.png`)});
 await act(page,'reset=6060');
 expect(await page.evaluate(()=>window.__GAME.raw('fly','120'))).toBe('ok:delivered');
 const state=await page.evaluate(()=>window.__GAME.state());expect(state.delivered).toBe(true);expect(state.hearts).toBe(3);expect(state.cargoStatus).toBe('delivered');
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`20-nursery-home-${info.project.name}.png`)});expect(errors).toEqual([]);
});
