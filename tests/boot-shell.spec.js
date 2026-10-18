const {test,expect}=require('@playwright/test');
const path=require('path');
const {watchForErrors,reviewShot,SHOTS_DIR}=require('./support');
test('cold boot uses the game art and pixel lettering until the rendered game is ready',async({page},info)=>{
 const errors=watchForErrors(page);let release;const gate=new Promise(resolve=>release=resolve);
 await page.route('**/_framework/blazor.boot.json',async route=>{await gate;await route.continue();});
 await page.goto('/index.html',{waitUntil:'domcontentloaded'});
 try {
   await expect(page.locator('#boot-shell')).toBeVisible();
   await expect.poll(()=>page.evaluate(()=>window.__GAME?.ready)).toBe(false);
   await page.waitForFunction(()=>[...document.querySelectorAll('#boot-shell img')].every(img=>img.complete&&img.naturalWidth>0));
   expect(await page.title()).toBe("Charlotte's Balloonatic");
   await page.screenshot({path:path.join(SHOTS_DIR,`30-branded-boot-${info.project.name}.png`)});
 }finally{release();}
 await page.waitForFunction(()=>window.__GAME?.ready===true,null,{timeout:180_000});
 await expect(page.locator('#boot-shell')).toHaveCount(0);
 expect(await page.evaluate(()=>window.__GAME.screen())).toBe('menu');
 await reviewShot(page,{path:path.join(SHOTS_DIR,`30-branded-ready-${info.project.name}.png`)});expect(errors).toEqual([]);
});
