# Charlotte's Balloonatic

Play in your browser: **https://stevengarberg.itch.io/charlottes-balloonatic**

A small sky adventure for Charlotte, built for Ludum Dare 60's **Old Tech** theme. Fly a wicker
hot-air balloon with Charlotte and her bunny: hold the burner to rise, release early to cool and
settle, and choose your altitude to ride the wind. Deliver a bottle to the nursery, reunite a lost
teddy with the Toy Isles, and carry a letter to the Cloud Garden windmill.

The deliverable is **WebGL / Blazor WebAssembly**, built with KNI. The Windows desktop head is for
judging flight feel and sound. Three short courses lead through a moonlit voyage map to a dedication.

## Run

Requirements: .NET SDK 8+ (verified here with 10.0.101), Node 20+, and Playwright Chromium.

```sh
npm ci
npx playwright install chromium
npm run dev      # desktop playtest
npm run web      # publish to export/web/
npm run serve    # http://localhost:4173
npm test         # Chromium, desktop and narrow viewports
npm run check    # all heads + unit tests + WebGL publish + browser suite
npm run zip      # archive from the published WebGL output
```

The checked-in PNGs and WAVs build without art/audio tools. To regenerate them:

```sh
npm run art      # installed Aseprite MCP; see art/README.md
npm run audio    # archived ElevenLabs renders + ffmpeg; no provider request
```

## Play

| Input | Action |
| --- | --- |
| Hold Space / left mouse | Heat the balloon and rise |
| Release | Cool gradually and descend; let go before the landing |
| B / right mouse | Drop one of two sandbags for extra lift and gentler descent |
| Arrows / Enter | Select and activate menu actions; Left/Right adjust settings |
| Enter / result button | Open the voyage chart after a delivery; retry after lost hearts |
| R | Restart the current flight; after delivery, open the voyage chart |
| Escape / pause button | Pause; resume deliberately after focus loss |

The wind ladder previews neighboring bands. Chevrons show direction and strength; **altitude is the
steering**. Land gently on the clear timber deck; the basket can overhang its edge and drift along
the planks into the recipient area. Heating lifts off again. A missed course returns Charlotte safely for another
approach, preserving cargo and spent bags. All ballast loadouts can descend and land.

Nursery Skies teaches burner/cooling and a breeze perch. Toy Isles needs a climb out of homeward air.
Approach a breeze perch from above and release heat to land. It parks the basket for a rest and a
look at the wind ladder; hold Space to warm the burner and lift off again. Destination decks allow sliding.
Cloud Garden adds a marked gust bank and resting flower perches. Wind Assist removes gust buffeting;
Reduced Motion stills decorative frames, drift, shake and transitions. Music and effects have separate
0–10 controls. Play requires a keyboard or mouse; the narrow layout is verified at 420×760.

Best delivery ratings and settings are remembered in browser storage; storage denial falls back to
session play. Desktop records last for the session. Start Adventure and the ending's Fly Again launch
a fresh first flight while preserving best ratings; Choose Sky launches any course directly.

## Theme and credits

Old Tech comes from the burner, thermometer, rope, wicker, sandbags and windsocks. The subject carries
the theme in clean pixel art, with a pink rounded balloon and floating dream islands.

- Game and direction: Steven Garberg / Gigabite Games. *For Charlotte ♥*
- Engine: KNI 4.3, C#, Blazor WebAssembly and WebGL.
- Pixel art: committed generator scripts through Aseprite MCP; source and palette-audited exports in
  `art/`. See [art/README.md](art/README.md).
- Music and all seven effects: **ElevenLabs**, archived source renders/prompts and offline mastering.
  See [assets/audio/SOURCES.md](assets/audio/SOURCES.md).
- Pixel font: the game's custom 5×7 face. Code assistance included Qwen and OpenAI Codex;
  the entry's generated-art/audio workflow is disclosed in the provenance files.

## Review and submission

`dist/charlottes-balloonatic-ld60-web.zip` contains the published `wwwroot`, with `index.html` at its
root. Serve over HTTP. [docs/LD_ENTRY.md](docs/LD_ENTRY.md) has entry copy and
[docs/SUBMISSION.md](docs/SUBMISSION.md) records candidate verification and remaining human checks.
The archive is prepared for review; hosting and the public jam entry are Steven's final submission steps.

[PLAN.md](PLAN.md) tracks the improvement work. [docs/ASSESSMENT.md](docs/ASSESSMENT.md) explains the
reference comparison; [docs/STYLE.md](docs/STYLE.md) locks the implemented visual language;
[docs/BASELINE.md](docs/BASELINE.md) records the bridge and verification history. Reviewed evidence
lives in [docs/screenshots/](docs/screenshots/).
