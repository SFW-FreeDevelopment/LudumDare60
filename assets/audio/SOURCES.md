# Audio provenance — Charlotte's Balloonatic (LD60)

All clips are **generated, not sampled**: `node audio/.audio/generate.js` synthesizes every
sample from the committed script (`audio/.audio/generate.js`) and writes the WAVs to
`audio/export/` (16-bit PCM, 11.025 kHz mono). Rebuilding is one command; nothing depends on a
file outside the repo. The output is byte-stable — the generator uses no randomness beyond its
own seeded counters.

Style choice: **old-tech chiptune voices** (square, triangle, seeded noise) at a telephony-era
sample rate — the jam theme carried by the instruments themselves, and light enough (~330 KB raw
for the whole kit) for the WebGL payload.

| Clip | What it is | Voice recipe |
| --- | --- | --- |
| `music.wav` | title/voyage loop, 11.25 s seamless | square lead over triangle bass, 112 bpm pentatonic |
| `burner.wav` | burner roar, 1 s seamless loop | seeded noise, 4-tap smoothing, soft-clip, one breath per second |
| `creak.wav` | basket creak (bounce/landing) | bent triangle through cubic AM |
| `chime.wav` | star chime (×3 after a delivery) | bell thirds — sine + octave, fast shimmer |
| `giggle.wav` | Charlotte laughs off a bump (heart lost) | four short upward formant pips |
| `jingle.wav` | delivery fanfare | five-note toy-piano triangle melody |
| `thump.wav` | sandbag hitting the deck | filtered noise burst, 80 ms |

**ElevenLabs note (PLAN I11 decision #4):** this development machine has no ElevenLabs access,
so the kit ships procedural-first. Any clip can be replaced with an ElevenLabs render by dropping
the file at the same name under `audio/export/` — the game loads by name and nothing changes. If
that swap happens before submission, this table must name the clip's generation prompt instead.
