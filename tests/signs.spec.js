const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');

test('world sign sprites keep their lettering and platform attachment through fractional camera motion',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'reset=3232');
 await act(page,'screen=world');const poses=[];
 for(let phase=0;phase<4;phase++){
  await page.evaluate(value=>window.__GAME.raw('warp',`${130+value}=${120+value}`),phase/4);
  const hash=await act(page,'hash');
  const signs=JSON.parse(await page.evaluate(()=>window.__GAME.raw('worldsigns')));
  for(const sign of signs){expect(sign.ready).toBe(true);expect(sign.asset).toMatch(/^(sign|tag)-/);
   expect(sign.x*2).toBe(Math.round(sign.x*2));expect(sign.y*2).toBe(Math.round(sign.y*2));}
  const home=signs.find(s=>s.text==='HOME'),perch=signs.find(s=>s.text==='BREEZE PERCH');
  expect(home.x-home.parentX).toBe(-16);expect(home.y-home.parentY).toBe(-16);
  expect(perch.x-perch.parentX).toBe(-30);expect(perch.y-perch.parentY).toBe(10);
  poses.push(home.x);
  await reviewShot(page,{path:path.join(SHOTS_DIR,`32-signs-phase${phase}-${info.project.name}.png`)});
  expect(await act(page,'hash')).toBe(hash);
 }
 expect(new Set(poses).size).toBeGreaterThan(1);expect(errors).toEqual([]);
});
