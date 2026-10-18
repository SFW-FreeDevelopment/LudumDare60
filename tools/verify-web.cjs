// Exercise the actual published/archive HTTP root, including active WebAudio and the final voyage.
const {chromium}=require('@playwright/test');
const assert=require('node:assert/strict'),path=require('node:path');
const {bootGame,act,clickUi,reviewShot,watchForErrors,SHOTS_DIR}=require('../tests/support');
const baseURL=process.argv[2]||'http://localhost:4173';
const prefix=process.env.GAME_PROOF_PREFIX||'30-package';
(async()=>{
 const browser=await chromium.launch();const report=[];
 try {
  for(const [name,viewport] of [['desktop',{width:1280,height:720}],['narrow',{width:420,height:760}]]) {
   const context=await browser.newContext({baseURL,viewport});const page=await context.newPage(),errors=watchForErrors(page);
   const shot=pose=>reviewShot(page,{path:path.join(SHOTS_DIR,`${prefix}-${pose}-${name}.png`)});
   const boot=await bootGame(page);await shot('title');await clickUi(page,'start');
   await page.waitForFunction(async()=>await window.__GAME.screen()==='world');await page.keyboard.down('Space');
   await page.waitForFunction(async()=>(await window.__GAME.state()).heating===true);
   const timing=await page.evaluate(()=>new Promise(resolve=>{
    const samples=[];let previous=performance.now();
    function frame(now){samples.push(now-previous);previous=now;if(samples.length<121)requestAnimationFrame(frame);else{samples.shift();resolve({average:samples.reduce((a,b)=>a+b,0)/samples.length,worst:Math.max(...samples)});}}
    requestAnimationFrame(frame);
   }));
   const audio=Object.fromEntries((await act(page,'audio')).split(';').map(x=>x.split('=')));
   assert.equal(audio.broken,'0');assert.equal(audio.clips,'8');
   for(const loop of ['music','burner','wind'])assert.equal(audio[loop],'playing');
   assert(timing.average<20,`Active-audio cadence ${timing.average} ms`);
   await page.keyboard.up('Space');await act(page,'pause');await shot('flight');
   const deliveries=[];
   for(const id of ['nursery-skies','winding-trail','night-crossing']) {
    await act(page,'reset=3030');await act(page,`level=${id}`);await act(page,'screen=world');
    await page.waitForFunction(async()=>await window.__GAME.screen()==='world');
    assert.equal(await page.evaluate(()=>window.__GAME.raw('fly','120')),'ok:delivered');
    const state=await page.evaluate(()=>window.__GAME.state());assert(state.hearts>0);assert.equal(state.cargoStatus,'delivered');
    deliveries.push({id,hearts:state.hearts,stars:state.stars,seconds:state.completedAtTick/60});
    await act(page,'screen=worldmap');await page.waitForFunction(async()=>await window.__GAME.screen()==='worldmap');
   }
   assert((await page.evaluate(()=>window.__GAME.raw('progress'))).startsWith('count=3;'));
   // Settle the final map after the CPU-only route batch, including a compositor readback.
   const mapFrame=await page.evaluate(()=>window.__GAME.frames());
   await page.waitForFunction(async start=>Number(await window.__GAME.frames())>Number(start)+60,mapFrame);
   await page.screenshot();
   await shot('voyage');
   await clickUi(page,'voyage');await page.waitForFunction(async()=>await window.__GAME.screen()==='ending');await shot('ending');
   await clickUi(page,'replay');await page.waitForFunction(async()=>await window.__GAME.screen()==='world');assert.equal(await act(page,'complete'),'0');
   await page.evaluate(()=>window.__GAME.flushSave());await bootGame(page);
   assert((await page.evaluate(()=>window.__GAME.raw('progress'))).startsWith('count=3;'));assert.deepEqual(errors,[]);
   report.push({viewport:name,url:baseURL,bootMs:boot.readyAtMs,bytes:boot.downloadedBytes,activeAudioAverageMs:+timing.average.toFixed(3),worstMs:+timing.worst.toFixed(3),deliveries,consoleErrors:errors.length});
   await context.close();
  }
  console.log(JSON.stringify(report,null,2));
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
