const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,clickDesign,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
const ui=page=>page.evaluate(async()=>Object.fromEntries((await window.__GAME.raw('ui')).split(';').map(x=>x.split('='))));
test('essential type adapts to narrow view and delivery has a real pointer and keyboard action',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');await act(page,'reset=2424');
 await act(page,'step=1');await page.evaluate(()=>window.__GAME.raw('warp','450=90'));
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 expect((await ui(page)).body).toBe(info.project.name==='narrow'?'3':'2');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`24-readable-flight-${info.project.name}.png`)});
 await act(page,'reset=2424');expect(await page.evaluate(()=>window.__GAME.raw('fly','120'))).toBe('ok:delivered');
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`24-delivery-action-${info.project.name}.png`)});
 const [x,y,w,h]=(await ui(page)).result.split(',').map(Number);expect(h).toBeGreaterThanOrEqual(32);
 await clickDesign(page,x+w/2,y+h/2);await expect.poll(()=>act(page,'screen')).toBe('worldmap');
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await page.keyboard.press('Enter');await expect.poll(()=>act(page,'screen')).toBe('worldmap');expect(errors).toEqual([]);
});
