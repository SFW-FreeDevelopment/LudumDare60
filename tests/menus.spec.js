const {test,expect}=require('@playwright/test');
const path=require('path');
const {bootGame,act,clickUi,reviewShot,SHOTS_DIR,watchForErrors}=require('./support');
test('the illustrated menu family has readable shared controls and a keyboard route',async({page},info)=>{
 const errors=watchForErrors(page);await bootGame(page);
 const capture=async name=>reviewShot(page,{path:path.join(SHOTS_DIR,`25-${name}-${info.project.name}.png`)});
 await capture('title');
 await clickUi(page,'settings');await expect.poll(()=>act(page,'screen')).toBe('settings');await capture('settings');
 await clickUi(page,'back');await expect.poll(()=>act(page,'screen')).toBe('menu');
 await clickUi(page,'credits');await expect.poll(()=>act(page,'screen')).toBe('credits');await capture('credits');
 await clickUi(page,'back');await expect.poll(()=>act(page,'screen')).toBe('menu');
 await page.keyboard.press('ArrowDown');await page.keyboard.press('Enter');await expect.poll(()=>act(page,'screen')).toBe('levels');
 await capture('skies');await clickUi(page,'course0');await expect.poll(()=>act(page,'screen')).toBe('world');
 await clickUi(page,'pause');await expect.poll(()=>act(page,'screen')).toBe('pause');await capture('pause');
 await clickUi(page,'resume');await expect.poll(()=>act(page,'screen')).toBe('world');expect(errors).toEqual([]);
});
