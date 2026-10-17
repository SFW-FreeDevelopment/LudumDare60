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
// design 60x75 -> internal 120x150, the sheet's proportions: the balloon is the biggest thing in
// frame after the islands, a round onion in a PINK/CREAM CHECKERBOARD (the sheet's pattern — not
// stripes), a deep-pink crown, a red skirt band, and Charlotte + the bunny riding an oversize
// heart basket. Half-widths are design px per design row, sampled at internal resolution.
const PROFILE = [2, 8, 14, 20, 24, 27, 29, 30, 30, 30, 30, 29, 27, 25, 22, 19, 15, 11, 7, 3];
const ENVELOPE_H = 88;                       // internal rows 0..87
const CROWN_H = 14;                          // deep pink cap
const SKIRT_TOP = 74;                        // red gather band where the fabric meets the ring
const CX = 60;                               // envelope centre column, internal

// checkerboard gore: eight panels of 2x2-cell checks, alternating cream and pink, gore seams in
// deep pink; sphere shading darkens the whole pattern toward the right, the crown reads flat deep
// pink, and the skirt band closes the mouth in red. Ramps only move within the palette.
function envelope() {
  const px = [];
  // profile rows are design half-widths; resample them smoothly across the full internal height
  const halfAt = (y) => {
    const r = (y / ENVELOPE_H) * (PROFILE.length - 1);
    const i = Math.min(PROFILE.length - 1, Math.floor(r));
    const j = Math.min(PROFILE.length - 1, i + 1);
    const t = r - i;
    return 2 * (PROFILE[i] * (1 - t) + PROFILE[j] * t);
  };
  for (let y = 0; y < ENVELOPE_H; y++) {
    const half = halfAt(y);
    const left = Math.round(CX - half);
    const right = Math.round(CX + half);
    const w = right - left;
    if (w < 2) continue;
    const skirt = y >= SKIRT_TOP;
    for (let x = left; x < right; x++) {
      const u = (x - left) / (w - 1);
      if (x - left < 2 || right - x <= 2) { px.push([x, y, PAL.K]); continue; }
      if (skirt) {
        px.push([x, y, u > 0.78 ? PAL.gL : (y - SKIRT_TOP) % 4 < 2 ? PAL.gR : PAL.gP]);
        continue;
      }
      if (y < CROWN_H) { px.push([x, y, u > 0.82 ? PAL.gL : PAL.gP]); continue; } // deep pink cap
      // three check cells per gore, alternating every 5 rows — the sheet's square pattern
      const seam = (u * 8) % 1;
      const cell = (Math.floor(seam * 3) + Math.floor((y - CROWN_H) / 5)) % 2;
      let key = cell === 0 ? "C" : "gp";
      if (seam < 0.06 || seam > 0.94) key = "gP";                 // gore seam line
      if (u > 0.9) key = key === "C" ? "paperWarm" : "gL";        // deep rim: warm, then magenta
      else if (u > 0.78) key = key === "C" ? "C" : "gP";           // checks stay crisp, pinks deepen
      else if (u < 0.12 && y < 30 && ((x + y) & 1) === 0) key = "cw"; // dithered sun glare
      px.push([x, y, PAL[key]]);
    }
  }
  return px;
}

// Small drawing helpers for the character block — circles and outlined boxes read at internal
// resolution the way the sheet's round shapes do; everything still lands on whole pixels.
function disc(cx, cy, r, fill, outline) {
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
function rigging() {
  const px = [];
  // brass load ring + cable
  for (let x = CX - 26; x <= CX + 25; x++) {
    px.push([x, 88, PAL.brass], [x, 89, (x & 1) ? PAL.w2 : PAL.w1]);
  }
  // burner cage: posts from the ring to a brass bar under the mouth
  for (let y = 90; y <= 97; y++) { px.push([CX - 11, y, PAL.w2], [CX - 10, y, PAL.brass], [CX + 9, y, PAL.w2], [CX + 10, y, PAL.brass]); }
  px.push(...obox(CX - 12, 98, 24, 3, PAL.brass, PAL.K));
  for (let x = CX - 10; x <= CX + 9; x += 4) px.push([x, 99, PAL.w2]); // gas jets
  // ropes: two per side from ring to basket corners, lit core + shadow column, sparse twist beat
  const rope = (x0, y0, x1, y1) => {
    const steps = y1 - y0;
    for (let i = 0; i <= steps; i++) {
      const x = Math.round(x0 + (x1 - x0) * (i / steps));
      const beat = i % 7 === 4;
      px.push([x, y0 + i, beat ? PAL.w2 : PAL.w0], [x + 1, y0 + i, beat ? PAL.w1 : PAL.w1]);
    }
  };
  rope(CX - 26, 90, 28, 124);
  rope(CX - 12, 90, 40, 124);
  rope(CX + 11, 90, 79, 124);
  rope(CX + 25, 90, 90, 124);
  // ---- Charlotte: ONE chibi head, clearly below the burner and sitting inside the basket.
  // The earlier pass stacked buns above the hairline so the hair read as a second head — buns
  // now sit at the temples (space-buns off the sheet), the face is a compact block, and only
  // her head and a sliver of dress rise above the basket rim.
  px.push(...disc(41, 107, 3.5, PAL.hair, PAL.K), ...disc(55, 107, 3.5, PAL.hair, PAL.K));          // temple buns
  px.push([40, 103, PAL.gp], [41, 103, PAL.gp], [55, 103, PAL.gp], [56, 103, PAL.gp]);              // pink beads on the buns
  px.push(...obox(42, 104, 14, 13, PAL.skin, PAL.K));                                               // face
  for (let x = 42; x <= 55; x++) for (let y = 104; y <= 106; y++) px.push([x, y, PAL.hair]);        // hair cap
  for (let x = 43; x <= 55; x += 3) px.push([x, 107, PAL.hair]);                                    // bangs zigzag
  for (let y = 106; y <= 119; y++) {                                                                // long side hair to the rim
    px.push([40, y, PAL.hair], [41, y, PAL.hair], [56, y, PAL.hair], [57, y, PAL.hair]);
    px.push([39, y, PAL.K], [58, y, PAL.K]);
  }
  // big glossy eyes: 3x4 ink with a cream shine pixel, the sheet's signature
  ascii(["KKK", "KCK", "KKK", ".KK."], 44, 110, { K: "K", C: "C" }).forEach(p => px.push(p));
  ascii(["KKK", "KCK", "KKK", ".KK."], 50, 110, { K: "K", C: "C" }).forEach(p => px.push(p));
  px.push([42, 115, PAL.cheek], [43, 115, PAL.cheek], [54, 115, PAL.cheek], [55, 115, PAL.cheek]);  // blush
  px.push([47, 116, PAL.K], [48, 116, PAL.K], [47, 117, PAL.gP], [48, 117, PAL.gP]);                // smile
  for (let x = 44; x <= 53; x++) for (let y = 119; y <= 122; y++) px.push([x, y, PAL.dress]);       // dress above the rim
  px.push([43, 119, PAL.K], [43, 120, PAL.K], [43, 121, PAL.K], [54, 119, PAL.K], [54, 120, PAL.K], [54, 121, PAL.K]);
  // ---- the bunny: clearly NOT a second girl — smaller than Charlotte, ears far taller than
  // his head, a 2 px ink eye and a pink nose in profile so the silhouette is unmistakable.
  px.push(...obox(65, 105, 3, 8, PAL.cream, PAL.K), ...obox(71, 105, 3, 8, PAL.cream, PAL.K));      // tall ears
  px.push([66, 107, PAL.gp], [66, 108, PAL.gp], [66, 109, PAL.gp], [72, 107, PAL.gp], [72, 108, PAL.gp], [72, 109, PAL.gp]); // inner ears
  px.push(...disc(69, 116, 4.2, PAL.cream, PAL.K));                                                 // small head
  px.push([66, 115, PAL.K], [66, 116, PAL.K]);                                                      // eye, facing Charlotte
  px.push([64, 117, PAL.gp]);                                                                       // nose at the profile edge
  for (let x = 65; x <= 73; x++) for (let y = 120; y <= 122; y++) px.push([x, y, PAL.cream]);       // body into the rim
  // ---- basket: oversize wicker with the sheet's heart emblem. Weave reads as horizontal
  // wicker rows (2 px lit / 2 px dark bands) with stakes over them, not a checkerboard.
  const L = 28, W = 64, T = 124, B = 148;
  px.push(...obox(L, T, W, B - T, PAL.wd, PAL.K));
  for (let x = L + 1; x < L + W - 1; x++) { px.push([x, T + 1, (x & 1) ? PAL.w0 : PAL.w1]); px.push([x, T + 2, (x & 1) ? PAL.w1 : PAL.w2]); } // rope rim
  for (let y = T + 3; y < B - 4; y++) {
    const lit = ((y - T) >> 1) % 2 === 1;                                                           // 2-row wicker bands
    for (let x = L + 1; x < L + W - 1; x++) {
      let hex = lit ? PAL.wk : PAL.wd;
      if (((x - L) % 14) === 0) hex = PAL.bark;                                                     // stakes over the weave
      if (x >= L + W - 6) hex = PAL.w2;                                                             // shaded face
      if (y >= B - 8 && (x + y) % 2 === 0) hex = PAL.w2;                                            // foot dither
      px.push([x, y, hex]);
    }
  }
  // the heart on the basket, straight off the sheet's hero basket
  ascii([
    "..KKK..KKK..",
    ".KhhhKKhhhK.",
    "KhhhhhhhhhhK",
    "KcchhhhhhhhK",
    "KcchhhhhhhhK",
    ".KhhhhhhhhK.",
    "..KhhhhhhK..",
    "...KhhhhK...",
    "....KhhK....",
    ".....KK.....",
  ], CX - 6, 129, { h: "gP", c: "C" }).forEach(p => px.push(p));
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
function island(delivery) {
  const W = 240, px = [];
  const deckTop = 32, deckH = 8;                      // 24 rows of headroom: trees stand ON the island
  const capTop = 40, pointY = 124;                    // grass cap starts, rock tapers to here
  const deckWood = delivery ? "w0" : "w1";
  // bunting: gold pips on a line across the delivery deck (the prize reads at a glance)
  if (delivery) {
    for (let x = 30; x < W - 30; x += 18) px.push([x, deckTop - 4, PAL.gold], [x + 1, deckTop - 4, PAL.gold], [x, deckTop - 3, PAL.gold], [x + 1, deckTop - 3, PAL.gold]);
    for (let x = 12; x < W - 12; x++) px.push([x, deckTop - 5, PAL.K]); // the string, post to post
  }
  // deck: lit top line, plank seams every 16 px, shaded bottom edge. Width matches the sim's
  // landing tolerance exactly: DeckHalfWidth 56 design px = 112 internal, centred.
  for (let x = W / 2 - 112; x < W / 2 + 112; x++) {
    px.push([x, deckTop, PAL.K], [x, deckTop + deckH + 1, PAL.K]);
    for (let y = deckTop + 1; y <= deckTop + deckH; y++) px.push([x, y, PAL[deckWood]]);
    if ((x - (W / 2 - 112)) % 16 === 0) for (let y = deckTop + 1; y <= deckTop + deckH; y++) px.push([x, y, PAL.w2]);
    px.push([x, deckTop + 1, (x & 1) ? PAL.w0 : PAL[deckWood]]);
  }
  // ---- the island body, in the concept sheet's language: an overhanging grass lip with flowers
  // and hanging vines, warm brown strata above a violet dithered underside, tree clusters on the
  // flanks, and a waterfall down the delivery island. Half-widths keep the old smooth taper.
  const halfAt = (y) => {
    const t = (y - capTop) / (pointY - capTop);       // 0 at cap, 1 at point
    const w = 224 * (1 - t * t * t);                  // cap slightly wider than the deck, long taper
    return Math.max(0, w / 2);
  };
  const LIP = capTop + 8;                             // lip depth, then bare rock
  for (let y = capTop; y < pointY; y++) {
    const overhang = y < LIP ? 6 : 0;                 // the lip juts out past the rock, like the sheet
    const half = halfAt(Math.max(y, y < LIP ? capTop + 4 : LIP)) + overhang;
    const left = Math.round(W / 2 - half), right = Math.round(W / 2 + half);
    for (let x = left; x < right; x++) {
      const edge = x - left < 2 || right - x <= 2;
      if (edge) { px.push([x, y, PAL.K]); continue; }
      let hex;
      if (y < LIP) {                                  // grass lip: lit top, leaf body, moss fringe
        hex = y < capTop + 4 ? PAL.grass : y < LIP - 1 ? PAL.leaf : PAL.moss;
        if (y === LIP - 1 && (x & 1) === 0) hex = PAL.leaf;  // dithered fringe into the rock
      } else {
        const u = (x - left) / Math.max(1, right - left - 1);
        const yUnder = 92 - Math.round(u * 12);       // the shadow line rides higher up the right face
        if (y >= yUnder) {                            // violet underside, gathering to ink at the tip
          const deep = (y - yUnder) / 34;
          hex = ((x + y) & 1) ? PAL.rockD : PAL.rock;
          if (deep > 0.45 && (((x >> 1) + y) & 1) === 0) hex = PAL.K;
          if (u > 0.8) hex = ((x + y) & 1) ? PAL.K : PAL.rockD;
        } else {                                      // warm strata, sun from the left
          const band = Math.floor(y / 9) % 2;
          hex = band ? PAL.w2 : PAL.bark;
          if (u < 0.3 || (u < 0.36 && ((x + y) & 1) === 0)) hex = band ? PAL.bark : PAL.w1;
          if (u > 0.84) hex = (y & 1) ? PAL.K : PAL.w2;
          if ((x * 31 + y * 17) % 89 === 0) hex = PAL.K; // sparse cracks
        }
      }
      px.push([x, y, hex]);
    }
  }
  // flowers along the first grass band visible below the deck lip, cycling pink/gold/cream
  for (let x = 36; x < W - 36; x += 16) {
    const bloom = [PAL.gp, PAL.gold, PAL.C][((x / 16) | 0) % 3];
    px.push([x, capTop + 2, bloom], [x + 1, capTop + 2, bloom], [x, capTop + 3, PAL.leaf]);
  }
  // hanging vines off the lip edge — 1 px strands, leaf tip, lengths that vary along the rim
  [[40, 14], [76, 22], [164, 17], [200, 10]].forEach(([vx, len]) => {
    for (let i = 0; i < len; i++) px.push([vx, LIP + i, i === len - 1 ? PAL.leaf : (i & 1) ? PAL.moss : PAL.leaf]);
    px.push([vx + 1, LIP + len - 2, PAL.moss]);
  });
  // tree clusters standing on the island surface beside the deck — the sheet's trees sit on top,
  // not on the flank. Canopies clear the deck line; trunks run down behind the planks.
  const tree = (cx, cy, r) => {
    px.push(...obox(cx - 2, cy + r - 4, 5, r + 6, PAL.bark, PAL.K));                // trunk
    px.push(...disc(cx, cy, r + 1.15, PAL.K, PAL.K), ...disc(cx, cy, r, PAL.leaf, PAL.K));
    px.push(...disc(cx - Math.round(r * 0.3), cy - Math.round(r * 0.35), r * 0.55, PAL.grass, PAL.grass));
    px.push(...disc(cx + Math.round(r * 0.28), cy + Math.round(r * 0.34), r * 0.5, PAL.moss, PAL.moss));
  };
  if (delivery) { tree(30, 20, 12); tree(54, 28, 8); }
  else { tree(208, 20, 12); tree(184, 28, 8); }
  // waterfall down the delivery island's right flank, following the rock face inward as it
  // narrows, with a dithered splash foot where it leaves the island
  if (delivery) {
    let foot = 0;
    for (let y = deckTop + 10; y < pointY - 10; y++) {
      const right = Math.round(W / 2 + halfAt(y));
      for (let x = right - 14; x <= right - 3; x++) px.push([x, y, (x & 1) ? PAL.cl : PAL.cw]);
      px.push([right - 15, y, PAL.K]);
      foot = right;
    }
    for (let x = foot - 18; x <= foot; x++) {
      const s = 1 - Math.abs(x - (foot - 9)) / 10;
      for (let y = pointY - 10; y < pointY; y++) if (((x + y) & 1) === 0 && s > (y - (pointY - 10)) / 10) px.push([x, y, PAL.C]);
    }
  }
  return px;
}

// Double a 1x-authored block to internal resolution — every pixel becomes a crisp 2x2.
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
function portrait() {
  const px = [];
  px.push(...obox(0, 0, 44, 48, PAL.paper, PAL.K));                                  // cream frame
  for (let y = 3; y < 45; y += 3) for (let x = 3; x < 41; x += 3) px.push([x, y, PAL.paperWarm]); // stitching
  px.push(...obox(5, 5, 34, 38, PAL.rockD, PAL.K));                                  // violet-blue backing
  px.push(...disc(22, 16, 11, PAL.hair, PAL.K));                                     // hair mass
  px.push(...obox(11, 15, 22, 20, PAL.skin, PAL.K));                                 // face
  for (let x = 12; x <= 31; x++) for (let y = 15; y <= 17; y++) px.push([x, y, PAL.hair]); // fringe
  for (let x = 12; x <= 31; x += 3) px.push([x, 18, PAL.hair]);                      // fringe zigzag
  for (let y = 18; y <= 35; y++) { px.push([10, y, PAL.hair], [11, y, PAL.hair], [32, y, PAL.hair], [33, y, PAL.hair]); } // side hair
  for (let x = 12; x <= 31; x++) px.push([x, 6, PAL.gP], [x, 7, PAL.gP]);            // headband
  ascii([
    "KK..KK",
    "KggggK",
    "KggggK",
    ".KggK.",
    "..KK..",
  ], 27, 6, { g: "gP" }).forEach(p => px.push(p));                                   // bow pinned to the band
  // big glossy eyes: 4x6 ink, cream shine at the top, a second speck low — the sheet's gloss
  ascii(["KKKK", "KCKK", "KKKK", "KKKK", "KKCK", ".KK."], 14, 22, { K: "K", C: "C" }).forEach(p => px.push(p));
  ascii(["KKKK", "KCKK", "KKKK", "KKKK", "KKCK", ".KK."], 25, 22, { K: "K", C: "C" }).forEach(p => px.push(p));
  px.push([12, 29, PAL.cheek], [13, 29, PAL.cheek], [30, 29, PAL.cheek], [31, 29, PAL.cheek]); // blush
  px.push([20, 31, PAL.K], [21, 31, PAL.K], [22, 31, PAL.K], [23, 31, PAL.K]);       // smile line
  px.push([20, 32, PAL.gP], [21, 32, PAL.gP], [22, 32, PAL.gP], [23, 32, PAL.gP]);   // open smile
  for (let x = 14; x <= 29; x++) for (let y = 38; y <= 43; y++) px.push([x, y, PAL.dress]);     // shoulders
  for (let x = 16; x <= 27; x += 4) px.push([x, 38, PAL.gp]);                        // collar trim
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
const SPRITES = {
  "balloon":      { w: 120, h: 150, frames: [0, 1, 2].map(f => balloonFrame(f)) },
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
  "heart-hud":    { w: 26, h: 22, frames: [x2(heartHud(true))] },
  "heart-hud-off": { w: 26, h: 22, frames: [x2(heartHud(false))] },
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
