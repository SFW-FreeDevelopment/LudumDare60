// Offline mastering of the committed ElevenLabs renders. npm run audio.
// Requires ffmpeg on PATH, or FFMPEG pointing to its executable.
'use strict';
const fs=require('fs'),path=require('path'),{spawnSync}=require('child_process');
const root=path.resolve(__dirname,'..'),out=path.join(root,'audio/export'),rate=22050;
const profiles={music:[.06,.32,.04],burner:[.055,.25,.15],wind:[.12,.35,.2],creak:[.07,.25,0],chime:[.065,.25,0],giggle:[.055,.22,0],jingle:[.07,.3,0],thump:[.075,.3,0]};
const requests=JSON.parse(fs.readFileSync(path.join(root,'assets/audio/requests.json'),'utf8'));
fs.mkdirSync(out,{recursive:true});
const report=[];
for(const record of requests.records) {
 const source=path.join(root,'assets/audio/source',record.source);
 const decoded=spawnSync(process.env.FFMPEG||'ffmpeg',['-v','error','-i',source,'-ac','1','-ar',String(rate),'-af','lowpass=f=6500','-f','f32le','pipe:1'],{maxBuffer:32*1024*1024});
 if(decoded.error||decoded.status!==0)throw decoded.error||new Error(decoded.stderr.toString());
 let samples=Array.from({length:decoded.stdout.length/4},(_,i)=>decoded.stdout.readFloatLE(i*4));
 const [target,cap,crossfade]=profiles[record.name];
 const mean=samples.reduce((s,x)=>s+x,0)/samples.length;samples=samples.map(x=>x-mean);
 const overlap=Math.floor(crossfade*rate);
 if(overlap) {
   const tail=samples.slice(-overlap),head=samples.slice(0,overlap);
   const blend=tail.map((x,i)=>x*(1-i/(overlap-1))+head[i]*i/(overlap-1));
   samples=samples.slice(overlap,-overlap).concat(blend);
 } else {
   const attack=Math.floor(rate*.003),release=Math.floor(rate*.02);
   samples=samples.map((x,i)=>x*Math.min(1,i/attack,(samples.length-1-i)/release));
 }
 const rms=Math.sqrt(samples.reduce((s,x)=>s+x*x,0)/samples.length),peak=samples.reduce((m,x)=>Math.max(m,Math.abs(x)),0);
 const gain=Math.min(target/rms,cap/peak);samples=samples.map(x=>x*gain);
 const wav=Buffer.alloc(44+samples.length*2);
 wav.write('RIFF',0);wav.writeUInt32LE(wav.length-8,4);wav.write('WAVEfmt ',8);wav.writeUInt32LE(16,16);
 wav.writeUInt16LE(1,20);wav.writeUInt16LE(1,22);wav.writeUInt32LE(rate,24);wav.writeUInt32LE(rate*2,28);
 wav.writeUInt16LE(2,32);wav.writeUInt16LE(16,34);wav.write('data',36);wav.writeUInt32LE(samples.length*2,40);
 for(let i=0;i<samples.length;i++)wav.writeInt16LE(Math.round(samples[i]*32767),44+i*2);
 fs.writeFileSync(path.join(out,record.name+'.wav'),wav);
 report.push({clip:record.name,seconds:+(samples.length/rate).toFixed(3),rate,rms:+(rms*gain).toFixed(4),peak:+(peak*gain).toFixed(4),loop:!!overlap,sourceSha256:require('crypto').createHash('sha256').update(fs.readFileSync(source)).digest('hex')});
}
fs.writeFileSync(path.join(out,'analysis.json'),JSON.stringify(report,null,2)+'\n');
console.log(report.map(x=>`${x.clip}: ${x.seconds}s / RMS ${x.rms} / peak ${x.peak}`).join('\n'));
