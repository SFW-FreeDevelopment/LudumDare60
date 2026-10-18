// Anonymous production smoke checks for our own itch.io page and uploaded WebGL build.
const {chromium,devices}=require('@playwright/test'),assert=require('node:assert/strict'),path=require('node:path'),fs=require('node:fs');
const url=process.argv[2],buildOnly=process.argv.includes('--build');
if(!url)throw Error('Pass the public page URL, or a hosted build URL with --build.');
(async()=>{
 const browser=await chromium.launch(),report=[];
 try{for(const [name,viewport]of [['desktop',{width:1280,height:720}],['narrow',{width:420,height:760}]]){
  const context=await browser.newContext({...(!buildOnly&&name==='narrow'?devices['Pixel 7']:{}),viewport,deviceScaleFactor:1}),page=await context.newPage(),errors=[],failures=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('console',m=>{if(m.type()==='error')errors.push(m.text());});
  page.on('requestfailed',r=>failures.push({url:r.url(),error:r.failure()?.errorText}));
  const response=await page.goto(url,{waitUntil:'domcontentloaded',timeout:180000});
  if(!response.ok())console.error({status:response.status(),retryAfter:response.headers()['retry-after'],body:(await page.locator('body').innerText()).slice(0,1200)});
  assert(response.ok(),`HTTP ${response.status()}`);
  const shot=pose=>page.screenshot({path:path.join(__dirname,`../docs/screenshots/36-itch-${pose}-${name}.png`)});
  let game=page;
  if(!buildOnly){
   await page.getByRole('heading',{name:'🎈 Float. Explore. Deliver dreams.',exact:true}).waitFor();
   await page.getByRole('img',{name:"Charlotte's Balloonatic",exact:true}).waitFor();
   await page.evaluate(()=>document.fonts.ready);await shot('page');
   await page.getByText('A brave girl. A big sky. Three little dreams to deliver.',{exact:true}).scrollIntoViewIfNeeded();await shot('copy');
   await page.getByRole('button',{name:/^(Run game|Launch game)$/}).click();
   await page.locator('iframe#game_drop').waitFor();
   game=await (await page.locator('iframe#game_drop').elementHandle()).contentFrame();
  }
  await game.waitForFunction(()=>window.__GAME?.ready===true,null,{timeout:180000});
  assert.equal(await game.evaluate(()=>window.__GAME.screen()),'menu');
  const canvas=game.locator('#theCanvas');await canvas.press('Enter');
  await game.waitForFunction(async()=>await window.__GAME.screen()==='world');
  await page.keyboard.down('Space');
  await game.waitForFunction(async()=>{const s=await window.__GAME.state();return s.heating&&s.temperature>0;});
  await page.keyboard.up('Space');
  const state=await game.evaluate(async()=>{await window.__GAME.pause();await window.__GAME.reset(3636);await window.__GAME.step(1);return window.__GAME.state();});
  assert.equal(state.cargoStatus,'carried');
  const credits=await game.evaluate(()=>window.__GAME.raw('creditstext'));assert(!/ELEVENLABS|OPENAI|QWEN|\bAI\b/i.test(credits));
  const voyage=[];
  for(const id of ['nursery-skies','winding-trail','night-crossing']){
   const result=await game.evaluate(async id=>{await window.__GAME.reset(3636);await window.__GAME.level(id);await window.__GAME.screen('world');const result=await window.__GAME.raw('fly','120');return {result,state:await window.__GAME.state()};},id);
   assert.equal(result.result,'ok:delivered');assert.equal(result.state.cargoStatus,'delivered');assert(result.state.hearts>0);voyage.push({level:id,hearts:result.state.hearts,stars:result.state.stars});
  }
  await game.evaluate(async()=>{await window.__GAME.reset(3636);await window.__GAME.screen('world');await window.__GAME.step(1);await window.__GAME.raw('warp','220=120.01');await window.__GAME.step(1);});
  const perch=await game.evaluate(()=>window.__GAME.state());assert.equal(perch.parked,true);assert.equal(perch.hint,'PERCHED: SPACE');
  await game.waitForTimeout(350);if(!buildOnly){await page.locator('iframe#game_drop').scrollIntoViewIfNeeded();await shot('flight');}
  assert.deepEqual(errors,[]);assert.deepEqual(failures,[]);
  report.push({viewport:name,mode:!buildOnly&&name==='narrow'?'mobile browser; keyboard/mouse required':'desktop',url,gameUrl:game.url(),ready:true,public:!buildOnly,realHeatInput:true,creditsClean:true,perchParked:true,voyage,errors,failures});await context.close();
 }}finally{await browser.close();}
 fs.mkdirSync(path.join(__dirname,'../_scratch'),{recursive:true});fs.writeFileSync(path.join(__dirname,'../_scratch/i36-host-proof.json'),JSON.stringify(report,null,2));console.log(JSON.stringify(report,null,2));
})().catch(e=>{console.error(e);process.exitCode=1;});
