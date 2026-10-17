# Build baseline and verification harness

Numbers are recorded so later initiatives have something to compare against instead of a feeling.
Measured by the Playwright suite (`npx playwright test`), which serves the published WebGL build
exactly the way an entry host would.

## WebGL build — I0 baseline (2026-10-17)

| Metric | Value | How it is measured |
| --- | --- | --- |
| Cold boot to first drawn frame | **~470–510 ms** | `window.__GAME.readyAtMs`, page start → first `Draw` |
| Transferred payload | **2.83 MB over 76 requests** | `performance.getEntriesByType('resource')` transfer sizes, brotli-served |
| Published payload on disk | 17.18 MB across 204 files (59 `.br` sidecars, 2.81 MB) | `export/web/` after `dotnet publish` |
| `dotnet build` (5 projects, Release) | ~11 s | measured |
| `dotnet publish` Blazor WebGL head | ~11 s (no `wasm-tools`, so no AOT) | measured |
| Playwright suite | 14 tests, 2 viewports, ~13 s | `npx playwright test` |
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
npm run dev      # desktop head (OpenGL/SDL2) for feel — the only way to judge game feel
```

## `window.__GAME`

One JavaScript surface over one .NET entry point (`Bridge.BridgeCall`), which forwards to
`Balloonatic.Core.TestHarness` — the same object the xunit suite drives directly, so the protocol is
pinned in one place and cannot drift between browser and unit tests.

| Call | Purpose |
| --- | --- |
| `ready`, `readyAtMs` | first drawn frame reached, and how long boot took |
| `version()` | bridge protocol (`i0`) |
| `screen()` | which screen is showing |
| `reset(seed)`, `seed()`, `tick()`, `hash()`, `state()` | scripted replays with a comparable hash |
| `set('heat', 1)`, `set('ballast', 1)` | inject input — **there is no left/right**, and the protocol rejects one |
| `step(n)`, `pause()`, `resume()` | move the clock by hand, so screenshots do not depend on frame rate |
| `frames()`, `metrics()` | renderer-side counters: frame count, viewport/backbuffer/scale |

Rules this enforces: only the harness advances time; `pause()` stops the game loop dead; rendering
reads state and never writes it, so effects cannot perturb a replay.

## Evidence layout

- `tests/*-snapshots/*.png` — committed pixel baselines (regression net, `maxDiffPixelRatio` 1 %)
- `docs/screenshots/NN-<initiative>-<what>-<project>.png` — review copies a human is expected to judge

## Known gaps carried forward (deliberate, I0 scope)

1. **HiDPI**: the canvas is sized in CSS pixels, so on a 2× display the browser upscales it smoothly.
   Pixel-art crispness on retina wants a `devicePixelRatio`-aware surface size — I6/I7 territory.
2. **Fractional downscale** below 1× (a 420-px-wide phone) uses nearest sampling, so thin wind dashes
   shimmer slightly. Fix is a linear sampler below 1× — I12 polish pass.
3. **No text yet**: the harness has no font, so nothing here proves the content-pipeline/text path.
   I1 decides between a `.spritefont` through MGCB and a bitmap font shipped as a PNG, and that
   decision unblocks real HUD work.
4. **Placeholder art**: `Palette.cs` colors and the rectangle balloon are harness stand-ins, replaced
   by I1's sampled palette and I8's Aseprite output.
