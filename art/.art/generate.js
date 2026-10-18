/*
 * Charlotte's Balloonatic — sprite generator (art plan steps B/C).
 *
 * The single source of truth for every sprite in the game. Plain deterministic JS: the same file
 * drives the Aseprite MCP build (art/src/balloonatic.aseprite) and can be dumped with
 * `node generate.js` so the exported sheet is always re-derivable from the repo alone.
 *
 * Everything is authored at 2x internal pixels — the design canvas renders at 1280x720 — so a
 * design-size sprite lives as twice these pixels and carries twice the detail while remaining
 * pure pixel art (no filtering, no anti-alias). Colour rule mirrors Theme.cs: only the hexes in
 * PAL may appear, and shading is ramp *selection*, never interpolation, so every sprite pixel is
 * on-palette by construction and the palette audit is meaningful.
 *
 * Sprite names become the loader's lookup keys; animated sprites stack frames vertically inside
 * one slice column (frame i at y + i*h). Draw order inside a frame is the order the pixel arrays
 * are concatenated — later entries paint over earlier ones.
 */
"use strict";

// --- palette (exact Theme.cs values — the only place hex may appear) ---
const PAL = {
  K: "#0b1129",            // ink outline
  D: "#241a1e",            // dim (burner throat shadow)
  // wood / wicker / rope / brass
  w0: "#bd733e", w1: "#b7633d", w2: "#653a38",
  wk: "#d2b383", wd: "#875449", bark: "#8d5726", brass: "#f1bf37",
  // paper
  paper: "#fbe8c7", paperWarm: "#f6c28b", cream: "#faf6f9",
  // balloon fabric (9 gores L->R, centre red — the sheet's balloon)
  gp: "#fb9796", gP: "#f96777", gR: "#d1244b", gL: "#a92b64",
  // Charlotte and the bunny
  skin: "#f6c28b", cheek: "#f48558", hair: "#8d5726", dress: "#f974b5",
  // flame
  f1: "#ffc21d", f2: "#ff9321", f3: "#e84e1b",
  // cloud
  cl: "#d7d7fb", cs: "#b6b8fa", cw: "#faf6f9",
  // world
  leaf: "#36a565", grass: "#7cd487", moss: "#237a54",
  rock: "#5d769b", rockD: "#585687",
  gold: "#f1bf37", heart: "#f73e6a", heartOff: "#74534f",
  sand: "#d2b383", water: "#5ec6f1", foam: "#7fd7e0",
};
PAL.C = PAL.cream; // the gore table spells cream "C", same fabric

/** Paint an ASCII block. '.' transparent; every other char resolves through `map` then PAL —
 *  map values may be palette names or hexes. */
function ascii(rows, ox, oy, map) {
  const px = [];
  rows.forEach((row, dy) => {
    [...row].forEach((ch, dx) => {
      if (ch === "." || ch === " ") return;
      const v = map && map[ch] !== undefined ? map[ch] : ch;
      const hex = PAL[v] || (/^#[0-9a-f]{6}$/i.test(v) ? v : PAL[ch]);
      if (hex) px.push([ox + dx, oy + dy, hex]);
      else console.warn("unresolved colour:", JSON.stringify(ch), "in block at", ox, oy);
    });
  });
  return px;
}

// ================================================================ balloon — the hero
// 60x75 design canvas: a rounded fabric envelope, clear rigging, Charlotte and her bunny.
const CX = 60;

function envelope() {
  const px=[];
  for(let y=2;y<88;y++) {
    const t=(y-44)/42;
    const half=Math.max(2,Math.round(50*Math.sqrt(Math.max(0,1-t*t))));
    for(let x=CX-half;x<=CX+half;x++) {
      const u=(x-(CX-half))/(2*half), panel=Math.min(6,Math.floor(u*7));
      let key=["gL","gP","paper","gp","paper","gP","gL"][panel];
      if(y<17)key=u<.5?"gp":"gP";
      if(y>73)key=y<79?"gR":"gL";
      if(y>=17 && y<=73 && (u*7)%1<.045)key="gR";
      if(key==="paper" && u>.5)key="paperWarm";
      if(key==="gp" && y>53)key="gP";
      if(x<CX-18 && y>20 && y<36 && (u*7)%1>.25 && (u*7)%1<.65)key="gp";
      if(Math.abs(x-CX)>=half-1 || y===2)key="K";
      px.push([x,y,PAL[key]]);
      if(y%16===0 && y>20 && y<72 && (u*7)%1>.08 && (u*7)%1<.13)px.push([x,y,PAL.paperWarm]);
    }
  }
  return px;
}

// Small drawing helpers for the character block — circles and outlined boxes read at internal
// resolution the way the sheet's round shapes do; everything still lands on whole pixels.
function disc(cx, cy, r, fill, outline = fill) {
  const px = [];
  for (let y = Math.floor(cy - r - 2); y <= cy + r + 2; y++) {
    for (let x = Math.floor(cx - r - 2); x <= cx + r + 2; x++) {
      const d = Math.sqrt((x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2);
      if (d <= r) px.push([x, y, fill]);
      else if (d <= r + 1.15) px.push([x, y, outline]);
    }
  }
  return px;
}
function obox(x0, y0, w, h, fill, outline) {
  const px = [];
  for (let y = y0 - 1; y <= y0 + h; y++) { px.push([x0 - 1, y, outline], [x0 + w, y, outline]); }
  for (let x = x0 - 1; x <= x0 + w; x++) { px.push([x, y0 - 1, outline], [x, y0 + h, outline]); }
  for (let y = y0; y < y0 + h; y++) for (let x = x0; x < x0 + w; x++) px.push([x, y, fill]);
  return px;
}

// The dark throat the flame licks into (draw order: envelope -> throat -> flame -> rigging).
function throat() {
  const px = [];
  for (let y = 78; y <= 87; y++)
    for (let x = CX - 9; x <= CX + 8; x++) px.push([x, y, y === 78 ? PAL.K : PAL.D]);
  return px;
}

// 3-frame flicker filling the throat — f1 core, tallest in frames 2-3 (burner on).
const FLAME_FRAMES = [
  ["................", "........11......", ".......1111.....", "......112211....", ".....11222211...", "....1122112211..", "....1221111221..", "....2211111122..", ".....322111223..", "......3222223..."],
  ["................", ".........111....", ".........111....", "........1111....", ".......112211...", "......11222211..", ".....1122112211.", ".....1221111221.", "......321111123.", ".......3222223.."],
  ["..........11....", ".........1111...", "........111111..", ".......11111111.", "......1121111211", "......1211111121", ".....12113311121", ".....21133331112", "......233333332.", ".......2333332.."],
].map(rows => ascii(rows, CX - 8, 76, { "1": "f1", "2": "f2", "3": "f3" }));

// Burner, ropes, Charlotte + bunny, wicker heart-basket — painted after the flame so the rig
// sits in front of the throat. Basket x 28..91, rim y 116.
function fillPixels(x,y,w,h,color) {
  const px=[];
  for(let iy=Math.round(y);iy<Math.round(y+h);iy++)for(let ix=Math.round(x);ix<Math.round(x+w);ix++)px.push([ix,iy,color]);
  return px;
}

// Rounded face shared by the basket and portrait, with deliberately large expression clusters.
function charlotteFace(cx,cy,scale=1,mood="idle") {
  const px=[], box=(x,y,w,h,key)=>px.push(...fillPixels(cx+x*scale,cy+y*scale,w*scale,h*scale,PAL[key]));
  px.push(...disc(cx,cy,10*scale,PAL.skin,PAL.K));
  px.push(...disc(cx-9*scale,cy-6*scale,4*scale,PAL.hair,PAL.K));
  px.push(...disc(cx+8*scale,cy-7*scale,4*scale,PAL.hair,PAL.K));
  for(let y=-10;y<=-5;y++)for(let x=-8;x<=8;x++)if(x*x+y*y<112)box(x,y,1,1,"hair");
  box(-8,-5,4,3,"hair");box(3,-5,5,2,"hair");box(-10,-3,2,10,"hair");box(8,-3,2,9,"hair");
  box(-12,-8,4,3,"dress");box(-9,-6,3,3,"gP");
  if(mood==="happy") {
    for(const x of [-6,3]){box(x,-1,1,1,"K");box(x+1,-2,2,1,"K");box(x+3,-1,1,1,"K");}
    box(-2,4,5,2,"K");box(-1,6,3,1,"gP");
  } else {
    for(const x of [-6,3]){box(x,-2,3,5,"K");box(x,-2,1,2,"C");}
    if(mood==="worried"){box(-2,5,4,1,"K");box(-1,4,2,1,"K");box(-6,-4,3,1,"hair");box(3,-4,3,1,"hair");}
    else {box(-2,5,1,1,"K");box(-1,6,3,1,"K");box(2,5,1,1,"K");}
  }
  box(-8,3,3,2,"cheek");box(6,3,3,2,"cheek");
  return px;
}

function rigging(mood="idle") {
  const px=[];
  px.push(...obox(42,90,36,3,PAL.brass,PAL.K));
  px.push(...obox(49,97,22,3,PAL.w2,PAL.K));
  for(const [a,b] of [[35,29],[48,38],[72,82],[85,91]]) {
    for(let y=92;y<=130;y++) {
      const x=Math.round(a+(b-a)*(y-92)/38);
      px.push([x,y,PAL.K],[x+1,y,PAL.w0],[x+2,y,PAL.wk]);
    }
  }
  px.push(...fillPixels(39,120,23,9,PAL.dress));
  px.push(...charlotteFace(49,113,1,mood));
  // Bunny ears rise above her friend's head, with a round cream muzzle and pink lining.
  px.push(...obox(67,99,4,16,PAL.C,PAL.K),...obox(76,100,4,15,PAL.C,PAL.K));
  px.push(...fillPixels(68,102,2,9,PAL.gp),...fillPixels(77,103,2,8,PAL.gp));
  px.push(...disc(74,117,7,PAL.C,PAL.K),...disc(74,126,7,PAL.C,PAL.K));
  px.push(...fillPixels(69,115,2,3,PAL.K),...fillPixels(77,115,2,3,PAL.K));
  px.push(...fillPixels(73,119,2,2,PAL.gP));
  // Quiet wicker clusters and a thick rolled rim, instead of a field of tiny checks.
  px.push(...obox(27,127,66,20,PAL.wd,PAL.K));
  for(let y=130;y<145;y++)for(let x=29;x<92;x++) {
    const shade=x>84?"w2":(Math.floor((y-130)/3)%2===0?"wk":"wd");
    px.push([x,y,PAL[shade]]);
  }
  for(let x=31;x<92;x+=12)px.push(...fillPixels(x,130,2,15,PAL.w1));
  px.push(...obox(26,125,68,4,PAL.w0,PAL.K),...fillPixels(28,125,63,1,PAL.wk));
  px.push(...ascii([".hh.hh.","hhhhhhh","hhhhhhh",".hhhhh.","..hhh..","...h..."],57,134,{h:"gP"}));
  px.push([58,135,PAL.C],[59,135,PAL.C]);
  px.push(...disc(42,125,2,PAL.skin,PAL.K),...disc(55,125,2,PAL.skin,PAL.K));
  px.push(...disc(70,125,2,PAL.C,PAL.K),...disc(80,125,2,PAL.C,PAL.K));
  return px;
}

function balloonFrame(flameFrame,mood="idle") {
  return envelope().concat(throat(),flameFrame===0?[]:FLAME_FRAMES[flameFrame%3],rigging(mood));
}

// ================================================================ clouds
// The concept sheet's clouds: cream lobes with a flat foot and a lavender shade dithered under
// it — built from lobe circles so the silhouette is bumpy, not trapezoidal. Drawn as a height
// field: for each column the local top edge comes from the lobe arcs, and shading follows depth
// below that edge, so the rim light hugs the actual bumps.
// Overlapping circles, all fractions of w: centre lobe dominant, small lobes close the gaps so
// every column has a top edge. Foot is a shared flat line — the sheet's clouds sit flat underneath.
const LOBES = [
  { cx: 0.48, cy: 0.31, r: 0.30 },
  { cx: 0.28, cy: 0.31, r: 0.21 },
  { cx: 0.70, cy: 0.31, r: 0.23 },
  { cx: 0.10, cy: 0.31, r: 0.11 },
  { cx: 0.90, cy: 0.31, r: 0.11 },
];
function cloud(w) {
  const px=[], foot=Math.round(.31*w);
  for(let x=0;x<w;x++) {
    let top=foot;
    for(const l of LOBES) {
      const dx=(x+.5)/w-l.cx;
      if(Math.abs(dx)<l.r)top=Math.min(top,Math.round((l.cy-Math.sqrt(l.r*l.r-dx*dx))*w));
    }
    top=Math.max(1,top);
    const base=foot-Math.floor((x%37)/28);
    for(let y=top;y<=base;y++)px.push([x,y,y>base-4?PAL.cs:y>base-9?PAL.cl:PAL.cw]);
  }
  return px;
}

function sandbag() {
  const body = ascii([
    "..KKKK....",
    ".KssksK...",
    "KssksskK..",
    "KskssksK..",
    "KssksskK..",
    "KskwkwsK..",
    ".KssksK...",
    "..KKKK....",
  ], 1, 3, { s: "sand", k: "bark", w: "w2" });
  // full ink silhouette so the dropped bag reads against sky AND ground
  const seen = new Set(body.map(p => `${p[0]},${p[1]}`));
  const ring = [];
  for (const key of seen) {
    const [x, y] = key.split(",").map(Number);
    for (const [dx, dy] of [[-1, 0], [1, 0], [0, -1], [0, 1]]) {
      if (!seen.has(`${x + dx},${y + dy}`)) ring.push([x + dx, y + dy, PAL.K]);
    }
  }
  return ring.concat(body);
}

function windsock(strength, alt = false) {
  // build the sock as a region, then outline its silhouette and paint stripes — an outline drawn
  // after the stripes would eat the pattern, so the ink ring goes down first
  const cells = new Map();
  const len = 20 + strength * 10;
  const droop = (3 - strength) * 2;
  for (let i = 0; i < len; i++) {
    const along = i / len;
    const taper = Math.floor(along * (5 - strength));
    // the alt frame shivers the outer third — cloth snapping in a real wind, not a new shape
    const sag = Math.floor(along * along * droop) + (alt && along > 0.6 ? 1 : 0);
    for (let row = taper; row < 18 - taper; row++) cells.set(`${3 + i},${2 + row + sag}`, i);
  }
  const px = [];
  for (let y = 2; y < 26; y++) px.push([1, y, PAL.bark], [2, y, PAL.bark]); // post
  px.push([1, 0, PAL.gold], [2, 0, PAL.gold], [1, 1, PAL.gold], [2, 1, PAL.gold]); // finial
  for (const key of cells.keys()) {
    const [x, y] = key.split(",").map(Number);
    for (const [dx, dy] of [[0, -1], [0, 1], [-1, 0], [1, 0]]) {
      if (!cells.has(`${x + dx},${y + dy}`)) px.push([x + dx, y + dy, PAL.K]);
    }
  }
  for (const [key, i] of cells) {
    const [x, y] = key.split(",").map(Number);
    px.push([x, y, (i / 6 | 0) % 2 === 0 ? PAL.paper : PAL.gP]);
  }
  return px;
}

function star(filled) {
  return ascii([
    "....KK......",
    "...KggK.....",
    "..KggggK....",
    ".KggcgggK...",
    "KgggggggggK.",
    ".KgggggggK..",
    "..KgggggK...",
    ".KggK.KggK..",
    ".KkK...KkK..",
  ], 0, 0, { g: filled ? "gold" : "heartOff", c: "cream", k: filled ? "cream" : "heartOff" });
}

function heart(filled) {
  return ascii([
    ".KKK...KKK..",
    "KhhhK.KhhhK.",
    "KhhhhKhhhhh.",
    "Khhhhhhhhhh.",
    ".KhhhhhhhhK.",
    "..KhhhhhhK..",
    "...KhhhhK...",
    "....KhhK....",
    ".....KK.....",
  ], 0, 0, { h: filled ? "heart" : "heartOff" });
}

// ================================================================ islands
// design 120x62 -> internal 240x124: 16 px of headroom above the deck so trees and bunting stand
// ON the island surface, then bunting (delivery only), plank deck, grass cap, rock underbelly to
// a point. Launch and delivery differ in deck wood, bunting, tree and waterfall side.
function polygon(points, color, outline=true) {
  const px=[], minY=Math.min(...points.map(p=>p[1])), maxY=Math.max(...points.map(p=>p[1]));
  for(let y=minY;y<maxY;y++) {
    const xs=[];
    for(let i=0;i<points.length;i++) {
      const a=points[i],b=points[(i+1)%points.length];
      if((a[1]>y+.5)!==(b[1]>y+.5))xs.push(a[0]+(y+.5-a[1])*(b[0]-a[0])/(b[1]-a[1]));
    }
    xs.sort((a,b)=>a-b);
    for(let n=0;n<xs.length;n+=2) {
      const l=Math.ceil(xs[n]),r=Math.floor(xs[n+1]);
      for(let x=l;x<=r;x++)px.push([x,y,outline&&(x===l||x===r)?PAL.K:typeof color==="function"?color(x,y):color]);
    }
  }
  return px;
}

function island(delivery) {
  const px=[], W=240;
  const rock=[[5,40],[20,73],[43,86],[62,104],[88,110],[112,123],[137,108],[164,111],[183,91],[213,78],[235,40]];
  px.push(...polygon(rock,(x,y)=> {
    if(y<49)return PAL.moss;
    if(y>94 || x>198-y*.2)return PAL.rockD;
    const shelf=Math.floor((y+Math.floor(x/21)*3)/13)%3;
    if(x<64)return shelf===0?PAL.w1:PAL.wd;
    return shelf===0?PAL.rockD:shelf===1?PAL.rock:PAL.w2;
  }));
  // Broad lit facets and discrete cracks; no checker overlay on the underside.
  px.push(...polygon([[24,51],[51,54],[56,71],[46,74],[50,89],[36,82]],PAL.w1,false));
  px.push(...polygon([[103,54],[124,54],[130,69],[122,77],[127,97],[119,109],[110,95]],PAL.rockD,false));
  for(const [x,y]of[[45,69],[89,75],[161,66],[186,82]])px.push(...fillPixels(x,y,11,2,PAL.K),...fillPixels(x+4,y+2,2,5,PAL.K));
  // Grass shelves overhang the faceted rock, with little tufts and warm timber decking.
  px.push(...fillPixels(4,39,232,5,PAL.leaf),...fillPixels(6,39,228,2,PAL.grass));
  for(let x=11;x<231;x+=13)px.push(...fillPixels(x,43,7,3,PAL.moss));
  px.push(...obox(8,32,224,6,PAL.w1,PAL.K),...fillPixels(10,32,220,2,PAL.w0));
  for(let x=24;x<225;x+=24)px.push(...fillPixels(x,33,2,5,PAL.w2));
  for(let x=71;x<183;x+=17) {
    px.push(...fillPixels(x,29,1,4,PAL.moss),...fillPixels(x-2,30,5,2,PAL.grass));
    px.push(...disc(x,27,1.4,x%2?PAL.gp:PAL.C,PAL.leaf));
  }
  for(const [x,len]of[[28,27],[60,41],[158,23],[203,32]]) {
    for(let y=45;y<45+len;y++){px.push([x,y,PAL.moss]);if(y%6===0)px.push(...fillPixels(x-2,y,4,2,PAL.leaf));}
  }
  const shrub=(x,y,r)=> {
    px.push(...disc(x,y,r,PAL.moss,PAL.K));
    px.push(...disc(x-r*.45,y-r*.3,r*.65,PAL.leaf,PAL.leaf));
    px.push(...disc(x+r*.25,y-r*.55,r*.55,PAL.grass,PAL.grass));
  };
  if(delivery){shrub(28,23,11);shrub(49,28,7);}else{shrub(209,21,12);shrub(186,28,7);}
  for(let x=20;x<226;x+=17) {
    px.push(...fillPixels(x,40,1,5,PAL.moss));
    px.push(...disc(x,40,1.5,x%2?PAL.gp:PAL.paper,PAL.leaf));
  }
  if(delivery) {
    for(let y=47;y<118;y++) {
      const left=215-Math.floor((y-47)/10);
      px.push(...fillPixels(left,y,11,1,PAL.water));
      px.push([left+1,y,PAL.foam],[left+7,y,PAL.C]);
    }
    px.push(...disc(211,118,7,PAL.cl,PAL.cl),...disc(206,116,5,PAL.C,PAL.C));
  }
  return px;
}

function house(pink=true) {
  const px=obox(89,16,13,39,PAL.w2,PAL.K);
  px.push(...fillPixels(90,17,4,34,PAL.w1));
  px.push(...obox(23,54,80,71,PAL.paperWarm,PAL.K),...fillPixels(25,56,18,65,PAL.paper));
  px.push(...fillPixels(25,60,2,63,PAL.wd),...fillPixels(98,60,3,63,PAL.w2));
  px.push(...polygon([[8,59],[63,12],[119,59]],(x,y)=>pink?(x>79?PAL.gR:y<34?PAL.gp:PAL.gP):(x>79?PAL.w2:PAL.w1)));
  px.push(...obox(9,58,109,5,PAL.wd,PAL.K));
  for(let y=30;y<57;y+=9)px.push(...fillPixels(45-(y-30)/2,y,42+(y-30),1,pink?PAL.gR:PAL.w2));
  px.push(...disc(63,44,9,PAL.paper,PAL.K),...disc(63,44,6,PAL.cs,PAL.w2));
  px.push(...fillPixels(62,38,2,13,PAL.w2),...fillPixels(57,43,13,2,PAL.w2));
  for(const x of [31,82]) {
    px.push(...obox(x,76,16,21,PAL.f1,PAL.w2),...fillPixels(x+2,78,4,16,PAL.paper));
    px.push(...fillPixels(x+7,76,2,21,PAL.w2),...fillPixels(x,86,16,2,PAL.w2));
    px.push(...fillPixels(x-3,97,22,4,PAL.w0));
  }
  px.push(...disc(64,97,11,PAL.bark,PAL.K),...obox(53,97,22,27,PAL.bark,PAL.K));
  for(let x=56;x<75;x+=5)px.push(...fillPixels(x,98,1,25,PAL.w2));
  px.push(...disc(70,111,1.5,PAL.brass,PAL.K));
  px.push(...obox(81,103,8,12,PAL.f1,PAL.K),...fillPixels(83,105,2,8,PAL.paper));
  px.push(...fillPixels(79,101,12,2,PAL.w2));
  px.push(...obox(45,125,40,3,PAL.w0,PAL.K),...fillPixels(40,129,50,3,PAL.w2));
  px.push(...ascii([".hh.hh.","hhhhhhh","hhhhhhh",".hhhhh.","..hhh..","...h..."],60,68,{h:pink?"gP":"leaf"}));
  return px;
}

function recipient(happy=false) {
  const px=obox(7,3,5,21,PAL.C,PAL.K);
  px.push(...obox(21,2,5,22,PAL.C,PAL.K),...fillPixels(8,6,3,13,PAL.gp),...fillPixels(22,5,3,14,PAL.gp));
  px.push(...polygon([[10,32],[22,32],[29,49],[3,49]],PAL.cs));
  px.push(...disc(16,27,9,PAL.C,PAL.K));
  if(happy)for(const x of[10,20])px.push(...fillPixels(x,24,3,1,PAL.K),...fillPixels(x-1,25,1,1,PAL.K),...fillPixels(x+3,25,1,1,PAL.K));
  else for(const x of[10,21])px.push(...fillPixels(x,24,2,4,PAL.K));
  px.push(...fillPixels(15,29,3,2,PAL.gP),...fillPixels(8,29,3,1,PAL.gp),...fillPixels(23,29,3,1,PAL.gp));
  px.push(...disc(6,happy?33:40,3,PAL.C,PAL.K),...disc(26,happy?33:40,3,PAL.C,PAL.K));
  px.push(...fillPixels(7,49,6,2,PAL.C),...fillPixels(20,49,6,2,PAL.C));
  return px;
}

function teddy() {
  const px=disc(12,21,6,PAL.w0,PAL.K);
  for(const x of[5,19])px.push(...disc(x,21,3,PAL.w0,PAL.K),...disc(x,26,3,PAL.wd,PAL.K));
  for(const x of[6,18])px.push(...disc(x,5,3,PAL.w0,PAL.K),...disc(x,5,1.4,PAL.sand,PAL.wd));
  px.push(...disc(12,11,7,PAL.w0,PAL.K),...disc(12,14,3.4,PAL.sand,PAL.wd));
  px.push(...fillPixels(8,9,2,3,PAL.K),...fillPixels(15,9,2,3,PAL.K),...fillPixels(11,12,2,2,PAL.K));
  px.push(...disc(12,22,3,PAL.sand,PAL.wd));
  return px;
}
function bottle() {
  const px=obox(4,13,14,19,PAL.C,PAL.K);
  px.push(...fillPixels(6,20,10,10,PAL.paperWarm),...obox(5,8,12,5,PAL.cs,PAL.K));
  px.push(...disc(11,5,3,PAL.sand,PAL.K));
  for(let y=20;y<30;y+=4)px.push(...fillPixels(14,y,3,1,PAL.wd));
  return px;
}
function letter() {
  const px=obox(2,2,25,17,PAL.paper,PAL.K);
  for(let i=0;i<12;i++)px.push([3+i,3+Math.floor(i*.55),PAL.wd],[26-i,3+Math.floor(i*.55),PAL.wd]);
  px.push(...ascii([".hh.hh.","hhhhhhh","hhhhhhh",".hhhhh.","..hhh..","...h..."],11,9,{h:"gP"}));
  return px;
}
function perch() {
  const px=polygon([[0,12],[12,38],[43,51],[72,63],[104,47],[143,12]],(x,y)=>x+y>105?PAL.rockD:PAL.rock);
  px.push(...fillPixels(0,12,144,4,PAL.leaf),...obox(1,6,141,6,PAL.w1,PAL.K),...fillPixels(2,6,139,2,PAL.w0));
  for(let x=16;x<140;x+=20)px.push(...fillPixels(x,8,1,4,PAL.w2));
  for(let x=8;x<140;x+=17)px.push(...fillPixels(x,15,4,4,PAL.moss));
  return px;
}

function x2(px) {
  const out = [];
  for (const [x, y, hex] of px) out.push([2 * x, 2 * y, hex], [2 * x + 1, 2 * y, hex], [2 * x, 2 * y + 1, hex], [2 * x + 1, 2 * y + 1, hex]);
  return out;
}

// ================================================================ HUD bits (I6)
// The sheet's HUD is a stitched crate panel: Charlotte's portrait in a blue-backed cream frame,
// pink hearts, a gold star, a gift box counter, and a heat gauge capped with a live flame. These
// are those pieces; the panel itself is kit-drawn so it can size itself to the text it carries.

// Charlotte's face, 24x26 design. Blue backing + stitched cream frame in the sprite itself, so
// the HUD draws one thing. The face is the balloon basket's Charlotte, grown up for the frame:
// bigger glossy eyes with two shine pixels, a pink headband with a bow, shoulders in her dress.
function portrait(mood="idle") {
  const px=obox(1,1,41,45,PAL.paper,PAL.K);
  px.push(...obox(5,5,33,37,PAL.rockD,PAL.K));
  px.push(...fillPixels(10,36,25,8,PAL.dress));
  px.push(...charlotteFace(22,24,1.3,mood));
  for(let x=4;x<41;x+=5)px.push([x,3,PAL.paperWarm],[x,44,PAL.paperWarm]);
  return px;
}

// Pink HUD heart, 13x11 design: gP body, dithered gR shade along the bottom curve, two cream
// shine pixels — the sheet's hearts sit lit at the top and deep at the chin.
function heartHud(on) {
  const px = [];
  ascii([
    ".KKK..KKK..",
    "KhhhKKhhhK.",
    "KshhhhhhhhK",
    "KsChhhhhhhK",
    "KsChhhhhhhK",
    "KhhhhhhhhhK",
    ".KhhhddhhhK",
    "..KhdhdhK..",
    "...KhddhK..",
    "....KhhK...",
    ".....KK....",
  ], 0, 0, on ? { h: "gP", d: "gR", s: "gR", C: "C" } : { h: "heartOff", d: "heartOff", s: "w2", C: "w2" })
    .forEach(p => px.push(p));
  return px;
}

// Gold HUD star, 13x11 design: gold body, plum-shaded right edge, cream corner glint.
function starHud() {
  const px = [];
  ascii([
    "......KK.....",
    ".....KCCK....",
    ".....KCGK....",
    ".KKKKGGKKKKK.",
    "KGGGGGGGGGgK.",
    "KgKGGGGGgKK..",
    ".KKGGGGGKK...",
    "..KGGgGgK....",
    ".KGGKdKGGK...",
    ".KKK.KKK.....",
  ], 0, 0, { G: "brass", C: "C", g: "w2", d: "gL", K: "K" }).forEach(p => px.push(p));
  return px;
}

// Gift box, 13x13 design: gold box, cream lid, pink ribbon and bow — the sheet's "deliver to
// someone special" icon. Dots on the box are the wrapping paper.
function gift() {
  const px = [];
  ascii([
    "...KK.KK.....",
    "..KrrKrrK....",
    "..KrrKrrK....",
    "...KKKKK.....",
    "....KrK......",
    ".KKKKKKKKKKK.",
    ".KPPPPPPPPPK.",
    ".KKKKrKKKKKK.",
    ".KGGGrKGGGGK.",
    ".KGDGrKSGGDG.",
    ".KGGGrKGGGGK.",
    ".KGGGrSGGGGK.",
    ".KKKKKKKKKKK.",
  ], 0, 0, { r: "gP", P: "paper", G: "brass", D: "C", S: "gL", K: "K" }).forEach(p => px.push(p));
  return px;
}

// Heat-gauge flame cap, two frames (the gauge flickers while the burner fires), 14x14 design.
// 1 = core, 2 = body, 3 = edge (same scheme as the burner flame).
const FLAME_HUD = [
  [
    ".....1.......",
    ".....1.......",
    "....K1K..1...",
    "....K2K......",
    "...K222K.....",
    "..K21113K....",
    "..K111131K.1.",
    ".K11222131K..",
    ".K12222113K..",
    ".K11222113K..",
    ".K13113133K..",
    "..K311333K...",
    "...K3333K....",
    "....KKKKK....",
  ],
  [
    "......1......",
    "......1......",
    "..1...1......",
    ".....K2K..1..",
    ".....K2K.....",
    "....K222K....",
    "...K21113K...",
    "..K1111131K..",
    "..K1222213K..",
    "..K1122213K..",
    "...K131131K..",
    "....K3113K...",
    ".....K33K....",
    "......KK.....",
  ],
].map(rows => ascii(rows, 0, 0, { "1": "f1", "2": "f2", "3": "f3", K: "K" }));

// ================================================================ sheet packing
// frames stack vertically inside the name's column; slices.json (the loader's table) mirrors this.
// ==================================
// I9 hazard sprites: a floating sky rock, a gull in two flaps, and a storm bank. The rock and
// the bird author as 1x design ASCII (x2'd like the HUD); the storm reuses the cloud lobes at
// internal resolution so weather reads as one family with the fair-weather clouds.
// ==================================

// Floating rock, 20x14 design: grass cap over a tapered, bruised stone body. The taper is
// straight-sided (round tapers through ASCII stairs), the shading is ramp *selection*.
function skyRock() {
  return ascii([
    "....KKKKKKKKKKKK....",
    ".KggggggggggggggggK.",
    ".KggggggggggggggggK.",
    "KmggggggggggggggggmK",
    "KRGGGGGGGGGGGGGGGGRK",
    "KRddRRRRRRRRRRRRddRK",
    "KRddRRRRRRRRRRRRddRK",
    ".KRddRRRRRRRRRRddRK.",
    ".KRdddRRRRRRRRdddRK.",
    ".KRdddRRRRRRRRdddRK.",
    "..KRdddRRRRRRdddRK..",
    "..KRdddRRRRRRdddRK..",
    "...KddddRRRRddddK...",
    "....KKKKKKKKKKKK....",
  ], 0, 0, { g: "grass", G: "moss", m: "moss", R: "rock", d: "rockD" });
}

// Gull, 12x6 design in two wing phases. Body rows sit at the same heights in both frames so a
// flapping bird never jitters its own centre; the beak and eye face downwind (+X) and the
// loader flips for the way home.
function birdFrame(rows) {
  return ascii(rows, 0, 0, { w: "cw", c: "cl", f: "f2" });   // cream gull, lavender underside, orange beak
}
function birdUp() {
  return birdFrame([
    ".KK......KK.",
    "KcwK....KcwK",
    ".KwKKKKKKwK.",
    ".KwwwwwKwwKf",
    "..KwwwwwwwK.",
    "...KKKKKK...",
  ]);
}
function birdDown() {
  return birdFrame([
    "............",
    "............",
    "..KKKKKKKK..",
    ".KwwwwwKwwKf",
    "KcwKwwwwKcwK",
    "..KwKKKKKwK.",
  ]);
}

// Storm bank at internal resolution: the same cloud lobes as the fair-weather clouds, but the
// lavender body sits under a bruised underbelly, the underside is closed with ink, and a brass
// spark hangs off the middle. The two f1 eye slits are the whole "angry" trick.
function stormCloud() {
  const w = 140;
  const foot = Math.round(0.31 * w);
  const topAt = (x) => {
    const u = (x + 0.5) / w;
    let best = null;
    for (const l of LOBES) {
      const dx = u - l.cx;
      if (Math.abs(dx) >= l.r) continue;
      const yTop = (l.cy - Math.sqrt(l.r * l.r - dx * dx)) * w;
      if (best === null || yTop < best) best = yTop;
    }
    return best === null ? null : Math.max(0, Math.round(best));
  };
  const px = [];
  for (let x = 0; x < w; x++) {
    const top = topAt(x);
    if (top === null) continue;
    for (let y = top; y <= foot; y++) {
      let hex = PAL.rock;                                            // the gloom body
      if (y >= foot - 2 && (x + y) % 2 === 0) hex = PAL.rockD;     // bruised floor
      else if (y >= foot - 5 && (x + y) % 3 === 0) hex = PAL.cl;   // lit puffs in the dark
      px.push([x, y, hex]);
    }
    px.push([x, Math.max(0, top - 1), PAL.K]);                     // ink crown, like the sheet's clouds
    px.push([x, foot + 1, PAL.K]);                                 // closed, hard underside
  }
  // Eye slits and the spark — the bank's mean face, dead centre.
  for (const [ex, ey] of [[58, 0], [59, 0], [60, 0], [78, 0], [79, 0], [80, 0]])
    px.push([ex, 18 + ey, PAL.f1]);
  const bolt = [[0, 0], [1, 0], [2, 0], [-1, 1], [0, 1], [1, 1], [0, 2], [1, 2], [2, 2], [1, 3], [0, 4], [1, 4]];
  for (const [bx, by] of bolt) px.push([69 + bx, foot + 3 + by, PAL.f1]);
  return px;
}

function farIsle() {
  const px=[];
  const remap = c => c===PAL.K ? PAL.rockD : [PAL.w0,PAL.w1,PAL.w2,PAL.wd,PAL.bark].includes(c) ? PAL.rock : c===PAL.leaf||c===PAL.moss ? PAL.foam : c===PAL.gold ? PAL.paper : c;
  for(const [x,y,c] of island(true))px.push([Math.floor(x*.6),70+Math.floor(y*.6),remap(c)]);
  for(const [x,y,c] of house(false))px.push([44+Math.floor(x*.5),35+Math.floor(y*.5),remap(c)]);
  return px;
}

function titleLogo() {
  const glyph={A:".###.|#...#|#...#|#####|#...#|#...#|#...#",B:"####.|#...#|#...#|####.|#...#|#...#|####.",C:".###.|#...#|#....|#....|#....|#...#|.###.",H:"#...#|#...#|#...#|#####|#...#|#...#|#...#",I:"#####|..#..|..#..|..#..|..#..|..#..|#####",L:"#....|#....|#....|#....|#....|#....|#####",N:"#...#|##..#|#.#.#|#..##|#...#|#...#|#...#",O:".###.|#...#|#...#|#...#|#...#|#...#|.###.",R:"####.|#...#|#...#|####.|#.#..|#..#.|#...#",S:".####|#....|#....|.###.|....#|....#|####.",T:"#####|..#..|..#..|..#..|..#..|..#..|..#..",E:"#####|#....|#....|####.|#....|#....|#####","'":"..#..|..#..|.....|.....|.....|.....|....."};
  const px=[];
  const paint=(x,y,w,h,c)=>{const out=[];for(let yy=y;yy<y+h;yy++)for(let xx=x;xx<x+w;xx++)out.push([xx,yy,c]);return out;};
  function text(word,y,scale,pink) {
    const mask=[]; const ox=Math.floor((700-(word.length*6-1)*scale)/2);
    [...word].forEach((c,i)=>glyph[c].split('|').forEach((row,yy)=>[...row].forEach((v,xx)=>{if(v==='#')mask.push([ox+(i*6+xx)*scale,y+yy*scale]);})));
    for(const [radius,color] of [[pink?9:4,PAL.K],[pink?5:1,PAL.paper]])
      for(const [x,y] of mask)px.push(...paint(x-radius,y-radius,scale+radius*2,scale+radius*2,color));
    for(const [x,yy] of mask)px.push(...paint(x,yy,scale,scale,pink?(yy-y>scale*3?PAL.gP:PAL.gp):PAL.paper));
    if(pink)for(const [x,yy] of mask)px.push(...paint(x,yy,scale,2,PAL.gp));
  }
  text("CHARLOTTE'S",12,4,false);text("BALLOONATIC",76,9,true);
  return px;
}

function toyBlocks() {
  const px=[];
  const cells=[[12,98,"gP","A"],[44,98,"cs","B"],[76,98,"f1","C"],[28,66,"foam","B"],[60,66,"gp","A"],[44,34,"gold","C"]];
  const letters={A:[".###.","#...#","#####","#...#","#...#"],B:["####.","#...#","####.","#...#","####."],C:[".####","#....","#....","#....",".####"]};
  for(const [x,y,color,letter] of cells) {
    px.push(...obox(x,y,29,29,PAL[color],PAL.K),...fillPixels(x+2,y+2,25,2,PAL.paper),...fillPixels(x+25,y+3,3,24,PAL.wd));
    for(const [xx,yy,c] of ascii(letters[letter],0,0,{"#":"K"}))px.push(...fillPixels(x+6+xx*3,y+6+yy*3,3,3,c));
  }
  for(const [x,y,c] of teddy())px.push([90+x,68+y,c]);
  return px;
}

function gardenMill(frame) {
  const px=polygon([[42,128],[52,58],[82,58],[94,128]],(x,y)=>x>73?PAL.wd:PAL.paperWarm);
  px.push(...polygon([[39,65],[67,38],[96,65]],(x,y)=>x>68?PAL.rockD:PAL.rock));
  px.push(...obox(60,102,17,25,PAL.bark,PAL.K),...obox(57,79,20,14,PAL.f1,PAL.w2));
  px.push(...fillPixels(65,79,2,14,PAL.w2),...fillPixels(57,85,20,2,PAL.w2));
  for(let blade=0;blade<4;blade++) {
    const a=Math.PI/4+blade*Math.PI/2+frame*.2,dx=Math.cos(a),dy=Math.sin(a),nx=-dy,ny=dx;
    const pt=(r,w)=>[Math.round(67+dx*r+nx*w),Math.round(67+dy*r+ny*w)];
    px.push(...polygon([pt(7,-3),pt(51,-6),pt(51,6),pt(7,3)],PAL.paperWarm));
    for(let r=12;r<=48;r+=9)px.push(...polygon([pt(r,-5),pt(r+2,-5),pt(r+2,5),pt(r,5)],PAL.bark));
  }
  px.push(...disc(67,67,7,PAL.brass,PAL.K),...disc(67,67,3,PAL.wd,PAL.bark));
  for(const x of[9,22,106,117]) {
    px.push(...fillPixels(x,116,2,12,PAL.moss),...disc(x,114,4,PAL.gp,PAL.gP),...disc(x,114,1.5,PAL.f1));
  }
  return px;
}
function gardenPerch() {
  const px=perch();
  for(let x=8;x<140;x+=15) {
    px.push(...fillPixels(x,4,2,10,PAL.moss),...disc(x,4,3,PAL.dress,PAL.gP),...disc(x,4,1,PAL.gold));
  }
  return px;
}

const SPRITES = {
  "garden-mill": {w:128,h:134,frames:[gardenMill(0),gardenMill(1)]},
  "garden-perch": {w:144,h:64,frames:[gardenPerch()]},
  "toy-blocks": {w:128,h:134,frames:[toyBlocks()]},
  "title-logo": {w:700,h:176,frames:[titleLogo()]},
  "far-isle": {w:144,h:150,frames:[farIsle()]},
  "nursery-house": {w:128,h:134,frames:[house(true)]},
  "cottage": {w:128,h:134,frames:[house(false)]},
  "recipient": {w:32,h:52,frames:[recipient(false),recipient(true)]},
  "parcel-teddy": {w:24,h:31,frames:[teddy()]},
  "parcel-bottle": {w:22,h:34,frames:[bottle()]},
  "parcel-letter": {w:30,h:22,frames:[letter()]},
  "island-perch": {w:144,h:64,frames:[perch()]},
  "balloon":      { w: 120, h: 150, frames: [0, 1, 2].map(f => balloonFrame(f)) },
  "balloon-happy": { w: 120, h: 150, frames: [0,1,2].map(f=>balloonFrame(f,"happy")) },
  "balloon-worried": { w: 120, h: 150, frames: [0,1,2].map(f=>balloonFrame(f,"worried")) },
  "bag-falling":  { w: 12, h: 14, frames: [sandbag()] },
  "cloud-small":  { w: 140, h: 48, frames: [cloud(140)] },
  "cloud-mid":    { w: 200, h: 66, frames: [cloud(200)] },
  "cloud-large":  { w: 280, h: 92, frames: [cloud(280)] },
  "windsock-1":   { w: 56, h: 26, frames: [windsock(1)] },
  "windsock-2":   { w: 66, h: 26, frames: [windsock(2), windsock(2, true)] },
  "windsock-3":   { w: 76, h: 26, frames: [windsock(3), windsock(3, true)] },
  "island-launch":    { w: 240, h: 124, frames: [island(false)] },
  "island-delivery":  { w: 240, h: 124, frames: [island(true)] },
  "star-off":     { w: 12, h: 9, frames: [star(false)] },
  "star-on":      { w: 12, h: 9, frames: [star(true)] },
  "heart-off":    { w: 12, h: 9, frames: [heart(false)] },
  "heart-on":     { w: 12, h: 9, frames: [heart(true)] },
  "portrait":     { w: 44, h: 48, frames: [portrait()] },
  "portrait-happy": { w:44,h:48,frames:[portrait("happy")] },
  "portrait-worried": { w:44,h:48,frames:[portrait("worried")] },
  "heart-hud":    { w: 26, h: 22, frames: [x2(heartHud(true))] },
  "heart-hud-off": { w: 26, h: 22, frames: [x2(heartHud(false))] },
  "rock":         { w: 40, h: 28, frames: [x2(skyRock())] },
  "bird":         { w: 24, h: 12, frames: [x2(birdUp()), x2(birdDown())] },
  "storm-cloud":  { w: 140, h: 52, frames: [stormCloud()] },
  "star-hud":     { w: 26, h: 22, frames: [x2(starHud())] },
  "gift":         { w: 26, h: 26, frames: [x2(gift())] },
  "flame-hud":    { w: 28, h: 28, frames: FLAME_HUD.map(x2) },
};

function pack(sheetWidth) {
  const out = [];
  let x = 4, y = 4, rowH = 0;
  for (const [name, spec] of Object.entries(SPRITES)) {
    const h = spec.h * spec.frames.length;
    if (x + spec.w + 4 > sheetWidth) { x = 4; y += rowH + 4; rowH = 0; }
    out.push({ name, x, y, w: spec.w, h: spec.h, frames: spec.frames });
    x += spec.w + 4;
    rowH = Math.max(rowH, h);
  }
  return { rects: out, width: sheetWidth, height: y + rowH + 4 };
}

module.exports = { PAL, SPRITES, pack };
if (typeof require !== "undefined" && typeof module !== "undefined" && require.main === module) {
  const p = pack(768);
  console.log(JSON.stringify({ width: p.width, height: p.height, sprites: p.rects.map(r => ({ name: r.name, x: r.x, y: r.y, w: r.w, h: r.h, frames: r.frames.length })) }, null, 1));
}


