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
npm run dev      # desktop head: Space or left mouse = heat, B or right mouse = drop ballast
npm test         # Playwright against the published WebGL build, two viewports
npm run check    # build + unit tests + WebGL publish + Playwright — the commit gate
```

Served at `http://localhost:4173` after `npm run web && npm run serve`.

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
