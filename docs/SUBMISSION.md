# WebGL review candidate

This is the improved local review build, prepared October 17, 2026. Public hosting and the jam-entry
upload have not been performed. Steven's initial flight verdict is **"Seems to feel fine at first play"**;
he delegated the remaining visual/reference judgment to the agent. On October 18, 2026, he completed
the listening check with **"Sounds are good"**, closing I29 and original I11 after his earlier muted playtest.

## Artifact and source

- Archive: `dist/charlottes-balloonatic-ld60-web.zip`, **15,675,240 bytes (14.95 MiB)**.
- SHA-256: `55F360B30D29616000B60B39F39A600E543324D4387F351C4F6FBAFBB179B422`.
- `index.html` is at the archive root. Serve over HTTP; file URLs cannot boot WASM.
- Runtime and generated assets: `88d6ee2` (I30b), including all I16–I30a changes.
- Review tag: `ld60-review-v2`, including the final verification record. The original `ld60-submit`
  tag is preserved and identifies the earlier scaffold build; it is not this improved candidate.
- Listening approval was recorded after the review tag. This documentation sign-off does not change
  the tagged runtime, audio assets or archive checksum.

## Verification

Release builds of the pure core, shared game, desktop and WebGL heads have zero compiler warnings
and errors. `npm run check` passes **83 unit tests and 118 Playwright tests** at 1280×720 and 420×760.
The browser assertions use `window.__GAME`; screenshots provide separate visual evidence.

A fresh clone from GitHub main, isolated under `_scratch/ship-final`, ran `npm ci`, `npm run art`,
`npm run audio` and the full check. Eighteen regenerated exports match the working candidate byte for
byte: atlas/slices, boot palette and images/labels, and all eight mastered WAVs. Art re-export uses
the Aseprite MCP; audio mastering uses the archived ElevenLabs source renders without another
provider request. Normal builds require neither authoring tool.

The final root publish began with the previous `export/web` moved out of the way. The ZIP was made
from this new output and extracted into `_scratch/zip-final`. Compressed and raw HTTP tests serve
that extracted directory rather than the development tree.

The compressed archive transferred **4,351,507 bytes (4.15 MiB)** over 82 requests. Local first-frame
readiness was 631 ms / 599 ms (desktop / narrow); timing is logged, never asserted as an internet
loading guarantee. With music, burner and breeze actively playing, average rAF cadence was
**16.667 / 16.666 ms**, worst 16.8 ms. Both viewport runs had zero console/JS errors.

Normal-collision deliveries completed in 36.92 s / 49.83 s / 68.92 s. Nursery and Toy Isles retained
three hearts; Cloud Garden retained two. All earned three stars, reached the honest 3/3 voyage map,
opened the dedication, replayed a fresh flight and restored saved progress after reload. These are
legal test-pilot runs, rather than a claim about a new player's completion times. Review map images
were selected from the settled captures of these successful archive runs, without altering pixels.

The raw HTTP archive boot also passed both viewport bridge checks: 10.10 MiB transferred, 595 / 582 ms
locally, no errors. Brotli-aware hosting reduces transfer size but is not required for the game to boot.

## Visual judgment

Reviewed loading/title, flight, voyage map and ending at both required sizes. The rounded pink/cream
craft, Charlotte and bunny, visible cargo, distinct nursery/toy/windmill destinations and cloud sea
carry the reference's main ideas. The clearer cargo objective, wind ladder and larger shared type
remain legible at the narrow viewport. Actions and dedication fit their panels; no default template
loading text remains. The game uses a simpler small-sprite treatment than the concept illustration.

Evidence: `docs/screenshots/30-branded-{boot,ready}-*.png` and
`docs/screenshots/30-package-{title,flight,voyage,ending}-*.png`. Earlier initiative screenshots show
the detailed route, HUD, recipient, settings, focus and motion checks.

## Remaining human/submission work

- Choose the public browser-game host, upload the archive and add its playable URL to the jam entry.
  Review copy and generated-content credits are in `docs/LD_ENTRY.md`.
- Original I13 remains the final public submission gate. This review package does not claim an upload.

## Repeat the artifact check

```sh
npm ci
npm run check
npm run zip
# Extract the ZIP to a new folder, then serve that folder with tools/serve.mjs (ROOT and PORT).
npm run verify:web -- http://localhost:4187
```

`verify:web` checks real input/audio, a complete collision-enabled voyage, the dedication/replay,
saved progress after reload, console errors and active-play frame cadence in both viewport sizes.
These localhost cadence measurements are smoke checks, not a performance guarantee for every device.
