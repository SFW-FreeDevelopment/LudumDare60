const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,clickUi,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
const progress=page=>page.evaluate(async()=>Object.fromEntries((await window.__GAME.raw('progress')).split(';').map(x=>x.split('='))));
test('direct course selection stays honest, the voyage ends and results survive reload',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');
 await act(page,'level=night-crossing');expect((await progress(page)).count).toBe('0');
 expect(await page.evaluate(()=>window.__GAME.raw('fly','120'))).toBe('ok:delivered');
 await expect.poll(async()=>(await progress(page)).count).toBe('1');
 expect((await progress(page))['nursery-skies']).toBe('0');
 await act(page,'screen=worldmap');await expect.poll(()=>act(page,'screen')).toBe('worldmap');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`26-voyage-record-${info.project.name}.png`)});
 await clickUi(page,'voyage');await expect.poll(()=>act(page,'screen')).toBe('ending');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`26-sweet-dreams-${info.project.name}.png`)});
 await clickUi(page,'replay');await expect.poll(()=>act(page,'screen')).toBe('world');
 expect(await act(page,'level')).toBe('nursery-skies');expect(await act(page,'complete')).toBe('0');
 await act(page,'settings=music=3');await act(page,'settings=sfx=7');await act(page,'settings=assist=1');
 await act(page,'screen=settings');await expect.poll(()=>act(page,'screen')).toBe('settings');
 await page.keyboard.press('ArrowRight',{delay:100});await expect.poll(()=>act(page,'settings')).toContain('music=4;');
 await expect.poll(()=>page.evaluate(()=>localStorage.getItem('balloonatic.save.v1'))).toContain('music=4;sfx=7;assist=1;motion=1');
 const saved=await page.evaluate(()=>window.__GAME.raw('save'));
 for(const bad of [saved.replace('music=4','music=99'),saved.replace('version=1','version=2'),saved+';unknown=1']) {
   expect(await page.evaluate(value=>window.__GAME.raw('loadsave',value),bad)).toBe('error:bad-save');
   expect(await page.evaluate(()=>window.__GAME.raw('save'))).toBe(saved);
 }
 await bootGame(page);expect(await act(page,'settings')).toBe('music=4;sfx=7;assist=1;motion=1');
 expect((await progress(page)).count).toBe('1');expect((await progress(page))['night-crossing']).not.toBe('0');
 expect(errors).toEqual([]);
});
test('blocked browser storage still boots and plays',async({page})=>{
 await page.addInitScript(()=>{Storage.prototype.getItem=Storage.prototype.setItem=function(){throw new Error('storage unavailable');};});
 const errors=watchForErrors(page);await bootGame(page);
 expect(await page.evaluate(()=>window.__GAME.persistence)).toBe('unavailable');
 await clickUi(page,'start');await expect.poll(()=>act(page,'screen')).toBe('world');expect(errors).toEqual([]);
});
