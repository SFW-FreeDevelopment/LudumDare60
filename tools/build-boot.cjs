// The web boot shell uses the same palette and exact 5x7 masks as the shared game.
const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..'),web=path.join(root,'Balloonatic/Balloonatic.Blazor.GL/wwwroot');
const font=fs.readFileSync(path.join(root,'Balloonatic/Balloonatic.Game/Rendering/PixelFont.cs'),'utf8');
const glyphs=Object.fromEntries([...font.matchAll(/\['([^']+)'\] = Split\("([^"]+)"\)/g)].map(m=>[m[1],m[2].split('|')]));
const theme=fs.readFileSync(path.join(root,'Balloonatic/Balloonatic.Game/Theme.cs'),'utf8');
const palette=Object.fromEntries([...theme.matchAll(/Color (\w+) = new Color\(([^)]+)\)/g)].map(m=>[m[1],'#'+m[2].split(',').slice(0,3).map(v=>Number(v.trim()).toString(16).padStart(2,'0')).join('')]));
const names={ink:'Ink',letterbox:'LetterBox',sky:'SkyMid',paper:'TagCream',wood:'WoodMid',edge:'WoodDark',light:'WoodLight',panel:'PanelBrown',pink:'HeartPink'};
fs.writeFileSync(path.join(web,'css/boot-theme.css'),':root{'+Object.entries(names).map(([n,p])=>`--boot-${n}:${palette[p]}`).join(';')+'}\n');
function label(file,text,width,scale,color) {
 const height=scale*7+36,x=Math.floor((width-(text.length*6-1)*scale)/2),rects=[];
 for(const [offset,fill] of [[2,palette.Ink],[0,palette[color]]]) {
  [...text].forEach((char,index)=>(glyphs[char]||[]).forEach((row,y)=>[...row].forEach((v,xx)=>{
   if(v==='#')rects.push(`<rect x="${x+(index*6+xx)*scale+offset}" y="${18+y*scale+offset}" width="${scale}" height="${scale}" fill="${fill}"/>`);
  })));
 }
 fs.writeFileSync(path.join(web,'images',file),`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" shape-rendering="crispEdges">${rects.join('')}</svg>\n`);
}
label('boot-warmup.svg','WARMING UP...',600,6,'TagCream');
label('boot-dedication.svg','FOR CHARLOTTE ♥',600,6,'HeartPink');
label('boot-error.svg','THE BURNER NEEDS A REST',600,4,'TagCream');
label('boot-reload.svg','RELOAD',320,6,'TagCream');
console.log('Boot shell: Aseprite crops, shared palette and pixel-glyph SVG labels exported.');
