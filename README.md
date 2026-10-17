# Charlotte's Balloonatic

Ludum Dare 60 entry — theme **Old Tech**. A baby named Charlotte crosses a sky of floating islands in
an old-fashioned hot-air balloon with no engine and no steering: heat to rise, cooling to sink,
sandbags for emergencies, and the wind at whatever altitude you choose as your only means of going
anywhere. Deliveries are the reason to make the trip.

Built with [KNI](https://github.com/kniEngine/kni). The entry is a **WebGL** build (Blazor WebAssembly);
a Windows desktop head exists so the balloon's feel can be judged at the keyboard.

- Rules and workflow: [`AGENTS.md`](AGENTS.md)
- Initiative list and design decisions: [`PLAN.md`](PLAN.md)
- Concept and theme: [`docs/CONCEPT.md`](docs/CONCEPT.md) · jam context: [`docs/JAM.md`](docs/JAM.md)
- Build baselines and the `window.__GAME` test bridge: [`docs/BASELINE.md`](docs/BASELINE.md)
- Visual evidence: [`docs/screenshots/`](docs/screenshots)

## Toolchain

.NET SDK 8 or newer (built against 10.0.101), Node 20+, Playwright Chromium
(`npx playwright install chromium`), and the KNI templates
(`dotnet new install nkast.Kni.Templates`).

## Run it

```bash
npm install
npm run dev      # desktop head — fastest way to feel it
npm run web      # publish the WebGL entry build to export/web/
npm run serve    # serve it at http://localhost:4173 like an entry host would
npm test         # Playwright against the served WebGL build, two viewports
npm run check    # build + unit tests + WebGL publish + Playwright — the commit gate
```

## Controls

| Input | Action |
| --- | --- |
| <kbd>Space</kbd> / hold left mouse | Burner — heat rises |
| release | Cool air — sink |
| <kbd>B</kbd> / right mouse | Drop a sandbag (there are two; they are the brake) |
| <kbd>↑↓</kbd> / <kbd>W</kbd><kbd>S</kbd> | Menu focus |
| <kbd>Enter</kbd> | Confirm · fly on from the world map |
| <kbd>R</kbd> | After a delivery: the voyage chart. After a crash or empty hearts: fly this sky again |
| <kbd>Esc</kbd> | Pause in flight · back out of a menu |

Fully keyboard-playable; the focus ring is as designed as the hover state. Settings offer music and
sfx volume, wind assist, and Reduced Motion (which removes shake, flicker, puffs, and screen
transitions — the visuals were tuned against a motion-sickness floor, and they stay there).

## The theme, carried honestly

**Old Tech** is the machine, not a filter: a burner you feather by hand, a temperature gauge with a
live flame, rope and wicker, sandbags as the only brake, windsocks and storm banks you read like a
chart. No CRT scanlines anywhere — the 16-bit look comes from what the balloon *is*, and there are
no left/right controls because a balloon has no rudder: horizontal travel is the wind's, and the
wind is chosen by altitude.

## Credits & provenance

- Engine: [KNI](https://github.com/kniEngine/kni) 4.3 (MonoGame-family, .NET), shipped as Blazor
  WebAssembly/WebGL.
- Art: generated — `art/.art/generate.js` computes every sprite through the Aseprite MCP
  (sources + exports under `art/`, palette audit in `art/.art/audit.js`).
- Audio: generated — `audio/.audio/generate.js` synthesizes the whole chiptune kit
  (`assets/audio/SOURCES.md` records each clip's recipe).
- Font: hand-authored 5×7 pixel face (`PixelFont.cs`).
- Design and code: Steven Garberg. *For Charlotte ♥*

## Submitting

`npm run zip` builds `dist/charlottes-balloonatic-ld60-web.zip` — the published `wwwroot` with
`index.html` at the archive root, which is what the LD page wants. Copy-paste entry text (blurb,
controls, theme note, credits) lives in [`docs/LD_ENTRY.md`](docs/LD_ENTRY.md).

## Layout

| Path | What lives there |
| --- | --- |
| `Balloonatic/Balloonatic.Core/` | Pure deterministic simulation — no KNI types, no platform APIs |
| `Balloonatic/Balloonatic.Game/` | Shared draw layer, screens, design-resolution canvas |
| `Balloonatic/Balloonatic.Desktop/` | Windows/OpenGL dev head |
| `Balloonatic/Balloonatic.Blazor.GL/` | **The entry**: WebGL/Blazor head + `window.__GAME` bridge |
| `Balloonatic/Balloonatic.Tests/` | xunit on the sim and the harness protocol |
| `tests/` | Playwright suite and committed pixel baselines |
| `tools/serve.mjs` | Static host that serves the entry build the way a real host would |
| `export/web/` | Published WebGL build (generated, not committed) |

## Links

Event: <https://ldjam.com/events/ludum-dare/60> · Previous entries:
<https://ldjam.com/users/steven-garberg/games>

*For Charlotte ♥*
