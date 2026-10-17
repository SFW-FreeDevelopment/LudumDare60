# Build baseline and verification harness

Numbers are recorded so later initiatives have something to compare against instead of a feeling.
Measured by the Playwright suite (`npx playwright test`), which serves the published WebGL build
exactly the way an entry host would.

## WebGL build â€” I0 baseline (2026-10-17)

| Metric | Value | How it is measured |
| --- | --- | --- |
| Cold boot to first drawn frame | **~470â€“510 ms** | `window.__GAME.readyAtMs`, page start â†’ first `Draw` |
| Transferred payload | **2.83 MB over 76 requests** | `performance.getEntriesByType('resource')` transfer sizes, brotli-served |
| Published payload on disk | 17.18 MB across 204 files (59 `.br` sidecars, 2.81 MB) | `export/web/` after `dotnet publish` |
| `dotnet build` (5 projects, Release) | ~11 s | measured |
| `dotnet publish` Blazor WebGL head | ~11 s (no `wasm-tools`, so no AOT) | measured |
| Playwright suite | 24 tests, 2 viewports, ~25 s | `npx playwright test` |
| xunit suite | 8 tests | `dotnet test` |

Budgets to watch (set in I12, warned about now): boot under ~3 s on a normal connection, payload under
~8 MB transferred, 60 fps in a Chromium tab. The 17 MB on disk is mostly uncompressed framework files
plus per-locale ICU data; the browser only downloads what it needs.

## The verification loop

```
npm run build    # dotnet build, all heads
npm run unit     # xunit on the pure sim + harness protocol
npm run web      # dotnet publish -> export/web (the entry build)
npm run serve    # serve export/web on http://localhost:4173 (brotli/gzip aware)
npm test         # Playwright against the served WebGL build, 1280x720 + 420x760
npm run shot     # rewrite snapshots + refresh docs/screenshots review copies
npm run check    # build + unit + web + test: the "never commit red" gate
npm run dev      # desktop head (OpenGL/SDL2) for feel â€” the only way to judge game feel
```

## `window.__GAME`

One JavaScript surface over one .NET entry point (`Bridge.BridgeCall`), which forwards to
`Balloonatic.Core.TestHarness` â€” the same object the xunit suite drives directly, so the protocol is
pinned in one place and cannot drift between browser and unit tests.

| Call | Purpose |
| --- | --- |
| `ready`, `readyAtMs` | first drawn frame reached, and how long boot took |
| `version()` | bridge protocol (`i3`) |
| `screen()` | which screen the game is actually drawing |
| `screen(id)` | request a screen change; answered `ok:requested=<id>`, applied by the game's own frame loop, so callers poll `screen()` to confirm it |
| `reset(seed)`, `seed()`, `tick()`, `hash()`, `state()` | scripted replays with a comparable hash |
| `set('heat', 1)`, `set('ballast', 1)` | inject input â€” **there is no left/right**, and the protocol rejects one |
| `step(n)`, `pause()`, `resume()` | move the clock by hand, so screenshots do not depend on frame rate |
| `temperature()`, `altitude()`, `velocity()`, `ballast()` | the balloon model's readouts, fixed-format strings so browser and xunit can assert the same numbers |
| `x()`, `velocityX()` | downwind distance and horizontal velocity â€” the wind's side of the model |
| `level()`, `level(id)` | read or switch the authored level; switching re-launches the flight (`ok:level=<id>`, `error:unknown-level:<id>`) |
| `frames()`, `metrics()` | renderer-side counters: frame count, viewport/backbuffer/scale |

Rules this enforces: only the harness advances time; `pause()` stops the game loop dead; rendering
reads state and never writes it, so effects cannot perturb a replay.

## Evidence layout

- `tests/*-snapshots/*.png` â€” committed pixel baselines (regression net, `maxDiffPixelRatio` 1 %)
- `docs/screenshots/NN-<initiative>-<what>-<project>.png` â€” review copies a human is expected to judge

## Known gaps carried forward (deliberate, I0 scope)

1. **HiDPI**: the canvas is sized in CSS pixels, so on a 2Ã— display the browser upscales it smoothly.
   Pixel-art crispness on retina wants a `devicePixelRatio`-aware surface size â€” I6/I7 territory.
2. **Fractional downscale** below 1Ã— (a 420-px-wide phone) uses nearest sampling, so thin wind dashes
   shimmer slightly. Fix is a linear sampler below 1Ã— â€” I12 polish pass.
3. **Kit art stands in for real sprites**: the balloon, islands and clouds in the kit are procedural
   stand-ins; I8's Aseprite output replaces them. The font decision is settled (hand-authored 5Ã—7 in
   `PixelFont.cs`, no content-pipeline dependency) and the style-sheet screenshot proves the text path
   on WebGL.

## Decisions recorded by I1

- **Font**: hand-authored pixel font in code, not MGCB `.spritefont`. No font file in the payload, no
  pipeline dependency, identical pixels on both heads; the display logo remains a drawn asset (I8).
- **Palette**: sampled from the concept sheet, not chosen â€” `tools/sample-palette.ps1` prints the
  evidence and `docs/STYLE.md` locks it. Where a ramp needed a step the sheet lacks, `Theme.cs` marks
  it `INTERPOLATED`.
- **Screen switching**: `screen(id)` is a request the game applies on its own frame, and `screen()`
  reports only what is actually drawn — a test cannot make the game pretend to be on a screen.

## Decisions recorded by I4

- **Camera = pure function of state.** Position plus velocity look-ahead (plus a downwind framing bias
  until the delivery is behind the balloon) is already smooth, because the physics is continuous — so
  there is no render-side smoothing state, and a stepped frame is byte-stable. The world screenshot
  test additionally asserts the state hash is unmoved after real frames of the world screen have
  drawn: drawing reads state and never writes it.
- **Live play boots on `world`**; a test's `reset` returns to the `scaffold` physics test bed, and any
  pose that needs a specific screen requests it and polls until the game confirms the switch.
