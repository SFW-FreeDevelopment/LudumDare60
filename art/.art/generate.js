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
  rock: "#8799a5", rockD: "#585687",
  gold: "#f1bf37", heart: "#f73e6a", heartOff: "#74534f",
  sand: "#d2b383",
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
// design 34x39 -> internal 68x78. The onion profile is half-widths in design px per design row,
// sampled at internal resolution so the silhouette is a smooth curve instead of the old kit's
// 11-row staircase. Gore count follows the concept sheet (nine, centre red).
const PROFILE = [2, 4, 6, 8, 10, 11, 12, 13, 14, 15, 15, 16, 16, 16, 15, 14, 12, 10, 7, 5];
const GORES = ["gp", "C", "gP", "C", "gR", "C", "gP", "C", "gp"];
// fabric ramps: palette-only light -> shade -> deep as the sphere turns away from the sun
const GORE_RAMP = {
  gp: ["gp", "gP", "gR"],
  C: ["C", "paperWarm", "paperWarm"],
  gP: ["gP", "gR", "gL"],
  gR: ["gR", "gL", "gL"],
};
const GORE_LIGHT = { gp: "C", C: "C", gP: "gp", gR: "C" }; // crown rows read one step lighter
const CX = 34; // envelope centre column, internal

function envelope() {
  const px = [];
  const halfAt = (y) => {
    const r = y / 2;
    const i = Math.min(PROFILE.length - 1, Math.floor(r));
    const j = Math.min(PROFILE.length - 1, i + 1);
    const t = r - i;
    return 2 * (PROFILE[i] * (1 - t) + PROFILE[j] * t);
  };
  for (let y = 0; y < 40; y++) {
    const half = halfAt(y);
    const left = Math.round(CX - half);
    const right = Math.round(CX + half);
    const w = right - left;
    if (w < 2) continue;
    const crown = y < 6;
    const gathered = y >= 34; // below here the fabric gathers to the load ring — it sits in shade
    for (let x = left; x < right; x++) {
      const u = (x - left) / (w - 1);
      if (x - left < 2 || right - x <= 2) { px.push([x, y, PAL.K]); continue; } // outline
      const gi = Math.min(8, Math.floor(u * 9));
      const gore = GORES[gi];
      let key;
      if (gathered) key = GORE_RAMP[gore][1];
      else if (crown) key = GORE_LIGHT[gore];
      else if (u > 0.955) key = GORE_RAMP[gore][2];
      else if (u > 0.86) key = GORE_RAMP[gore][1];
      else key = gore;
      px.push([x, y, PAL[key]]);
    }
  }
  return px;
}

// The dark throat the flame licks into, painted over the gathered neck (draw order: envelope ->
// throat -> flame -> rigging). The burner hangs below it; the flame must read INSIDE the mouth.
function throat() {
  const px = [];
  for (let y = 34; y <= 39; y++) {
    for (let x = CX - 5; x <= CX + 4; x++) px.push([x, y, y === 34 ? PAL.K : PAL.D]);
  }
  return px;
}

// 3-frame flicker filling the throat, f1 core rising tallest in frame 2 (burner on).
const FLAME_FRAMES = [
  ["............", ".....11.....", "....1111....", "...122221...", "..12211221..", "..22111122..", "..32211223..", "...322223..."],
  ["............", "......11....", "......11....", ".....1111...", "....112211..", "...12211221.", "...22111122.", "....322223.."],
  ["............", "......11....", ".....1111...", "....111111..", "...11211211.", "...21111112.", "...23111132.", "....233332.."],
].map(rows => ascii(rows, CX - 6, 32, { "1": "f1", "2": "f2", "3": "f3" }));

// Burner, ropes, Charlotte with her bunny, wicker basket — painted after the flame so the rig
// sits in front of the throat. Centre CX=34; basket spans x 21..47.
function rigging() {
  const px = [];
  // brass load ring closing the neck, with a dark beat for relief
  for (let x = CX - 10; x <= CX + 9; x++) px.push([x, 39, PAL.brass], [x, 40, (x & 1) ? PAL.w2 : PAL.w1]);
  // burner: two brass posts and a bar, hanging from the ring
  for (let y = 41; y <= 44; y++) { px.push([CX - 5, y, PAL.brass], [CX + 4, y, PAL.brass]); }
  for (let x = CX - 6; x <= CX + 5; x++) px.push([x, 45, PAL.K]);
  for (let x = CX - 5; x <= CX + 4; x++) px.push([x, 45, ((x & 1) ? PAL.brass : PAL.w2)]);
  for (let x = CX - 5; x <= CX + 4; x++) px.push([x, 46, PAL.K]);
  // ropes: near-vertical from ring ends down to the basket rim, 2 px wide, rope colour with an
  // ink beat every 4 px so they read as twisted fibre, not slats
  for (let i = 0; i <= 22; i++) {
    const y = 41 + i;
    const lx = 25 - Math.round(i / 12);        // 25 -> 23, onto the rim's left corner
    const rx = 44 - Math.round((22 - i) / 12); // 42 -> 44, onto the rim's right corner
    const beat = i % 6 === 3;                   // a sparse twist beat — rope, not ladder rail
    px.push([lx, y, beat ? PAL.w2 : PAL.w0], [lx + 1, y, beat ? PAL.w1 : PAL.w0]);
    px.push([rx, y, beat ? PAL.w1 : PAL.w0], [rx + 1, y, beat ? PAL.w2 : PAL.w0]);
  }
  // Charlotte: hair cap, face, eyes, cheeks, dress body sinking into the basket
  ascii([
    "..hhhhhh..",
    ".hhhhhhhh.",
    ".hssssssh.",
    ".ssKssKss.",
    ".ssssssss.",
    ".sKssssKs.",
    "..ssssss..",
  ], 26, 53, { h: "hair", s: "skin" }).forEach(p => px.push(p));
  px.push([27, 57, PAL.cheek], [33, 57, PAL.cheek]); // cheeks
  ascii([
    ".dddddddd.",
    ".dddddddd.",
    ".dddddddd.",
  ], 26, 60, { d: "dress" }).forEach(p => px.push(p));
  // the bunny: cream, ears joined to the head, riding her right
  ascii([
    ".c....c.",
    ".c....c.",
    ".cccccc.",
    ".ccKccc.",
    ".cccccc.",
    ".c.cc.c.",
  ], 38, 54, { c: "cream" }).forEach(p => px.push(p));
  // wicker basket: ink-outlined rim, brick-pattern weave (rows offset by 2 px), shaded right face
  const L = 21, W = 27, T = 63, B = 76;
  for (let y = T; y < B; y++) { px.push([L, y, PAL.K], [L + W - 1, y, PAL.K]); }
  for (let x = L; x < L + W; x++) { px.push([x, T, PAL.K], [x, B - 1, PAL.K]); }
  for (let x = L + 1; x < L + W - 1; x++) { px.push([x, T + 1, (x & 1) ? PAL.w0 : PAL.w1]); } // lit rim
  for (let y = T + 2; y < B - 1; y++) {
    const offset = (y % 4 < 2) ? 0 : 2;                       // brick shift between rows
    for (let x = L + 1; x < L + W - 1; x++) {
      const cell = (((x - L + offset) >> 1) % 2) === 0;
      let hex = cell ? PAL.wk : PAL.wd;
      if ((x - offset) % 6 === 0) hex = PAL.bark;             // vertical stakes under the weave
      if (x >= L + W - 4) hex = PAL.wd;                        // shaded right face
      px.push([x, y, hex]);
    }
  }
  px.push([L + 3, T + 1, PAL.paper], [L + 4, T + 1, PAL.paper]); // a paper tag on the rim
  return px;
}

function balloonFrame(flameFrame) {
  return envelope().concat(throat(), FLAME_FRAMES[flameFrame % 3], rigging());
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
      let hex = PAL.cw;                                            // cream body
      if (y >= foot - 2 && (x + y) % 2 === 0) hex = PAL.cs;        // dithered lavender foot
      else if (y >= foot - 5 && (x + y) % 3 === 0) hex = PAL.cl;   // first soft row above it
      px.push([x, y, hex]);
    }
  }
  // ink outline along the bumpy top edge only — clouds read soft against the sky
  for (let x = 0; x < w; x++) {
    const top = topAt(x);
    if (top !== null) px.push([x, Math.max(0, top - 1), PAL.K]);
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

function windsock(strength) {
  // build the sock as a region, then outline its silhouette and paint stripes — an outline drawn
  // after the stripes would eat the pattern, so the ink ring goes down first
  const cells = new Map();
  const len = 20 + strength * 10;
  const droop = (3 - strength) * 2;
  for (let i = 0; i < len; i++) {
    const along = i / len;
    const taper = Math.floor(along * (5 - strength));
    const sag = Math.floor(along * along * droop);
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

// ================================================================ sheet packing
// frames stack vertically inside the name's column; slices.json (the loader's table) mirrors this.
const SPRITES = {
  "balloon":      { w: 68, h: 78, frames: [0, 1, 2].map(f => balloonFrame(f)) },
  "bag-falling":  { w: 12, h: 14, frames: [sandbag()] },
  "cloud-small":  { w: 140, h: 48, frames: [cloud(140)] },
  "cloud-mid":    { w: 200, h: 66, frames: [cloud(200)] },
  "cloud-large":  { w: 280, h: 92, frames: [cloud(280)] },
  "windsock-1":   { w: 56, h: 26, frames: [windsock(1)] },
  "windsock-2":   { w: 66, h: 26, frames: [windsock(2)] },
  "windsock-3":   { w: 76, h: 26, frames: [windsock(3)] },
  "star-off":     { w: 12, h: 9, frames: [star(false)] },
  "star-on":      { w: 12, h: 9, frames: [star(true)] },
  "heart-off":    { w: 12, h: 9, frames: [heart(false)] },
  "heart-on":     { w: 12, h: 9, frames: [heart(true)] },
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
