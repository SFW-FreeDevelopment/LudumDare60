const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
const art=page=>page.evaluate(async()=>Object.fromEntries((await window.__GAME.raw('art')).split(';').map(p=>p.split('='))));
test('the nursery has a landmark, the right parcel and a recipient who celebrates',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');await act(page,'reset=2222');
 const loaded=await art(page);expect(loaded.ready).toBe('1');expect(Number(loaded.slices)).toBeGreaterThanOrEqual(45);
 expect(loaded.landmark).toBe('nursery-house');expect(loaded.parcel).toBe('parcel-teddy');expect(loaded.recipient).toBe('waiting');
 await act(page,'step=1');await page.evaluate(()=>window.__GAME.raw('warp','450=90'));
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`22-nursery-arrival-${info.project.name}.png`)});
 await act(page,'reset=2222');expect(await page.evaluate(()=>window.__GAME.raw('fly','120'))).toBe('ok:delivered');
 expect((await art(page)).recipient).toBe('happy');
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`22-nursery-thanks-${info.project.name}.png`)});expect(errors).toEqual([]);
});
