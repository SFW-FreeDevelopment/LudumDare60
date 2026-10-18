const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');

test('all timber decks support basket edges and deliveries accept a slide along the surface',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');
 const shot=name=>reviewShot(page,{path:path.join(SHOTS_DIR,`33-${name}-${info.project.name}.png`)});
 for(const id of ['nursery-skies','winding-trail','night-crossing']){
  await act(page,'reset=3333');await act(page,`level=${id}`);
  const decks=JSON.parse(await page.evaluate(()=>window.__GAME.raw('decks')));
  for(const deck of decks)for(const side of [-1,1]){
   await act(page,'reset=3333');await act(page,`level=${id}`);await act(page,'step=1');
   await page.evaluate(({x,y})=>window.__GAME.raw('warp',`${x}=${y}`),{x:deck.x+side*(deck.halfWidth+8),y:deck.altitude+.01});
   await act(page,'step=1');const state=await page.evaluate(()=>window.__GAME.state());
   expect(state.support,`${id} ${deck.kind} edge ${side}`).toBe(deck.kind);
   expect(state.altitude).toBe(deck.altitude);expect(state.velocityY).toBe(0);expect(state.complete).toBe(false);
   if(id==='nursery-skies'&&deck.kind==='perch'&&side===-1){await act(page,'screen=world');await shot('perch-edge');}
  }
  const target=decks.find(d=>d.kind==='delivery'),side=id==='nursery-skies'?-1:1;
  await act(page,'reset=3333');await act(page,`level=${id}`);await act(page,'step=1');
  await page.evaluate(({x,y})=>window.__GAME.raw('warp',`${x}=${y}`),{x:target.x+side*64,y:38.01});
  await act(page,'step=1');await act(page,'screen=world');
  let state=await page.evaluate(()=>window.__GAME.state());expect(state.support).toBe('delivery');expect(state.delivered).toBe(false);
  expect(state.hint).toBe('DECK: DRIFT IN');expect(state.approach).toBe('landed');if(id==='nursery-skies')await shot('slide-contact');
  const initialX=state.x;await act(page,'step=60');state=await page.evaluate(()=>window.__GAME.state());
  expect(state.support).toBe('delivery');expect(state.altitude).toBe(38);expect(state.complete).toBe(false);
  expect(Math.sign(state.x-initialX)).toBe(-side);if(id==='nursery-skies')await shot('slide-travel');
  await act(page,'step=240');state=await page.evaluate(()=>window.__GAME.state());
  expect(state.delivered).toBe(true);expect(state.cargoStatus).toBe('delivered');expect(state.bounces).toBe(0);
  if(id==='nursery-skies')await shot('slide-home');
 }
 expect(errors).toEqual([]);
});
