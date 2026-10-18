const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');

test('a breeze perch explains its approach, parks the basket and launches with held heat',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');await act(page,'reset=3535');await act(page,'step=1');
 const deck=JSON.parse(await page.evaluate(()=>window.__GAME.raw('decks'))).find(d=>d.kind==='perch');
 expect(deck.halfWidth).toBe(56);
 const warp=y=>page.evaluate(({x,y})=>window.__GAME.raw('warp',`${x}=${y}`),{x:deck.x,y});
 await warp(deck.altitude-20);expect((await page.evaluate(()=>window.__GAME.state())).hint).toBe('RISE ABOVE DECK');
 await warp(deck.altitude+30);expect((await page.evaluate(()=>window.__GAME.state())).hint).toBe('LAND TO REST');
 await warp(deck.altitude+.01);await act(page,'step=1');await act(page,'screen=world');
 let state=await page.evaluate(()=>window.__GAME.state());expect(state.parked).toBe(true);expect(state.hint).toBe('PERCHED: SPACE');
 const x=state.x;await act(page,'step=180');state=await page.evaluate(()=>window.__GAME.state());
 expect(state.x).toBe(x);expect(state.altitude).toBe(deck.altitude);expect(state.velocityX).toBe(0);expect(state.cargoStatus).toBe('carried');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`35-perch-rest-${info.project.name}.png`)});
 await page.evaluate(async()=>{await window.__GAME.set('heat',1);await window.__GAME.step(120);});state=await page.evaluate(()=>window.__GAME.state());
 expect(state.altitude).toBeGreaterThan(deck.altitude+8);expect(state.parked).toBe(false);expect(state.support).toBe('air');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`35-perch-takeoff-${info.project.name}.png`)});expect(errors).toEqual([]);
});
