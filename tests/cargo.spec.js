const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
test('a parcel must be collected and carried to its recipient',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');
 await act(page,'reset=1919');
 expect((await page.evaluate(()=>window.__GAME.state())).cargoStatus).toBe('waiting');
 await page.evaluate(()=>window.__GAME.raw('warp','550=60'));await act(page,'step=600');
 expect((await page.evaluate(()=>window.__GAME.state())).delivered).toBe(false);
 await act(page,'reset=1919');await act(page,'step=1');
 let s=await page.evaluate(()=>window.__GAME.state());expect(s.cargo).toBe('teddy');expect(s.cargoStatus).toBe('carried');expect(s.recipient).toBe('NURSERY');
 await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`19-cargo-pickup-${info.project.name}.png`)});
 expect(await act(page,'deliver=120')).toBe('ok:delivered');
 s=await page.evaluate(()=>window.__GAME.state());expect(s.cargoStatus).toBe('delivered');expect(s.cargo).toBe('none');
 const pickedUp=s.pickupTick;await act(page,'step=60');expect((await page.evaluate(()=>window.__GAME.state())).pickupTick).toBe(pickedUp);
 await reviewShot(page,{path:path.join(SHOTS_DIR,`19-cargo-home-${info.project.name}.png`)});
 expect(errors).toEqual([]);
});
