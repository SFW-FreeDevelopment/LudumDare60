# WebGL release and itch.io page

This corrected build was prepared October 18, 2026 and published on itch.io under I36. The Ludum Dare
entry has not been updated. Steven approved initial flight feel with **"Seems to feel fine at
first play"** and audio with **"Sounds are good"**. His follow-up **"Jitter is fixed"** confirms I32.
The unclear Breeze Perch prompted I35: a wider rest deck, stable parking and explicit takeoff guidance.

## Artifact and source

- Archive: `dist/charlottes-balloonatic-ld60-web.zip`, **15,701,197 bytes (14.97 MiB)**.
- SHA-256: `5359177AB2D24AA059754BBB6FD0FA3C7A99DBE0AF0129506390B7DF6A42AD27`.
- `index.html` is at the archive root. Serve over HTTP; file URLs cannot boot WASM.
- Runtime and generated assets: `566e482` (I35), including the I31–I33 playtest corrections.
- Review tag: `ld60-review-v3`, including this verification record and the refreshed entry copy.
  Earlier `ld60-review-v2` and `ld60-submit` tags are preserved.

The credits menu and entry copy have no AI or sound-provider credits. World signs have baked lettering that
moves with their boards. Basket-foot contacts support deck edges, and a landed basket can slide into
the destination's recipient area. A gentle unheated perch landing parks the basket; holding Space
warms the burner and lifts off. Internal source/provenance records remain with the assets.

## Verification

`npm run check` passes **182 unit tests and 124 Playwright tests**, with zero compiler warnings or
errors across the pure core, shared game, desktop and WebGL heads. Browser assertions use the real
`window.__GAME` bridge. Screenshots are separately reviewed at 1280×720 and 420×760.

An isolated GitHub clone under `_scratch/review-v3-clean` passed `npm ci`, art/audio re-export and the
full check at the final runtime commit. All **18 regenerated exports** match the working candidate
byte for byte: atlas/slices, boot styling/images/labels, and eight mastered WAVs. Art re-export uses
the Aseprite MCP; audio mastering uses archived source renders. Normal builds require neither
authoring tool.

The root publish started with the previous `export/web` moved aside. The new ZIP was extracted into
`_scratch/zip-v3-final` and tested over compressed and raw HTTP, rather than from the development tree.
The compressed archive transferred **4,360,495 bytes (4.16 MiB)** over 82 requests. Local first-frame
readiness was **732 / 780 ms** (desktop / narrow). With music, burner and breeze actively playing,
average rAF cadence was **16.666 / 16.667 ms**, worst 16.8 ms. Both runs had zero console/JS errors.
These localhost measurements are smoke checks, not an internet-loading or device-performance promise.

Normal-collision test-pilot deliveries completed in **38.08 / 49.83 / 68.92 seconds**. Nursery and Toy
Isles retained three hearts; Cloud Garden retained two. All earned three stars, reached the 3/3 voyage
map and dedication, replayed a fresh flight, and restored saved progress after reload. These are legal
scripted flights, rather than estimates of a new player's completion time.

Eight additional browser checks against the compressed ZIP verify the credits, sign attachments,
all deck edges/sliding deliveries, and parked-perch takeoff at both sizes. Two raw-HTTP boot checks
also pass: **10.10 MiB** transferred, **682 / 665 ms** locally, with no errors. Compression improves
transfer size but is not required for the archive to boot.

## Visual judgment

Reviewed package title, flight, completed voyage and dedication at both sizes: warm pink/cream craft,
Charlotte and bunny, visible cargo, distinct destinations and a blue cloud sea. Essential type fits,
focus rings are visible, and shared panels and actions are unclipped. The game's small-sprite art is
simpler than the supplied concept illustration. Perch screenshots show the basket on the widened
timber, readable rest/takeoff instructions, and the craft rising clear after heating.

Evidence: `docs/screenshots/34-package-{title,flight,voyage,ending}-*.png` and
`35-perch-{rest,takeoff}-*.png`. The `31-credits-*`, `32-signs-phase*-*` and `33-*` images retain the
individual playtest-correction evidence. The refreshed desktop preview is open for further play.

## Remaining submission work

- Public game: https://stevengarberg.itch.io/charlottes-balloonatic — free browser play, published in
  Steven Garberg's verified Chrome account. Project ID `5179249`; uploaded WebGL build `19840176`.
- The cover, title banner, cloud/heart background and scenic launch panel come from `npm run itch:art`.
  Four PNG exports reproduce byte for byte through Aseprite MCP; sources are in `art/itch/src/`.
  The page uses cream/lavender/plum colors, Lora type, four real gameplay images and clear controls.
- `npm run verify:itch -- https://stevengarberg.itch.io/charlottes-balloonatic` uses fresh anonymous
  browser contexts. Desktop and mobile-browser views load the public page and actual iframe, accept
  real Enter/Space input, complete all three normal-collision routes, park on the perch and verify
  clean credits. Both runs have zero browser errors or failed requests. Desktop evidence is 1280×720;
  mobile evidence is 420×760 at DPR 1. Mobile retains itch.io's compatibility warning because the game
  requires keyboard/mouse; touch support is not advertised. Reviewed `36-itch-{page,copy,flight}-*`.
- Page copy has no tool/provider branding. itch.io's mandatory generation disclosure is completed
  accurately; the asset source records remain in the repository.
- Original I13 still needs the Ludum Dare page, playable URL and final entry submission. Steven
  explicitly deferred that workflow until after itch.io setup; no Ludum Dare edits were made.

## Repeat the artifact check

```sh
npm ci
npm run check
npm run zip
# Extract the ZIP to a new folder, then serve that folder with tools/serve.mjs (ROOT and PORT).
npm run verify:web -- http://localhost:4193
```

`verify:web` checks real input/audio, a complete collision-enabled voyage, dedication/replay,
saved progress after reload, console errors and active-play frame cadence in both viewport sizes.
