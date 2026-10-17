/*
 * Art pipeline audit (art-plan step B gate): decodes art/export/sheet.png from raw PNG (no
 * dependencies — inflate + unfilter) and fails if any opaque pixel is off-palette or a slice
 * rect falls outside the sheet. Run: node art/.art/audit.js
 */
"use strict";
const fs = require("fs");
const path = require("path");
const zlib = require("zlib");
const { PAL } = require("./generate.js");

const png = fs.readFileSync(path.join(__dirname, "..", "export", "sheet.png"));
// walk chunks
let off = 8, width = 0, height = 0, idat = [];
while (off < png.length) {
  const len = png.readUInt32BE(off);
  const type = png.toString("ascii", off + 4, off + 8);
  if (type === "IHDR") { width = png.readUInt32BE(off + 8); height = png.readUInt32BE(off + 12); }
  if (type === "IDAT") idat.push(png.subarray(off + 8, off + 8 + len));
  off += 12 + len;
}
const raw = zlib.inflateSync(Buffer.concat(idat));
const bpp = 4, stride = width * bpp;
const img = Buffer.alloc(height * stride);
for (let y = 0; y < height; y++) {
  const filter = raw[y * (stride + 1)];
  const line = raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1));
  for (let x = 0; x < stride; x++) {
    const a = x >= bpp ? img[y * stride + x - bpp] : 0;          // left
    const b = y > 0 ? img[(y - 1) * stride + x] : 0;             // up
    const c = x >= bpp && y > 0 ? img[(y - 1) * stride + x - bpp] : 0; // up-left
    let v = line[x];
    if (filter === 1) v += a;
    else if (filter === 2) v += b;
    else if (filter === 3) v += (a + b) >> 1;
    else if (filter === 4) { const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
    img[y * stride + x] = v & 0xff;
  }
}

const allowed = new Set(Object.values(PAL).map(h => h.toLowerCase()));
const bad = new Map();
for (let i = 0; i < img.length; i += bpp) {
  if (img[i + 3] === 0) continue;
  const hex = `#${img[i].toString(16).padStart(2, "0")}${img[i + 1].toString(16).padStart(2, "0")}${img[i + 2].toString(16).padStart(2, "0")}`;
  if (!allowed.has(hex)) bad.set(hex, (bad.get(hex) || 0) + 1);
}
const slices = JSON.parse(fs.readFileSync(path.join(__dirname, "..", "export", "slices.json"), "utf8"));
const rectFail = slices.filter(s => s.x < 0 || s.y < 0 || s.x + s.w > width || s.y + s.h > height);

if (bad.size || rectFail.length) {
  console.error("AUDIT FAIL — off-palette:", [...bad.entries()], "rects outside:", rectFail);
  process.exit(1);
}
console.log(`audit ok: ${width}x${height}, ${slices.length} slices in bounds, all opaque pixels on-palette`);
