const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,clickUi,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
const presentation=page=>page.evaluate(async()=>Object.fromEntries((await window.__GAME.raw('presentation')).split(';').map(x=>x.split('='))));

test('losing focus clears held heat and pauses until a deliberate resume',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await clickUi(page,'start');await expect.poll(()=>act(page,'screen')).toBe('world');
 await page.keyboard.down('Space');await expect.poll(async()=>(await page.evaluate(()=>window.__GAME.state())).heating).toBe(true);
 await page.evaluate(()=>window.dispatchEvent(new Event('blur')));await expect.poll(()=>act(page,'screen')).toBe('pause');
 expect((await presentation(page)).focus).toBe('0');expect((await page.evaluate(()=>window.__GAME.state())).heat).toBe(false);
 const hash=await act(page,'hash');await reviewShot(page,{path:path.join(SHOTS_DIR,`30-focus-pause-${info.project.name}.png`)});expect(await act(page,'hash')).toBe(hash);
 await page.keyboard.up('Space');await page.evaluate(()=>window.dispatchEvent(new Event('focus')));await expect.poll(async()=>(await presentation(page)).focus).toBe('1');
 expect(await act(page,'screen')).toBe('pause');await clickUi(page,'resume');await expect.poll(()=>act(page,'screen')).toBe('world');
 await expect.poll(async()=>(await page.evaluate(()=>window.__GAME.state())).heating).toBe(false);expect(errors).toEqual([]);
});

test('settings opened from pause return to that same frozen flight',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await clickUi(page,'start');await expect.poll(()=>act(page,'screen')).toBe('world');
 await clickUi(page,'pause');await expect.poll(()=>act(page,'screen')).toBe('pause');const hash=await act(page,'hash');
 await clickUi(page,'settings');await expect.poll(()=>act(page,'screen')).toBe('settings');
 await page.keyboard.press('ArrowRight',{delay:100});await expect.poll(()=>act(page,'settings')).toContain('music=9;');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`30-pause-settings-${info.project.name}.png`)});
 await clickUi(page,'back');await expect.poll(()=>act(page,'screen')).toBe('pause');expect(await act(page,'hash')).toBe(hash);
 await clickUi(page,'resume');await expect.poll(()=>act(page,'screen')).toBe('world');expect(errors).toEqual([]);
});

test('presentation options leave the flight hash identical and freeze decorative frames',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await clickUi(page,'start');await expect.poll(()=>act(page,'screen')).toBe('world');const hashes=[];
 for(const [motion,volume] of [[0,0],[1,0],[0,10],[1,10]]) {
   await act(page,`settings=motion=${motion}`);await act(page,`settings=music=${volume}`);await act(page,`settings=sfx=${volume}`);await act(page,'settings=assist=0');
   await act(page,'reset=3030');await act(page,'level=night-crossing');await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
   await page.evaluate(async()=>{await window.__GAME.set('heat',1);await window.__GAME.step(150);await window.__GAME.set('ballast',1);await window.__GAME.step(1);await window.__GAME.set('heat',0);await window.__GAME.step(240);await window.__GAME.set('heat',1);await window.__GAME.step(96);});
   hashes.push(await act(page,'hash'));
   await expect.poll(async()=>Object.fromEntries((await act(page,'audio')).split(';').map(x=>x.split('='))).music).toBe(volume?'playing':'stopped');
 }
 expect(new Set(hashes).size).toBe(1);
 let frames=await presentation(page);expect(frames.burner).toBe('1');expect(frames.bird).toBe('0');expect(frames.sock).toBe('0');expect(frames.mill).toBe('0');
 await act(page,'step=24');frames=await presentation(page);expect(frames.burner).toBe('1');expect(frames.bird).toBe('0');expect(frames.sock).toBe('0');expect(frames.mill).toBe('0');
 await page.evaluate(()=>window.__GAME.raw('warp','720=180'));await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`30-reduced-motion-${info.project.name}.png`)});expect(errors).toEqual([]);
});
