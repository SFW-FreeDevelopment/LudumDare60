/*
 * Pack step: turns generate.js's sprite table into the three artefacts downstream steps read —
 * pixels.json (render.js -> draft PNG), .art/slices.json (working rects) and the committed
 * art/export/slices.json (the loader's slice table, mirrored into Aseprite). Frame naming: a
 * sprite with >1 frame becomes `name-0..n`; single frames keep the bare name.
 * Run: node art/.art/pack.js && node art/.art/render.js
 */
"use strict";
const fs = require("fs");
const path = require("path");
const { pack } = require("./generate.js");

const p = pack(768);
fs.writeFileSync(path.join(__dirname, "pixels.json"), JSON.stringify({ width: p.width, height: p.height, rects: p.rects }));

const slices = [];
for (const r of p.rects) {
  const n = r.frames.length;
  for (let f = 0; f < n; f++) slices.push({ name: n > 1 ? `${r.name}-${f}` : r.name, x: r.x, y: r.y + f * r.h, w: r.w, h: r.h });
}
fs.writeFileSync(path.join(__dirname, "slices.json"), JSON.stringify(slices));
fs.writeFileSync(path.join(__dirname, "..", "export", "slices.json"), "[\n" + slices.map(s => JSON.stringify(s)).join(",\n") + "\n]\n");
console.log(`packed ${slices.length} slices -> pixels.json/slices.json, sheet ${p.width}x${p.height}`);
