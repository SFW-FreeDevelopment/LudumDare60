#!/usr/bin/env node
// Charlotte's Balloonatic — audio, generated the same philosophy as the sprites: one committed
// script, every sample computed, WAVs byte-stable, `node audio/.audio/generate.js` rebuilds all
// of it. The style is deliberately old-tech chiptune (square/triangle/noise, 11.025 kHz mono) —
// the theme carried by the instrument, not a filter, and light enough for the WebGL payload.
//
// PLAN I11 named ElevenLabs as the source; this machine has no ElevenLabs access, so the clips
// ship procedural-first — see assets/audio/SOURCES.md. Replacing a clip with an ElevenLabs
// render later is a file drop at the same name, nothing in the game changes.
'use strict';
const fs = require('fs');
const path = require('path');

const RATE = 11025;              // telephony-era rate: half the payload, and it *is* the aesthetic
const OUT = path.join(__dirname, '..', 'export');

// ---- wav writing ---------------------------------------------------------------------------
function wav(samples) {
  const n = samples.length;
  const buf = Buffer.alloc(44 + n * 2);
  buf.write('RIFF', 0); buf.writeUInt32LE(36 + n * 2, 4); buf.write('WAVE', 8);
  buf.write('fmt ', 12); buf.writeUInt32LE(16, 16); buf.writeUInt16LE(1, 20); buf.writeUInt16LE(1, 22);
  buf.writeUInt32LE(RATE, 24); buf.writeUInt32LE(RATE * 2, 28); buf.writeUInt16LE(2, 32); buf.writeUInt16LE(16, 34);
  buf.write('data', 36); buf.writeUInt32LE(n * 2, 40);
  for (let i = 0; i < n; i++) {
    let s = Math.max(-1, Math.min(1, samples[i]));
    buf.writeInt16LE((s * 32767) | 0, 44 + i * 2);
  }
  return buf;
}

// ---- voices (each returns one sample of a held note at phase t seconds, freq f) ------------
const square = (f, t, duty = 0.5) => (t * f % 1 < duty ? 1 : -1);
const tri = (f, t) => 4 * Math.abs((t * f % 1) - 0.5) - 1;
const sine = (f, t) => Math.sin(2 * Math.PI * f * t);
// deterministic noise for the burner and the sand thump (LCG — no Math.random, byte-stable output)
let lcg = 12345;
const noise = () => ((lcg = (lcg * 1103515245 + 12345) & 0x7fffffff) / 0x3fffffff) - 1;

const envelope = (t, dur, a, d) => {       // attack a s, decay-to-end over the remaining d window
  if (t < a) return t / a;
  const x = (t - a) / Math.max(1e-6, dur - a);
  return Math.pow(1 - x, d);
};

function render(durSec, fn) {
  const n = Math.floor(durSec * RATE);
  const out = new Float64Array(n);
  for (let i = 0; i < n; i++) out[i] = fn(i / RATE);
  return out;
}

// ---- the kit -------------------------------------------------------------------------------
// Burner: a bandpassed roar — noise smoothed three ways, breathing at ~2 Hz. Seam-loopable: the
// breath completes a whole cycle inside the clip.
const burner = () => render(1.0, (t) => {
  const breath = 0.55 + 0.45 * (0.5 + 0.5 * sine(1.0, t));      // one breath per second = seamless
  let s = 0;
  for (const dt of [0, -1, -2, -3]) {                          // crude lowpass: moving average
    const k = Math.max(0, Math.min(0x7fffffff, (Math.floor(t * RATE) + dt)));
    s += (((k * 1103515245 + 12345) & 0x7fffffff) / 0x3fffffff) - 1;
  }
  s /= 4;
  const body = Math.tanh(s * 2.2);                              // soft-clip warmth
  return body * 0.32 * breath;
});

// Basket creak: wood under load — a low sine bent downward through a wobble, creaked by AM.
const creak = () => render(0.5, (t) => {
  const f = 120 - 60 * (t / 0.5);
  const wobble = 1 + 0.25 * sine(17, t);
  const creakAm = Math.pow(0.5 + 0.5 * sine(23, t), 3);
  return tri(f, t) * creakAm * envelope(t, 0.5, 0.02, 1.6) * 0.5;
});

// Star chime: a bell third — sine + its octave, fast shimmer, long tail. One per star.
const chime = () => render(0.7, (t) => {
  const e = envelope(t, 0.7, 0.004, 2.4);
  return (sine(1318, t) + 0.5 * sine(2637, t) + 0.15 * sine(988, t)) / 1.65 * e * 0.5;
});

// Giggle: Charlotte laughs it off — three short "he-he-he" formant pips, upward at the end.
const giggle = () => {
  lcg = 999;                                                    // reset so re-runs match byte-for-byte
  return render(0.55, (t) => {
    const beats = [0.0, 0.11, 0.22, 0.33];
    let s = 0;
    beats.forEach((b, i) => {
      const lt = t - b;
      if (lt < 0 || lt > 0.09) return;
      const f = 520 + i * 70 + 140 * (lt / 0.09);               // the upward flick is the "he"
      const gl = tri(f, lt) * 0.6 + sine(f * 2, lt) * 0.4;
      s += gl * envelope(lt, 0.09, 0.01, 1.8) * (i === 3 ? 0.8 : 1);
    });
    return s * 0.36;
  });
};

// Delivery jingle: five notes of toy-piano fanfare (triangle = the old-tech music box).
const jingle = () => {
  const seq = [[523, 0.14], [659, 0.14], [784, 0.14], [1046, 0.34], [784, 0.3]];
  return render(1.06, (t) => {
    let s = 0, at = 0;
    for (const [f, d] of seq) {
      const lt = t - at;
      if (lt >= 0 && lt < d) s += tri(f, lt) * 0.55 * envelope(lt, d, 0.008, 1.4) + sine(f * 2, lt) * 0.12;
      at += d;
    }
    return s * 0.55;
  });
};

// Title music: four bars of a square lead over a triangle bass, toy-piano tempo, seamless loop.
const music = () => {
  const BPM = 112, beat = 60 / BPM;
  const lead = [ // [midi, beats] — a small pentatonic thing that hums while the burner breathes
    [76, .5], [79, .5], [81, 1], [79, .5], [76, .5], [74, 1], [72, 1],
    [76, .5], [79, .5], [81, 1], [84, 1], [81, .5], [79, .5], [76, 2],
    [74, .5], [76, .5], [79, 1], [76, .5], [74, .5], [72, 1], [69, 1],
    [72, 1], [76, 1], [79, 1], [72, 2]];
  const bass = [45, 45, 52, 45, 43, 43, 50, 43, 45, 45, 52, 45, 48, 48, 43, 43]; // one per beat
  const total = lead.reduce((a, [, b]) => a + b, 0);
  const dur = Math.max(total, bass.length) * beat;
  return render(dur, (t) => {
    let s = 0, at = 0;
    for (const [m, b] of lead) {
      const d = b * beat, lt = t - at;
      if (lt >= 0 && lt < d * 0.92) {
        const f = 440 * Math.pow(2, (m - 69) / 12);
        s += square(f, lt, 0.5) * 0.30 * envelope(lt, d * 0.92, 0.01, 1.2);
      }
      at += d;
    }
    const bi = Math.floor(t / beat) % bass.length;
    const lt = t % beat;
    const bf = 440 * Math.pow(2, (bass[bi] - 69) / 12);
    s += tri(bf, lt) * 0.26 * envelope(lt, beat, 0.01, 2.0);
    return s;
  });
};

// Sand thump: the ballast hitting the deck — filtered noise burst, 80 ms.
const thump = () => {
  lcg = 777;
  return render(0.08, (t) => {
    let s = 0;
    for (const dt of [0, -1, -2]) {
      const k = Math.max(0, (Math.floor(t * RATE) + dt));
      s += (((k * 1103515245 + 12345) & 0x7fffffff) / 0x3fffffff) - 1;
    }
    return Math.tanh(s * 0.8) * envelope(t, 0.08, 0.002, 3.0) * 0.6;
  });
};

fs.mkdirSync(OUT, { recursive: true });
const kit = { 'burner': burner(), 'creak': creak(), 'chime': chime(), 'giggle': giggle(),
  'jingle': jingle(), 'music': music(), 'thump': thump() };
let total = 0;
for (const [name, samples] of Object.entries(kit)) {
  const bytes = wav(samples);
  fs.writeFileSync(path.join(OUT, `${name}.wav`), bytes);
  total += bytes.length;
  console.log(`${name}.wav`.padEnd(14), (bytes.length / 1024).toFixed(1).padStart(7), 'KB',
    `(${(samples.length / RATE).toFixed(2)} s)`);
}
console.log('total', (total / 1024).toFixed(1), 'KB');
