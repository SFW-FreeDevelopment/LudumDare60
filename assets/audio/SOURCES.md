# Audio provenance — Charlotte's Balloonatic

The current music and all seven effects were generated with **ElevenLabs** through its installed
MCP on October 17, 2026 (October 18 UTC). Prompts, requested durations, loop flags, provider output
filenames and reported model names are committed in [requests.json](requests.json). Original MP3
renders are in `assets/audio/source/`; runtime WAVs are in `audio/export/`.

| File | Character and purpose | Source |
| --- | --- | --- |
| music | Gentle music-box / celesta lullaby, ~36 s | Music v2, instrumental prompt |
| burner | Restrained hot-air burner whoosh | SFX endpoint, loop requested |
| wind | Quiet open-sky breeze | SFX endpoint, loop requested |
| creak | Small wicker/rope flex on bounce or damage | SFX endpoint |
| chime | Delicate ascending parcel/rating bell | SFX endpoint |
| giggle | Generated fictional character laugh after delivery | SFX endpoint |
| jingle | Short warm delivery melody | SFX endpoint |
| thump | Cloth sandbag rustle and soft plop | SFX endpoint |

The SFX endpoint did not report its model name or generation IDs. The archived provider filenames
and SHA-256 hashes identify the exact source renders used. An initial Music v2 request combining a
prompt and seed was rejected without producing audio; the successful request used the prompt alone.

Rebuild every runtime clip offline with **`npm run audio`**, requiring Node and `ffmpeg` on PATH
(or the `FFMPEG` environment variable). The committed generator delegates to `tools/master-audio.cjs`.
It decodes to mono 22.05 kHz, removes DC, softens high frequencies, closes loop joins with short
crossfades, applies short event envelopes and caps per-clip peaks. `audio/export/analysis.json`
records output durations, measured RMS/peaks and source hashes. Rebuilding does not call the provider.
The old procedural chip-wave kit is superseded.

Music has its own 0–10 bus. Burner, breeze and events share the independent SFX bus. Pause stops
ambience/music; completion stops flight ambience. Damage uses a wicker creak and Charlotte's concerned
expression. Delivery schedules the jingle, a happy giggle and only the earned number of star chimes;
leaving the celebration clears delayed cues. Browser tests verify routing and playback health.
**Steven's listening review remains required** for the final sound and loop/mix judgment.
