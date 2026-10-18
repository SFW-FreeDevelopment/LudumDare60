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
| Playwright suite | 78 tests, 2 viewports, ~90 s | `npx playwright test` |
| xunit suite | 58 tests | `dotnet test` |

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
| `version()` | bridge protocol (`i11`) |
| `screen()` | which screen the game is actually drawing |
| `screen(id)` | request a screen change; answered `ok:requested=<id>`, applied by the game's own frame loop, so callers poll `screen()` to confirm it |
| `reset(seed)`, `seed()`, `tick()`, `hash()`, `state()` | scripted replays with a comparable hash |
| `set('heat', 1)`, `set('ballast', 1)` | inject input â€” **there is no left/right**, and the protocol rejects one |
| `step(n)`, `pause()`, `resume()` | move the clock by hand, so screenshots do not depend on frame rate |
| `temperature()`, `altitude()`, `velocity()`, `ballast()` | the balloon model's readouts, fixed-format strings so browser and xunit can assert the same numbers |
| `x()`, `velocityX()` | downwind distance and horizontal velocity â€” the wind's side of the model |
| `level()`, `level(id)` | read or switch the authored level; switching re-launches the flight (`ok:level=<id>`, `error:unknown-level:<id>`) |
| `restart()` | the player's R mid-flight over the wire: same seed, same level, same screen, unpaused (`ok:<hash>`) |
| `advance()` | the player's R after a delivery: next authored level (wraps after the last), fresh run on a derived seed, unpaused (`ok:<hash>`) |
| `deliver([sec])` | the autopilot flies the run onto the delivery deck, stepping directly (`ok:delivered` / `error:not-delivered`) |
| `delivered()`, `complete()`, `stars()`, `bounces()` | the landing rule's readouts; `state()` carries the same as `delivered`/`complete`/`stars`/`bounces`/`completedAtTick` |
| `drops()` | sandbags released this run (`SimState.DropsSeen`), also in `state()` as `drops` — the falling-bag art draws from the release records, and the hash covers them |
| `frames()`, `metrics()` | renderer-side counters: frame count, viewport/backbuffer/scale |
| `settings()`, `settings('k=v')` | read/apply the menu options (`music`, `sfx`, `assist`, `motion`) — also mirrored in `state()` as `settings` and `windAssist` |
| `raw('hearts')` | hearts left this run (I9) — also mirrored in `state()` as `hearts` |
| `raw('hazards')` | the authored hazards as of the current tick: `rock:x,y;…;bird:x,y;…;storm:cx,cy` — the same closed-form functions the collision test and the renderer call |
| `raw('warp', 'x=alt')` | park the balloon motionless and vulnerable at x/alt — scripted collisions and hazard screenshots |
| `audio()` | renderer-side kit readout: `unlocked/broken/music/burner` (I11) — proof the browser audio stack woke on a gesture and never retired itself |
| `key(code=1\|0)`, `keyclear()` | internal: the WebGL head's keyboard feed (`WebKeys`), fired by `index.html`'s DOM listeners |

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
- **Live play boots on `menu` and the simulation clock is held there** (start screen, first slice
  of I7): nothing can wander or crash-land while the title is up, so the boot frame is byte-stable
  with no reset or seed at all. Enter **or** a pointer press hands off to `world`; the hand-off
  test drives a real 120 ms canvas click, because a zero-duration synthetic click can fall between
  two 60 Hz updates and never be seen. A test's `reset` keeps the current screen, and any pose that
  needs a specific screen requests it and polls until the game confirms the switch.

## Decisions recorded by I5

- **Decks are the only soft ground.** A soft landing is a downward crossing of the deck plane at
  <= 18 px/s inside the deck's x-span; faster contact bounces (restitution 0.4, counted, and the
  world screen shows a red "TOO FAST!" tag for ~1.5 s). The open ground is always soft and never
  ends a run; only the delivery deck wins it.
- **The landing skill is the sandbag.** Loaded terminal descent (~25 px/s) is deliberately *faster*
  than the soft-landing rule, and unloaded the envelope will not come down at all — so arriving soft
  means spending exactly one bag mid-descent (~12 px/s). That is the B key's whole purpose, and why
  the autopilot never arrives with both bags aboard or none.
- **Decks are 112 px wide (`DeckHalfWidth` 56), sized against the landing read.** A settled approach
  drifts up to ~40 px while the last 50 px of altitude bleed away; anything narrower punishes
  arrivals that are flown correctly. Wide road, small prize.
- **Runs start resting on the launch deck (`DeckTopAltitude` 38), not on the ground.** Cold, the wind
  walks an unburned balloon off the deck within a second or two — the deck is a departure, not a
  parking space (asserted in `BalloonPhysicsTests`).
- **A finished run freezes the simulation, not the world**: `Complete` ignores heat and ballast, while
  `Tick` and the seed keep advancing, so the hash still moves and the ambient world keeps living.
- **`restart` keeps the run's seed, screen and level** — a restart replays literally the same opening
  (proven: post-restart hash equals the fresh-reset hash), unlike `reset`, which is the paused,
  seed-your-own test entry point.
- **The delivery autopilot is receding-horizon planning, not a hand-drawn pattern.** This physics
  gives descent rate to loadout and the horizontal to the wind, so no flyable glideslope exists —
  three hand-authored approaches (geometric slope, touchdown-triggered, state machine) all orbited
  the deck. The pilot that works rolls each candidate altitude (carrying air, lowest air, deck hover,
  committed fall under the deck, return band) forward ~12 s on cloned state, scores the imagined
  ending with the landing read, and flies the winner for a second before re-imagining. Player-legal
  verbs only, fully deterministic, and every authored level ships with a proof it can be delivered
  (all three land clean inside par — a dedicated test demands 3 stars from the ghost pilot, so
  par stays a promise that a good human flight exists, never fiction).
- **Levels are re-authored until the autopilot can land them.** `winding-trail`'s lowest band now
  carries gently toward the deck, and `night-crossing` became a calm floor with a fast ceiling; the
  solvability test (`Every_authored_level_can_actually_be_delivered`) is what licenses the wind data,
  and the comment in `Autopilot.Deliver` explains the pattern the ghost pilot flies.
- **Stars = 1 (delivered) + (under par) + (no bounces)**, with pars 45/60/80 s on the three levels;
  the delivery banner shows time against par so the rating reads as arithmetic, not mystery.

## Decisions recorded by the I5 feel playtest

Steven flew the desktop head and reported three things; each became a rule, not a patch:

- **The results clock freezes at the landing** (`SimState.CompletedAtTick`, in `state()` as
  `completedAtTick`). The sim's own Tick keeps its honest world time, but a banner whose TIME is
  still climbing reads as a bug — and every drifting decoration (clouds, chevrons, star twinkle,
  flame) reads the frozen clock too, so the finished world visibly stands still.
- **R's meaning follows the run**: delivered → next course (`advance`, the course list wraps so the
  last level loops to the first, on a seed derived purely arithmetically from the run's own — never
  object hashcodes, which differ per process); anything else → replay this level's sky unchanged.
  The banner states which one R will do.
- **Background layers move slower than the world, not with it** (playtest: "making me sick").
  The camera already sweeps at wind speed; mid clouds sat at 0.75 parallax and drifted at 40 % of
  band speed on top of that. They are far layers now (0.40/0.25 parallax, 6–10 % drift).
- **Round 2 pulled the last lever: the wind bands themselves are halved** (40/75/110 → 20/38/55
  px/s, gusts scaled with them). Decoration tuning had not been enough — the whole world was
  sweeping past the camera at band speed, and at the old numbers the sky flew *at* the player.
  Courses and pars were kept; the delivery autopilot re-plans every second and absorbed the
  halving without a single constant changing, which is the argument for planning over patterns.
  A balloon rides a breeze: these are the approved floor numbers, not a temporary softening.

## Decisions recorded by I8 (art)

- **Sprites are generated, not painted by hand in an editor session.** `art/.art/generate.js` holds
  every pixel rule (profiles, gore tables, ramps, ASCII blocks) and produces the sheet through
  `pack.js` → `render.js` → Aseprite MCP (`art/src/balloonatic.aseprite`, palette + slices live
  there) → `art/export/sheet.png` + `slices.json`. `node art/.art/audit.js` decodes the export and
  fails on any off-palette pixel or out-of-bounds slice — the same palette rule `Theme.cs`
  enforces, now enforced on the PNG itself.
- **Sheet fidelity follows the concept sheet** (rework pass after review): the hero is the sheet's
   60×75-design onion envelope in a pink/cream checkerboard with a deep-pink crown, red skirt band
   and heart basket holding a big-eyed chibi Charlotte and the bunny; islands are warm-brown strata
   over a violet dithered underside, with an overhanging grass lip, flowers, hanging vines, tree
   clusters and a waterfall down the delivery island. The old 34×39 stripe was a different artist.
 - **`art/export/` was silently gitignored** — the root `export/` rule meant for the Blazor output
   also matched it, so the sheet the game embeds lived only in this working tree. Now `/export/` is
   root-anchored and `art/export/` is committed; a clone can build the real sprites.
- **Sheet pixels are internal-resolution** (2 per design px), matching the 2x canvas: sprites ride
  the half-pixel camera without subpixel sampling, so motion is smooth and edges stay hard.
- **Animation is baked frames + clock index.** Flame/burner frames are separate slices
  (`balloon-0..2`) chosen by `Clock(state)` and `Heating`; the sheet holds no timeline, so a
  deterministically stepped state always draws the identical pixels.
- **Payload**: the sheet is ~23 KB embedded in `Balloonatic.Game.dll` (compressed delta inside the
  noise of the 2.83 MB transfer baseline); both heads load it from one embedded stream — no disk
  reads, no platform branch.
- **I6 HUD is the sheet's instrument set** (crate panel top-left: Charlotte's portrait in its
   stitched frame, hearts, star rating, gift-box delivery counter; wind chevrons + band pips
   top-centre; brass heat gauge with a live two-frame flame cap down the right edge; sprite
   sandbags for ballast). Hearts draw full until I9 gives the sim a hearts field — the empty
   sprite (`heart-hud-off`) is already on the sheet waiting for it.
- **Every HUD number is mirrored in `__GAME.state()`** — `heating`, `temperature`, `ballastHeld`,
   `drops`, `stars`, `delivered`, `level`, `altitude` — and the mirror spec asserts the fields
   actually move (heat climbs, a dump decrements, altitude changes the band). The wind chevrons
   have no field of their own because the wind *is* level + altitude: the HUD derives the band
   with the same `BandAt` the sim steers by.
- **HUD sprites are sheet-authored**: `portrait` (44×48), `heart-hud`/`heart-hud-off` (26×22),
   `star-hud` (26×22), `gift` (26×26), `flame-hud` (2×28×28), `bag-falling` reused for ballast —
   the frame and violet backing are painted into the portrait sprite, so the HUD draws one thing.
- **I7 flow: time runs on exactly one screen.** The sim clock steps only while the world shows; the
   title, courses, settings, credits and pause all hold it (the balloon hangs where it was under the
   pause veil — `PauseScreen` composites the world draw plus a translucent ink wash). Screens talk
   to the game only through `UiFrame` (pointer in design coordinates + one-frame input edges) and
   `UiHost` verbs (`GoTo`, `FlyLevel`, `RestartFlight`, `Quit`, `LiveSettingsChanged`); focus and
   hover are screen-side rendering state, so stepped frames stay byte-stable.
- **KNI's Blazor keyboard dies after the first canvas click** (measured, headless and otherwise:
   the window listeners keep firing and the game loop keeps running, but key state stops updating
   through the sync interop the window module uses — while every mouse event survives). Since real
   players click before they type, the WebGL head answers the keyboard from `WebKeys`: `index.html`
   forwards raw keyCodes through the bridge's async path, and `PlatformBalloonaticGame` sets
   `UseWebKeyboard` so every key read in the game routes through one gate. A press that arrives and
   leaves between two 60 Hz polls (what `page.keyboard.press` does) also latches fresh, so the game
   sees exactly one frame of it instead of nothing — sub-frame taps cannot be lost to polling.
   The tab's blur event clears the table; keys cannot stick through an alt-tab.
- **Wind Assist lives in sim state on purpose** (`SimState.WindAssist`, hashed): band winds still
   steer, gusts are ignored — gentler air, same game. Reduced Motion is renderer-only (the camera
   gives up its velocity look-ahead, the gauge flame holds its still frame). Volumes are mirrored in
   `state().settings` before any sound ships — the numbers are the contract I11 honours.
- **xunit runs sequential now** (`[assembly: CollectionBehavior(DisableTestParallelization)]`):
   `AppSettings` is session state like a real options menu, and a settings toggle in one class was
   racing harness boots in another. The suite takes under a second either way.

## Decisions recorded by I9 (hazards)

- **Solvability is the wind's promise, not the dodge's.** `SimRules.Step` takes a `hazards` flag and
  the Autopilot steps with it off: the ghost proves a delivery line through the wind bands exists,
  and rocks/birds are the player's responsibility. Probing had shown imagined collisions poison the
  planner's rollouts (return-air rocks made every recovery idea score as a knockback, and the ghost
  drifted off the end of the world). Storm buffet stays on for everyone — gusts are wind, and the
  navigation promise has to cover them. `GhostPilotFliesEveryCourseWithoutScratches` guards the
  wiring: stitch real damage back into the ghost and an authored rock on a line goes red.
- **Damage model**: 3 hearts, one hit per 2-second invulnerability window, knockback impulses away
  from what was hit (48 px/s across, 30 up/down by side of contact) so a collision is never a
  freeze. Zero hearts completes the run undelivered — the OUT OF HEARTS panel, not a game-over
  screen; losing is one R away from flying again. A fresh state is invulnerable for its first two
  seconds (launch grace), which also keeps warp-based tests honest: `warp` clears the window.
- **Hazard positions are closed-form in the tick** (triangle-wave bob, polyline ping-pong, square-wave
  gusts — no transcendental state, no integration), so the sprite, the collision test and the
  `raw('hazards')` mirror all read one function of `(level, tick)`, and authored rock/bird/storm
  placement is a data review, not a simulation log.
- **Hearts and HurtTick join `SimState.Hash`**, the HUD draws `heart-hud-off` the moment the sim
  spends one, and the hurt feedback respects the calmness floor: a 1 px, half-second camera wobble
  (pure function of `Tick - HurtTick`, byte-stable under stepping) and a balloon blink — under
  Reduced Motion the wobble is off and the blink becomes a steady dim through the window.
- **Hazard art is sheet family, not a new dialect**: the rock is the islands' grass cap over a
  `rock`/`rockD` taper, the gull is a cloud-bird (cream body, lavender underside, flame-orange beak)
  flapping on the shared decoration clock, and the storm reuses the fair-weather cloud lobes with a
  bruised underbelly, closed ink underside, eye slits and a hanging spark. The bank's drawn extent
  *is* its box; a three-chevron arrow under it shows which way the gust pushes this tick.

## Decisions recorded by I10 (content freeze)

- **Display names followed PLAN, ids did not.** The courses are **Nursery Skies**, **Toy Isles**
  (opposing bands) and **Cloud Garden** (strong wind + hazards) on every tag and slat, while the
  harness ids stay `nursery-skies` / `winding-trail` / `night-crossing` — committed tests, verbs and
  snapshot anchors keep their handles.
- **The world map sits between courses, not before them.** A delivered flight ends on the panel;
  R or Enter opens `worldmap` — the three islands as chart nodes, a dotted route, stars earned
  painted under each carried-home course, and the balloon parked at the next isle. `FLY ON` (Enter,
  R, or the slat) advances the run; Escape walks to the menu; after the last course the chart
  wraps, so the voyage never dead-ends. `screen('worldmap')` reaches it like any screen.
- **R's meaning sharpens by outcome**: delivered → the chart; anything else (including out of
  hearts) → replay the same sky. Running out of hearts no longer *skips* a course the way plain
  `Complete` used to — the check is Delivered, not Complete.
- **Voyage progress is session-scoped rendering state** (`AppProgress`): best stars per course,
  written from Update the instant a run is home — scripted `deliver` counts exactly like a flown
  one, and the sim hash never sees it. A reload starts the voyage fresh, like a scrap of chart
  paper lost between sessions.
- **The whole voyage is a Playwright test**: three courses delivered start-to-finish in WebGL
  through the real keyboard (R, Enter), ending wrapped on the first course with fresh hearts.

## Decisions recorded by I11 (audio)

- **The kit is generated, not fetched.** `node audio/.audio/generate.js` synthesizes every sample —
  square/triangle/noise chiptune voices at 11.025 kHz mono (~330 KB for all seven clips) — and the
  WAVs embed into the Game assembly beside the sprite sheet. PLAN named ElevenLabs; this machine
  has no ElevenLabs access, so the kit ships procedural-first (a documented deviation in
  `assets/audio/SOURCES.md`): swapping in an ElevenLabs render later is a file drop at the same
  clip name, nothing in the game changes.
- **Mute-first architecture**: nothing loads until a real gesture (first pointer press or key edge)
  unlocks the kit; from then on every call is volume-gated and try/caught, and any single failure
  retires the whole kit rather than spamming a broken call per frame. A scripted test bed that only
  pokes the harness never touches audio at all — which is also why the boot path stays error-free.
- **The director reads state edges, renderer side**: burner breathes while `Heating` (the one
  sound that is a state, not an event), creak on bounce edges, thump on ballast drops, giggle when
  a heart goes missing (she laughs it off), jingle + three staggered chimes on delivery. Music runs
  everywhere but behind the pause veil and on the test beds. Sim hash never sees any of it.
- **Volumes are live**: the settings sliders (0..8) re-echo into the playing instances the frame
  they move; sfx at zero silences the burner loop and every one-shot, music at zero only the tune.
- **`audio()` proves it woke**: the smoke test asserts the kit is asleep at boot
  (`unlocked=0;...music=none`), a title-screen press wakes it into `music=playing`, and `broken`
  stays 0 with zero console errors — the KNI 4.3 WebAudio backend works in Chromium.
- **Protocol bumped to `i11`** for the new verb.

## Decisions recorded by I12 (juice + performance)

- **Every decoration is a pure function of `Tick`/`Clock`** — burner puffs (a rolling 30-tick vent
  cycle while heating), the star-mote celebration ring (`Tick - CompletedAtTick`, so it animates
  live and freezes exactly where a paused screenshot caught it), sand-release dust and the falling
  bag's grain trail, and the one-px load lean read straight off `VelocityY`. Stepped frames stay
  byte-identical with juice on; nothing here can perturb a replay.
- **The ink wash between screens is UI transition state** — wall-clock like hover and focus, ~100 ms
  of lifting veil when a new screen is presented, absent under Reduced Motion. Screenshots settle
  past it before shooting, so baselines never race a transition.
- **The calmness floor held**: puffs and sparkles are 1–3 px dots near the (still) balloon, the lean
  is one design pixel tied to real velocity, and Reduced Motion removes the lean, the puffs, the
  twinkle and the wash. Nothing new moves the background.
- **Measured budgets (2026-10-17, I12 pass, served localhost):** boot-to-title **565 ms**;
  transferred payload **3.13 MB over 76 requests** (audio kit included, budget 8 MB); average rAF
  frame time in live heated play **16.67 ms** with p95 16.80 ms — a steady 60 fps in the Chromium
  tab. `tests/budget.spec.js` logs the first two and asserts all three every suite run.

## Current recovery and route contracts (I17–I20, October 17)

Every ballast loadout now cools into a landable descent (gravity 84, each bag 8, landing limit 24 px/s).
Travel beyond a course margin returns the craft to a safe approach without spending health or restoring
bags. Cargo goes from waiting to carried to delivered and is consumed only at the matching recipient.
`state()` mirrors cargo, pickup tick, recipient, recovery count, wind band, heat trend and approach.
`raw('fly', '120')` flies with normal rock/bird/storm collisions. `deliver()` retains the original
wind-only diagnostic. Core tests record actual heat/ballast inputs and replay them with normal
collisions, requiring matching full state hashes on every course. Nursery's active route loses no hearts.
The measured human feel gate remains open until Steven reports on the desktop build.
# I23 scenery contract

The shared `SkyBackdrop` draws daytime sky, distant Aseprite islands and a cloud sea behind the
collidable world. `raw('backdrop')` reports `floor=clouds`, `atmosphere=day` and decorative drift in
design pixels. Drift is 0.6 pixels per second (ticks divided by 60); Reduced Motion stops it. Wind
streaks use seconds too. Drawing leaves the simulation hash unchanged. Sky evidence covers low,
middle, ceiling and arrival views at both required sizes in `23-sky-*`.
# I24 UI contract

`raw('ui')` exposes the actual shared pause/result rectangles and current essential type scale.
Body text uses scale 2 at the design width and scale 3 when the canvas is reduced below it. The
world HUD shows cargo and destination; rating is reserved for the result panel. A 272×36 result
button supports pointer input and Enter. Screenshot evidence: `24-readable-flight-*` and
`24-delivery-action-*`. Menu layouts receive the same type treatment in I25.
# I25 menu contract

The title logo is a generated Aseprite slice, with the same hero and bunny used in flight. All menu
actions now use shared `UiLayout` bounds, exposed through `raw('ui')`; tests click the actual controls.
Settings, pause, credits and illustrated course choices use the adaptive body/heading scale. Controls
are at least 32 design pixels tall. `25-*` records every menu at desktop and narrow sizes. Screenshot
review copies wait 16 rendered frames so the six-frame transition has settled before export.
# I26 save and voyage contract

WebGL stores versioned best ratings and settings in `localStorage['balloonatic.save.v1']` after changes,
with a 400 ms polling interval. Storage denial falls back to session play. Save loading validates the
whole schema and value ranges before applying anything; unknown versions/keys and invalid values leave
the running data unchanged. `raw('progress')` reports actual saved completion count. Selecting the last
course directly does not imply earlier deliveries. `raw('save')`/`raw('loadsave', value)` expose the same
format for verification; `flushSave()` flushes browser storage when needed. Desktop uses session records.

After the last course, the map leads home to the dedication. Replay explicitly launches a fresh first
flight, preserving best ratings. All player-facing course launches reset completion/cargo/hearts via
the normal restart path. Screenshot evidence: `26-voyage-record-*`, `26-sweet-dreams-*`.
# I27 opposing-wind route

Toy Isles has a homeward low band, forward middle band, return band and optional fast high band. A
perch marks carrying air. The control hint and `state.hint` say `RISE FOR > WIND` when the player needs
that first climb. Normal-collision legal replays prove a soft teddy delivery across multiple seeds.
The test pilot predicts cooling through wind reversals instead of extrapolating constant horizontal
velocity; its recorded input is replayed through ordinary `SimRules.Step` and compared by full hash.
The objective panel shields text from world marks; four-band instruments use distinct M1/M2 labels.

Parcel assignment now follows the reference's delivery examples: nursery bottle, Toy Isles teddy;
Cloud Garden's letter is added in I28. Earlier I19 screenshots/notes describe its original teddy MVP.
# I28 garden route

Cloud Garden delivers a letter to the garden windmill. Its first carrying band crosses a visible
gust bank at x380–500 / altitude210–236; flowers mark two resting perches. `state.hint` warns before
entry, `state.stormWind` reports the effective gust and becomes zero with Wind Assist. Three seeded
normal-collision recordings prove the flight actually meets the bank and still lands; replay hashes
match. The mill has two Aseprite frames and freezes under Reduced Motion. Recipient messages sit above
the buildings rather than over their defining details. Evidence: `28-garden-*` at both sizes.
