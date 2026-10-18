const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
const backdrop=page=>page.evaluate(async()=>Object.fromEntries((await window.__GAME.raw('backdrop')).split(';').map(x=>x.split('='))));

test('the cloudscape drifts calmly, honors reduced motion and never changes the flight',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'reset=2323');
 await act(page,'settings=motion=0');await act(page,'step=600');
 const sky=await backdrop(page);expect(sky.floor).toBe('clouds');expect(sky.atmosphere).toBe('day');expect(Number(sky.drift)).toBe(6);
 await act(page,'settings=motion=1');expect(Number((await backdrop(page)).drift)).toBe(0);
 for(const [pose,alt,x] of [['low',60,0],['middle',200,300],['high',390,300],['arrival',90,450]]) {
   await act(page,'reset=2323');await act(page,'step=1');await page.evaluate(args=>window.__GAME.raw('warp',args),`${x}=${alt}`);
   await act(page,'screen=world');await expect.poll(()=>act(page,'screen')).toBe('world');
   const hash=await act(page,'hash');await reviewShot(page,{path:path.join(SHOTS_DIR,`23-sky-${pose}-${info.project.name}.png`)});
   expect(await act(page,'hash')).toBe(hash);
 }
 expect(errors).toEqual([]);
});
