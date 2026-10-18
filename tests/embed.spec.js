const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,clickUi,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');

test('host frames fill at fractional scales and pointer input follows resized presentation',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);await act(page,'settings=motion=1');
 const hash=await act(page,'hash');
 for(const [width,height]of [[960,540],[1024,576],[800,600],[420,760],[1280,720]]){
  await page.setViewportSize({width,height});
  await expect.poll(async()=>page.evaluate(()=>`${document.querySelector('#theCanvas').width}x${document.querySelector('#theCanvas').height}`)).toBe(`${width}x${height}`);
  const expected=Math.min(width/640,height/360);
  await expect.poll(async()=>Number(Object.fromEntries((await act(page,'metrics')).split(';').map(p=>p.split('='))).scale)).toBeCloseTo(expected,3);
  expect(Math.abs(width-640*expected)<1||Math.abs(height-360*expected)<1).toBe(true);
  expect(await act(page,'hash')).toBe(hash);
 }
 await page.setViewportSize({width:960,height:540});
 await expect.poll(async()=>Number(Object.fromEntries((await act(page,'metrics')).split(';').map(p=>p.split('='))).scale)).toBe(1.5);
 await clickUi(page,'settings');await expect.poll(()=>act(page,'screen')).toBe('settings');
 await clickUi(page,'back');await expect.poll(()=>act(page,'screen')).toBe('menu');
 await clickUi(page,'start');await expect.poll(()=>act(page,'screen')).toBe('world');await act(page,'pause');
 await page.setViewportSize(info.project.use.viewport);
 await reviewShot(page,{path:path.join(SHOTS_DIR,`37-embed-flight-${info.project.name}.png`)});
 await page.setViewportSize({width:960,height:540});
 await expect.poll(async()=>Number(Object.fromEntries((await act(page,'metrics')).split(';').map(p=>p.split('='))).scale)).toBe(1.5);
 await reviewShot(page,{path:path.join(SHOTS_DIR,`37-embed-960-${info.project.name}.png`)});
 expect(errors).toEqual([]);
});
