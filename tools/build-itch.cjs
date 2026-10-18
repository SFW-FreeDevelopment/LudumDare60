// Storefront compositions use the game's own pixel sprites, palette and lettering.
const fs=require('fs'),path=require('path'),zlib=require('zlib'),{spawnSync}=require('child_process');
const {SPRITES}=require('../art/.art/generate.js');
const root=path.resolve(__dirname,'..'),out=path.join(root,'art/itch');
const scratch=path.join(root,'_scratch/itch-art');fs.mkdirSync(scratch,{recursive:true});fs.mkdirSync(path.join(out,'src'),{recursive:true});
const theme=fs.readFileSync(path.join(root,'Balloonatic/Balloonatic.Game/Theme.cs'),'utf8');
const colors=Object.fromEntries([...theme.matchAll(/Color (\w+) = new Color\(([^)]+)\)/g)].map(m=>[m[1],'#'+m[2].split(',').slice(0,3).map(v=>Number(v.trim()).toString(16).padStart(2,'0')).join('')]));
const font=fs.readFileSync(path.join(root,'Balloonatic/Balloonatic.Game/Rendering/PixelFont.cs'),'utf8');
const glyphs=Object.fromEntries([...font.matchAll(/\['([^']+)'\] = Split\("([^"]+)"\)/g)].map(m=>[m[1],m[2].split('|')]));
function crc(b){let c=0xffffffff;for(const v of b){c^=v;for(let k=0;k<8;k++)c=c&1?0xedb88320^(c>>>1):c>>>1;}return(c^0xffffffff)>>>0;}
function chunk(t,b){const body=Buffer.concat([Buffer.from(t),b]),n=Buffer.alloc(4),c=Buffer.alloc(4);n.writeUInt32BE(b.length);c.writeUInt32BE(crc(body));return Buffer.concat([n,body,c]);}
function canvas(w,h,bg){
 const data=Buffer.alloc(w*h*4);
 const pixel=(x,y,c)=>{if(x<0||y<0||x>=w||y>=h)return;let o=(y*w+x)*4;data[o]=parseInt(c.slice(1,3),16);data[o+1]=parseInt(c.slice(3,5),16);data[o+2]=parseInt(c.slice(5,7),16);data[o+3]=255;};
 const box=(x,y,bw,bh,c)=>{for(let yy=y;yy<y+bh;yy++)for(let xx=x;xx<x+bw;xx++)pixel(xx,yy,c);};
 box(0,0,w,h,bg);
 const sprite=(n,x,y,s=1)=>{if(!Number.isInteger(s))throw Error('Integer pixel scales only');for(const [px,py,c]of SPRITES[n].frames[0])box(x+px*s,y+py*s,s,s,c);};
 const text=(t,y,s,c,cx=w/2)=>{let x=Math.floor(cx-(t.length*6-1)*s/2);[...t].forEach((ch,i)=>(glyphs[ch]||[]).forEach((row,dy)=>[...row].forEach((v,dx)=>{if(v==='#')box(x+(i*6+dx)*s,y+dy*s,s,s,c);})));};
 const save=n=>{const ih=Buffer.alloc(13);ih.writeUInt32BE(w);ih.writeUInt32BE(h,4);ih[8]=8;ih[9]=6;const rows=Buffer.alloc((w*4+1)*h);for(let y=0;y<h;y++)data.copy(rows,y*(w*4+1)+1,y*w*4,(y+1)*w*4);fs.writeFileSync(path.join(scratch,n+'.png'),Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ih),chunk('IDAT',zlib.deflateSync(rows,{level:9})),chunk('IEND',Buffer.alloc(0))]));};
 return {sprite,text,save,box};
}
const cover=canvas(756,600,colors.SkyPale);
cover.sprite('cloud-mid',-30,225);cover.sprite('cloud-mid',580,250);cover.sprite('far-isle',38,265);cover.sprite('far-isle',585,240);
cover.sprite('cloud-large',-70,440,2);cover.sprite('cloud-large',355,480,2);
cover.sprite('island-delivery',35,430);cover.sprite('nursery-house',88,326);
cover.sprite('island-delivery',490,430);cover.sprite('garden-mill',543,326);
cover.sprite('balloon-happy',258,238,2);cover.sprite('title-logo',28,28);
cover.text('FLOAT. EXPLORE. DELIVER DREAMS.',212,3,colors.Ink);
cover.text('FOR CHARLOTTE',563,3,colors.BalloonPlum);cover.sprite('heart-on',497,565,2);cover.save('cover');
const banner=canvas(1200,300,colors.SkyPale);
banner.sprite('cloud-large',-60,180);banner.sprite('cloud-large',400,240);banner.sprite('cloud-large',930,170);
banner.sprite('far-isle',10,50);banner.sprite('far-isle',1030,65);banner.sprite('balloon-happy',185,0,2);
banner.sprite('title-logo',450,22);banner.text('FLOAT. EXPLORE. DELIVER DREAMS.',252,3,colors.Ink,800);banner.save('banner');
const embed=canvas(960,540,colors.SkyPale);
embed.sprite('far-isle',30,15);embed.sprite('far-isle',800,35);
embed.sprite('cloud-large',-80,430,2);embed.sprite('cloud-large',520,405,2);
embed.sprite('island-delivery',520,330);embed.sprite('nursery-house',573,226);
embed.sprite('island-delivery',740,220);embed.sprite('garden-mill',793,116);
embed.sprite('balloon-happy',100,150,2);embed.save('embed');
const background=canvas(512,512,colors.CloudLavender);
background.sprite('cloud-small',32,48);background.sprite('cloud-small',316,292);
background.sprite('heart-on',272,120,2);background.sprite('heart-on',104,380,2);background.save('background');
fs.writeFileSync(path.join(out,'theme.json'),JSON.stringify({background:colors.CloudLavender,body:colors.TagCream,text:colors.PanelBrown,links:colors.BalloonPlum,buttons:colors.BalloonPlum,font:'Lora',embed:{width:960,height:540,fullscreen:true}},null,2)+'\n');
const server=process.env.ASEPRITE_MCP_DIR||path.join(process.env.USERPROFILE,'Repos/aseprite-mcp');
const result=spawnSync('uv',['run','--project',server,path.join(__dirname,'export-itch.py'),server],{cwd:root,stdio:'inherit'});if(result.status!==0)process.exit(result.status||1);
