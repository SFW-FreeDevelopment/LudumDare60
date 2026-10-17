# Ludum Dare 60 — Agent Rules

## What this is
Ludum Dare 60 jam entry (theme: **Old Tech**), built with [KNI](https://github.com/kniEngine/kni).
The entry ships as **WebGL** (Blazor WebAssembly). A Windows desktop head exists only so iteration
stays fast — it is never the deliverable.

- `PLAN.md` — the initiative list. This is the work queue.
- `docs/JAM.md` — jam deadlines, entry rules, toolchain baseline, references.
- `docs/STYLE.md` — locked palette, type, spacing, post-process recipe.
- `docs/screenshots/` — visual evidence, one or more per initiative.

## Hard constraint: it is a jam
Compo ends **Sun Oct 18 2026, 18:00 EDT**; Jam ends **Mon Oct 19 2026, 18:00 EDT**.
Every initiative must be finishable in ≈90 minutes. If it is bigger, split it.
When behind, cut a feature before cutting polish — one small, sharp, good-looking game wins.

## Commit rules (mandatory)
1. **One commit per completed initiative.** When an initiative in `PLAN.md` is finished — code
   written, build clean, tests green, screenshots reviewed — commit it immediately, before starting
   the next one. Do not batch initiatives, and do not leave a finished initiative uncommitted.
2. Conventional Commits with an initiative scope: `feat(crt-postfx): scanlines + phosphor bloom`,
   `fix(menu): keyboard focus ring`, `chore(scaffold): ...`, `docs(plan): ...`, `test(...)`.
3. Commit body names the initiative and how it was verified, e.g.
   `Initiative: 4 (CRT post-process)` + `Verified: playwright 12/12, docs/screenshots/04-*.png`.
4. Tick the initiative `- [x]` in `PLAN.md` in the **same** commit that completes it.
5. **Never commit red.** Before committing an initiative:
   - `dotnet build -c Release` clean for shared core + desktop head + Blazor WebGL head
   - `dotnet test` green (pure-core tests)
   - `npx playwright test` green against the WebGL build in Chromium
6. Follow-up fixes to an earlier initiative get their own `fix(scope):` commit — still one logical
   change per commit, still one initiative's worth of intent per commit.
7. Partial work at the end of a session: commit it (`wip(scope): <what> (partial)`) with the PLAN
   box left unticked and a "left to do" line in the body. Committed partial beats lost work.
8. Push `main` once per completed initiative (`origin` = `SFW-FreeDevelopment/LudumDare60`).
   Jam time is exactly when an off-machine backup matters.
9. Keep the tree clean: build output, `_framework/`, `node_modules/`, `_scratch/` stay out of git;
   reviewed screenshots go in `docs/screenshots/`, never at the repo root.

## Definition of done — an initiative is complete when
- [ ] Behaviour it claims exists and runs in the **WebGL** head, not just the desktop head.
- [ ] Build + tests green (all three gate commands above).
- [ ] At least one Playwright test asserts the initiative's observable behaviour through the
      `window.__GAME` bridge, not through pixels.
- [ ] At least one screenshot in `docs/screenshots/NN-<initiative>-<what>.png` at 1280×720, and one
      at 420×760 when the initiative touches anything the player reads or presses.
- [ ] The agent **opened and judged** those screenshots: legible text, on-palette, nothing clipped,
      overlapping, or default-styled. Not green tests but ugly output.
- [ ] `PLAN.md` box ticked, committed alone.

## Styling rules — "nice styling" is a requirement, not a stretch goal
- Every visual decision comes from `docs/STYLE.md`: palette, type, spacing grid, radii, motion.
  No hardcoded hex in C# outside the palette file, no default-font text, no default widgets.
- One shared UI layer (borders, panels, buttons, type scale) used by every screen; no per-screen
  one-off styling.
- Everything pixel-snapped at the design resolution, integer-scaled and letterboxed to the viewport;
  the canvas resizes with its container so the LD page iframe looks intentional at any width.
- The theme is carried by the subject, not a filter: **no CRT/scanline/phosphor post-processing**
  anywhere (see `docs/CONCEPT.md` Amendment 2). "Old Tech" reads through the balloon's machinery —
  burner flame, temperature gauge, rope and wicker, sandbags, windsocks — drawn in the same 16-bit
  style, and it must read on the menu, the HUD and the game-over screen, not only during play.
- Every input gets feedback (hover/press/focus), every state change gets a transition or a sound,
  and both must be cheap enough to hold 60 fps in a browser tab.
- Keyboard-only playability is required; focus state must be as designed as hover state.

## Engineering rules
- Target frameworks/heads: shared game logic in the KNI **shared class library**; platform heads stay
  thin. Use `TitleContainer.Platform` and `graphicsDevice.Adapter.Backend` runtime checks, not
  `#if DESKTOPGL`-style symbols (KNI 4.3 templates compile shared code to a DLL).
- Simulation is pure, deterministic, fixed-timestep C# in the core with no KNI/audio/DI dependencies,
  seeded from a single `ulong`. Rendering, audio and FX never write sim state — the test suite proves
  identical state hashes with effects on and off.
- Test bridge: one small JSInterop surface exposing `window.__GAME` — `ready`, `screen`, `seed`,
  `step(n)`, `stateHash()`, plus input injection — so Playwright asserts state instead of guessing
  from screenshots. Screenshots are for judging looks, tests are for judging behaviour.
- Content stays small: procedural/prerendered at build time over large art dumps, so the WebGL build
  and the LD zip stay light and boot fast.
- Assets are generated and reproducible: pixel art comes from Aseprite driven through the Aseprite MCP
  by a committed generator script (`.aseprite` sources + exported PNGs under `art/`); audio comes from
  ElevenLabs with provenance noted in `assets/audio/SOURCES.md`. Re-exporting must be one command, and
  nothing depends on a file sitting outside the repo.
- No warnings tolerated in shared core; `null`-hostile APIs wrapped once, at the bridge.

## Verification loop (Playwright)
- Serve the **published WebGL output** over `http://localhost` (file:// will not load WASM), drive it
  with Playwright Chromium, and screenshot from the same run.
- WASM cold boot is slow and machine-dependent: wait on `window.__GAME.ready`, generous timeouts
  (≥ 60 s on first load), and never assert on absolute boot wall-clock times.
- Run the suite at 1280×720 and at 420×760; capture both.
- Fixed-step tests use `__GAME.step(n)` so assertions do not depend on frame rate or headless rAF
  starvation.
- Screenshot byte drift between full runs is normal; committing the new shots is fine.

## Commands
npm scripts are the front door for the .NET build; use them rather than rediscovering `dotnet` flags.

| Command | What it does |
| --- | --- |
| `npm run dev` | Desktop head (OpenGL) — the only way to judge *feel*; the browser proves behaviour |
| `npm run build` | Release build of core + shared game + desktop + WebGL heads |
| `npm run unit` | xunit on the pure sim and the harness protocol |
| `npm run web` | Publish the WebGL entry build to `export/web/` |
| `npm run serve` | Serve `export/web/` on `http://localhost:4173` (brotli/gzip aware, like a host) |
| `npm test` | Playwright suite against the served WebGL build, 1280×720 + 420×760 |
| `npm run shot` | Rewrite snapshot baselines and refresh `docs/screenshots/` review copies |
| `npm run check` | build + unit + web + Playwright — the "never commit red" gate |

Measured baselines, budgets and the `window.__GAME` protocol: `docs/BASELINE.md`.

## Repo hygiene
- `main` stays working and shippable; no force-pushes after a build is shared.
- Delete files by moving them to `_toDelete/` (ignored), not by `rm`.
- The deliverable is a browser-playable WebGL build: verify `export/web/index.html` from a clean
  publish before calling anything submittable.
