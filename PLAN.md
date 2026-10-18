# Charlotte's Balloonatic — PLAN

Ludum Dare 60 entry, theme **Old Tech**. Concept: `docs/CONCEPT.md`. Rules: `AGENTS.md`.
**Engine: KNI** (shared C# core + Windows desktop head + Blazor WebGL head). Not Unity.

One initiative = one commit, ticked here in that same commit. Each initiative is scoped to ≈90
minutes. Work top to bottom; do not start a later initiative to avoid feeling a earlier one.

## Clock
The clock and milestones below are the original scaffold's planning snapshot. For the October 17
assessment and improvement queue, use the section immediately below; re-estimate remaining time
before implementation.

| | |
| --- | --- |
| Now | Sat Oct 17, ~05:00 EDT |
| Compo ends | Sun Oct 18, 18:00 EDT (not our target) |
| **Jam ends** | **Mon Oct 19, 18:00 EDT** |
| Working assumption | ~30–35 focused hours with sleep and a two-week-old in the house |

Milestones: **feel gate** (I5) by Sun ~20:00 · **content freeze** (I10) by Mon 12:00 ·
**audio+juice** (I12) by Mon 15:30 · **submission** (I13) 16:00–17:30.

## Assessment and improvement queue — October 17

Full findings, evidence, dependencies, acceptance criteria and cuts: [docs/ASSESSMENT.md](docs/ASSESSMENT.md).
The original checked initiatives below remain implementation history; they are not a claim that the
game meets the reference's creative bar. This queue takes priority over stretch work. Planning is
complete; implementation is proceeding in the order below.

- [x] **I15 — Assess current game and plan recovery** (documentation)
  Compared the reference and shared conversation with code, desktop launch/idle behavior and published
  WebGL probes; reviewed `docs/screenshots/15-assessment-*.png`. Verified Release build with zero
  warnings/errors, 58 unit tests and 78 Chromium tests at both viewports. Confirmed empty-ballast
  descent trap, volume-9 audio failure, missing cargo/recipient state, and hazard-disabled route proof.
- [x] **I16 — Audio settings reliability** (~60m)
  Correct 0–10 normalization and independent music/SFX/burner control; browser endpoint/mute regression.
  Verified: build clean, 58 unit tests, 80 browser tests; reviewed `16-audio-settings-*` at both sizes.
  Existing narrow type issue is tracked by I24; audio mix is mirrored through `raw('audioMix')`.
- [ ] **I17 — Recoverable flight** (~90m)
  Landable 0/1/2-bag loadouts, clearer thermal response, forgiving course recovery; human flight gate.
  Engineering verified: 63 unit tests and 82 browser tests; reviewed `17-flight-recovery-*` and
  refreshed flight baselines. **Pending: Steven's feel report** (desktop playtest copy is open).
  The tested mechanical fix is committed; the box stays open until the human gate is satisfied.
  Independent readability/content work may proceed while that report is pending; no feel approval
  is inferred from automated solvability.
  Follow-up: clamp rising flight at the authored sky ceiling, keep the envelope clear of the HUD,
  and allow cooling to descend again. Verified: 76 unit tests, 94 browser tests; reviewed
  `17-flight-ceiling-*`. This does not close the human feel gate.
- [x] **I18 — Readable wind and approach** (~90m)
  Wind ladder/neighbor preview, matching world cues, and gentle/fast approach feedback.
  Verified: 66 unit tests, 84 browser tests; reviewed `18-wind-approach-*` at both sizes.
  Instrument space is reserved above the envelope; heat gauge marks load-dependent hover temperature.
  Screenshot exports retry only transient file access errors and settle screen transitions.
- [x] **I19 — Real delivery state** (~90m)
  One visible teddy pickup, carried cargo, matching nursery recipient, and grateful completion.
  Verified: 69 unit tests, 86 browser tests; reviewed `19-cargo-pickup-*` and `19-cargo-home-*`.
  Empty/wrong parcels cannot complete a delivery; pickup/delivery state is hashed and replayed.
  The existing parcel icon and recipient marker will receive their final art in I22.
- [x] **I20 — Nursery tutorial route** (~90m)
  Safe teaching geometry, useful altitude choice, readable destination; legal replay with hazards on.
  Verified: 73 unit tests, 88 browser tests; reviewed `20-nursery-perch-*` and `20-nursery-home-*`.
  A landing perch marks the faster breeze; low teaching corridor is clear. Recorded legal input
  replays match full state hashes on all three courses with collisions enabled; nursery retains 3 hearts.
  Human feel approval is still tracked under I17, independently of these route proofs.
- [x] **I21 — Charlotte and craft** (~90m)
  Reference-led rounded balloon, readable Charlotte/bunny, portrait and minimal reactions.
  Verified: 73 unit tests, 90 browser tests; reviewed `21-charlotte-{idle,worried,happy}-*` and a
  magnified Aseprite export. Hero/portrait share expressions; bags are attached to the basket.
  `npm run art` uses the installed Aseprite MCP, saves named slices and palette in the source, and
  audits the export. Two builds produced identical PNG bytes; payload remains ~3.13 MiB locally.
- [x] **I22 — Nursery environment assets** (~90m)
  One nursery landmark/recipient, coherent island modules and rounded cloud variants; reproducible sources.
  Verified: 73 unit tests, 92 browser tests; Aseprite palette/slice audit, 45 slices; reviewed
  `22-nursery-arrival-*` and `22-nursery-thanks-*`. Nursery/cottage, bunny recipient, teddy/bottle/letter,
  perch and cliff/plant/waterfall/cloud assets replace their temporary markers through shared selectors.
- [x] **I23 — Compose the sky** (~90m)
  Replace dominant ground slab with cloud depth, authored scenery and a clear arrival; calm parallax.
  Verified: 76 unit tests, 96 browser tests; reviewed all `23-sky-*` poses at both sizes. Shared
  menu/world cloudscape, distant Aseprite islands, seconds-based drift and Reduced Motion coverage.
- [x] **I24 — Type, HUD and narrow readability** (~90m)
  Shared readable type/layout and cargo HUD; real focus/hit targets and both viewport reviews.
  Verified: 76 unit tests, 98 browser tests; reviewed `24-readable-flight-*` and
  `24-delivery-action-*`. Essential text grows from scale 2 to 3 at narrow widths; cargo leads,
  results have a shared 272×36 hit target, keyboard Enter and pointer input both reach the map.
- [ ] **I25 — Title and menu composition** (~90m)
  Illustrated title/logo, clear Start Adventure/dedication, short teaching copy and illustrated courses.
- [ ] **I26 — Voyage and closure** (~60m)
  Distinct destination thumbnails, truthful progress, remembered results/settings and a simple dedication.
- [ ] **I27 — Toy Isles** (~90m)
  Distinct toy landmark and opposing-wind delivery, using the approved kit; active-hazard route proof.
- [ ] **I28 — Cloud Garden** (~90m)
  Garden/windmill landmark, telegraphed gust challenge and safe approach; active-hazard route proof.
- [ ] **I29 — Dream-world sound** (~60m)
  Gentle music-box character, restrained mix and appropriate Charlotte reactions; provenance and listening gate.
- [ ] **I30 — Submission revalidation** (~90m)
  Full gates, real-collision voyage, settings/reload/focus-loss coverage, both viewport reviews and
  fresh WebGL/archive smoke. Reconcile STYLE/credits/completion claims and finish I13's ship requirements.

**Budget:** I16–I25 ~14.5 focused hours; the rest ~6.5 hours, plus 2–3 hours contingency.
Judge controls after I20 and the full first slice after I25 before extending it.
**Cut first:** I27–I28, elaborate progression/map work, collectibles and extra animation. Preserve
recoverable flight, one meaningful beautiful delivery, readable UI and WebGL validation.

## Design decisions (answers to `docs/CONCEPT.md` §16)
1. **Burner:** hold-to-heat with a temperature integrator. No intensity slider — one verb, and the
   lift curve does the expressing.
2. **Vent:** not in the MVP. Descent is cooling. A vent is a stretch goal (adds a second simultaneous
   hold with no new decisions for the player).
3. **Wind bands:** fixed per level, authored; gust *zones* (storm clouds) are the only dynamic wind.
   Live-shifting bands turn into a tuning mess on day three.
4. **Landing:** trigger **and** soft-touch — contact the destination platform with `|vy|` under a
   threshold. A hard contact bounces the basket and the delivery simply does not register; the HUD says
   why. Hard landings cost no hearts.
5. **Damage:** 3 hearts, hazards only, 1.2 s invulnerability + knockback. Cargo is never damaged.
   Hearts at zero restarts the level at the launch platform, keeping collected stars.
6. **Stars:** optional. Rating is 1–3 from time + hearts left + stars collected, shown on delivery.
7. **Camera:** scrolls within per-level bounds, smooth follow with velocity look-ahead, parallax
   background layers.
8. **Ballast:** in the MVP — 2 sandbags per level, non-recoverable, and the most legible piece of
   "Old Tech" the balloon carries.
9. **Resolution:** design canvas **640×360**, integer-scaled and letterboxed (1280×720 = ×2). Balloon
   ≈40×56 design px, Charlotte ≈10×12 inside the basket, 16 px grid for island props. The palette is
   sampled from the concept sheet, not a fixed 16: the sheet is a modern pixel-art sheet (~50 values),
   and I1 locks what it actually contains — see `docs/STYLE.md`.
10. **Forgiveness:** a **Wind Assist** setting (softens the spread between bands and slows gusts),
    instant restart, and every level ships with an autopilot solvability test so no level is ever
    authored unsolvable.

## Initiatives

### Foundation
- [x] **I0 — Scaffold + verification harness** (~90m)
  `kni-multiplatform` solution: `Balloonatic.Core` (pure sim, no KNI types), `Balloonatic.Game`
  (shared draw layer), `Balloonatic.Desktop`, `Balloonatic.Web`; `dotnet test` project; npm scripts
  `dev/build/serve/test/shot`; Playwright config at 1280×720 + 420×760; `window.__GAME` bridge with
  `ready`, `screen`, `step(n)`, `stateHash()`; a placeholder title screen using the I1 palette.
  *Done when:* WebGL build boots in headless Chromium, `dotnet test` green, boot screenshots at both
  viewports reviewed, timings recorded.
- [x] **I1 — Style lock + UI kit** (~60m)
  `docs/STYLE.md` + `Theme.cs`: palette sampled from `docs/reference/concept-sheet.png`
  (`tools/sample-palette.ps1` prints the evidence into `docs/reference/palette.txt`), hand-authored 5×7
  pixel font + type scale, 8 px grid, panel/button/gauge/heart/star/pip/windsock components drawn from
  the kit. *Done when:* a component-sheet scene renders every component from `Theme` only, screenshots
  reviewed for legibility, no hardcoded hex outside `Theme.cs`.

### Core (the game is only as good as this)
- [x] **I2 — Balloon sim** (~90m)
  Pure fixed-step (60 Hz) sim: temperature → lift, ambient cooling, gravity, vertical drag, ballast
  weight, seeded RNG, state hash. **No horizontal input at all.** All tunables in `SimTuning.cs`.
  *Done when:* xunit proves heating rises, cooling descends, ballast raises the ceiling, momentum
  overshoots after heat stops, and identical hashes for identical inputs.
- [x] **I3 — Wind layers + level data** (~60m)
  Three+ altitude bands of direction/strength, authored per level (`LevelCatalog`); horizontal
  velocity chases band wind with inertia; gust-zone hook; PD-loop autopilot.
  *Done when:* tests prove velocity never snaps across a band boundary, and an autopilot (heat toward
  a target band) reaches a target x — the same test reused for every level later.
- [x] **I4 — Draw layer + camera** (~90m)
  Balloon/basket/Charlotte placeholders from the I1 kit, parallax cloud layers, wind streaks readable
  as direction+strength, smooth follow with look-ahead, letterboxed integer scaling, desktop + WebGL
  parity.
  *Done when:* Playwright screenshots at both viewports show a readable balloon against its background,
  and effects on/off keeps the state hash identical.
- [x] **I5 — Delivery loop + landing rule** → **FEEL GATE** (~90m)
  Launch platform, pickup, destination, soft-touch landing rule with a reason shown on a hard bounce,
  level-complete + 1–3 rating, instant restart.
  *Done when:* an autopilot run completes a delivery in the **WebGL** head under Playwright, and
  **Steven has played it and says the balloon feels good.** Do not continue past here on red feel —
  tune heat curve, drag, and band strengths until it does.
  *Status:* **gate passed 2026-10-17** after four playtest rounds: frozen results clock + level
  advance (c411e31), wind halved + background pushed far (50f1919), fixed-step presentation + third
  wind notch (7a408f9). Verdict: *"It feels alright. We'll adjust more after running through your
  plan"* — re-tune offer stands after the I8 art initiative.

### Presentation
- [x] **I6 — HUD** (~90m)
  Charlotte portrait, hearts, stars, delivery counter, vertical heat meter with flame, ballast pips,
  wind arrow + band marker — all kit-drawn, all mirrored in `__GAME` so tests can assert them.
  *Done when:* screenshots reviewed at 1280×720 and 420×760 with nothing clipped or unreadable.
- [x] **I7 — Menus, flow, settings** (~90m)
  Title (**For Charlotte ♥**), level select, settings (music/SFX volume, Wind Assist, reduced motion),
  credits, pause; keyboard-and-mouse with designed focus states, mouse-only playable too.
  *Done when:* Playwright drives title → level → pause → settings → credits with screenshots at both
  viewports.

### Content
- [x] **I8 — Aseprite asset pipeline** (~90m)
  Committed generator script (Aseprite MCP) + `.aseprite` sources + exported PNGs under `art/`:
  balloon (idle/rise/hurt), Charlotte expressions, bunny, sandbag, stars, packages, platform/island
  sets for levels 1–3, HUD bits, title logo. Build-time import, one re-export command.
  *Done when:* placeholders replaced everywhere, screenshots reviewed against the concept sheet, and
  the WebGL payload is measured, not guessed.
  *Status:* pipeline + all world/game sprites shipped (plan steps A–F ticked, commits `629d6d3`…`4ce0ef1`):
  hero balloon with Charlotte + bunny, 3 clouds, 2 islands, 3 windsock fills + flutter, falling bag,
  stars/hearts — palette-audited export, embedded sheet 8.5 KB, every world placeholder replaced, screenshots
  judged both viewports. Title logo + Charlotte portrait + HUD gauge/tags are the *same pipeline's* next
  sprites, scheduled inside I6 which owns that screen.
- [x] **I9 — Hazards + damage** (~90m)
  Floating rocks, birds on authored paths, storm-cloud gust zones; knockback, hearts, invulnerability
  flicker, reduced-motion-safe shake.
  *Done when:* one sim test per hazard plus a screenshot of each in play.
- [x] **I10 → content freeze** — **Levels + world map** (~90m)
  **Nursery Skies** (teaches heat/cool/land), **Toy Isles** (opposing bands), **Cloud Garden** (strong
  wind + hazards), taught by level geometry and windsock placement, not text; world-map screen between
  levels; every level passes the I3 autopilot solvability test.
  *Done when:* three levels completable start-to-finish in WebGL, each with reviewed screenshots.

### Ship
- [ ] **I11 — Audio (ElevenLabs + KNI WebAudio)** (~60m)
  Music loop + burner, basket creak, star chime, giggle, delivery jingle; provenance in
  `assets/audio/SOURCES.md`; volumes live in settings; every call behind a mute check so a missing clip
  can never throw in the browser.
  *Done when:* the WebGL build boots and plays with zero console errors, and Steven has listened.
- [x] **I12 — Juice + performance** (~90m)
  Burner puffs, star sparkles, sand puffs, basket sway, delivery celebration, screen shake behind the
  reduced-motion setting, screen transitions; then a budget pass: boot time, payload size, 60 fps in a
  Chromium tab.
  *Done when:* Playwright measures average frame time < 20 ms during play, boot-to-title logged,
  screenshots reviewed.
- [ ] **I13 → submission** — **Ship checklist** (~60m)
  Clean `dotnet publish` to `export/web/`, verified from a fresh clone and from the zip over http;
  `README.md` + LD entry copy (blurb, controls, theme note, credits, art/audio provenance); entry
  archive; `ld60-submit` tag.
  *Done when:* the submission checklist is run end to end and the last screenshot is one Steven would
  put on the entry page.

### Stretch (in order, only when I13 is safe)
Level 4 Moonlight Bay · Level 5 Star Observatory · ending crib scene ("Sweet dreams, Charlotte.
Love, Mom & Dad.") · touch controls for the LD page on phones · GIF/preview for the entry page ·
speedrun-style time splits.

## Cut ladder — if the clock gets tight, drop in this order
1. Stretch levels 4–5 and the ending scene.
2. World-map screen → straight level-to-level advance.
3. Hazards beyond rocks.
4. Stars and 3-tier rating → single "delivered" state.
5. Ballast (the loop survives without it).
6. Second and third levels → **one level, beautiful, and shipped.**

## Risks and how we handle them
| Risk | Handling |
| --- | --- |
| Movement feels flat and nothing else matters | I2–I5 exist only to fix this; feel gate at I5 with Steven at the keyboard before content work |
| KNI content pipeline / `.xnb` builds in Blazor headless builds | Prefer procedurally-drawn + PNG assets loaded without the content compiler where the pipeline fights us; decide in I8, not in a panic in I12 |
| WASM boot too slow / payload too fat for the LD page | Measure at I0 (baseline), I8, I12 — a number each time, never a vibe |
| Aseprite output misses the key-art quality bar | I1 palette + silhouette pass first; the sim ships on kit art if art runs long |
| I cannot hear | Every audio change is verified by Steven's ears; build is always shippable muted |
| Feel needs human hands and I only see pixels | Two scheduled playtest slots: I5 (feel) and I12 (polish); tune parameters from a written report between them |
| WebGL audio quirks | Mute-first architecture; `console` errors fail the Playwright run |
| Hosting for the in-browser entry | Decide by Mon 12:00 (itch.io upload vs self-host); I13 needs the link, not the argument |

## Human playtest protocol
Between sessions, Steven plays the desktop head (`npm run dev`) and reports in writing: heat response
(sudden/sluggish), how readable band changes are, landing frustration 1–10, and the one thing that
would most improve feel. Physics tunables live in one config object so a report becomes a one-commit
tune with the tests still green.
