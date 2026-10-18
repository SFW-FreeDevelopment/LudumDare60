# Charlotte's Balloonatic — assessment and improvement plan

Assessed October 17, 2026. Baseline: `fdaa674`, plus the pre-existing screenshot changes in the working tree. This is a planning pass; no game implementation has been changed.

This document preserves the pre-improvement findings. For the implemented recovery work, reviewed
screenshots and current gates, see [../PLAN.md](../PLAN.md) and [SUBMISSION.md](SUBMISSION.md).

## Verdict

The technical foundation is worth keeping. The game implements altitude-driven steering, deterministic simulation, browser delivery, menus, hazards, and a useful test bridge. But the experience currently reads as a balloon navigation test course. The reference promises a small, magical delivery adventure with Charlotte and her bunny as its heart.

The largest gaps are **art direction and composition, a missing meaningful delivery loop, and flight rules that are hard to learn and recover from**. More levels and more particles will not close those gaps. Build one excellent Nursery Skies delivery, judge it against the reference, then extend that standard to the other two stages.

## Evidence and limits

- Read the [shared conversation](https://chatgpt.com/share/6ad40d46-4df0-83ea-abfd-48d239423fe3) in the browser, the concept sheet, `docs/CONCEPT.md`, `docs/STYLE.md`, the work queue, implementation, and tests. The conversation's relevant design discussion emphasizes a recognizable Charlotte and bunny, packages and destinations, wind navigation, and a gentle soundtrack. Its instructions to an earlier agent are historical source material, not new authorization to implement features.
- Ran `npm run check`: Release build, unit tests, published WebGL build, and Chromium suite passed. Independently confirmed **0 build warnings/errors, 58 unit tests, and 78 Playwright tests across both viewports**. Fresh localhost boot measured 534 ms and 3,286,484 transferred bytes (~3.13 MiB); these are local measurements, not an internet loading guarantee.
- Opened the desktop head, launched a flight, and observed the idle failure state. Ran deterministic WebGL probes for cooling, ballast, delivery, and out-of-course state. This is not a substitute for Steven judging sustained flight feel at the keyboard. Audio playback state was inspected; musical quality was not judged by listening.
- Captured and opened `docs/screenshots/15-assessment-*.png`: menu, launch, flight, delivery, map, settings, and narrow menu/launch. Review: no conspicuous desktop panel overlap; the title obscures part of its balloon; much of the flight view is flat empty ground/sky; the map repeats nearly identical islands; narrow text loses readable strokes. These are current-state evidence, not approved visual targets.
- Existing checks are valuable regression protection. They do not certify concept fidelity, human readability, enjoyable flight, or full-route survival with hazards enabled.

## What to retain

Keep KNI, the thin desktop/WebGL heads, the pure fixed-step core, seeded replay, the JS bridge, shared UI components, reproducible sprite/audio sources, low payload, and the existing tests. Retain heat/cooling/ballast/wind as the control vocabulary and preserve the recorded motion-sickness constraints. No engine migration, direct horizontal steering, CRT filter, general-purpose content editor, or wholesale framework rewrite is warranted.

## Findings

| Area | Current evidence | Improvement and player benefit |
| --- | --- | --- |
| Charlotte and bunny | Tiny square face, largely static pose; the same craft sprite does most storytelling. | Rounded chibi silhouette, readable brown hair/pink outfit/cream bunny, a few strong expressions shared by portrait and basket. Make the passenger someone the player cares about. |
| Balloon | Dense pink/cream checker detail, flat broad envelope and rigid basket. | Larger coherent fabric panels, deliberate shade clusters, rounded crown, visible ropes/burner/heart/sandbags. Match the reference's shapes before adding texture. |
| World | Two reusable island sprites, circular trees, sparse clouds, hard horizontal sky stripes, large brown ground fill. | Layered cloud sea, distant islands, warm architecture, asymmetrical rock masses, nursery landmarks and an identifiable destination. Keep the flight corridor clear. |
| Levels | Different wind/hazard data, essentially the same two-deck layout and art. | A distinct navigation lesson and visual landmark per stage. Nursery, toys and gardens should be identifiable without reading their names. |
| Delivery | `Delivered` is set by soft landing at `CourseLength`; no cargo identity, pickup state, or recipient requirement. | Explicit waiting → carried → delivered state; package visible on deck/in basket; a recipient asks for it and reacts. Give the trip a purpose. |
| Stars/rating | `Stars` is a completion rating (delivery + par + no bounce); no collectible-star system. HUD shows zero throughout flight. | Separate optional collectibles from medals if time permits; otherwise remove the misleading in-flight collectible counter. Prioritize clear delivery success over a speed grade. |
| Wind | Current-band arrows and pips; no clear preview of neighboring directions or altitude thresholds. | Small vertical wind chart with balloon marker, directional patterns, transition cue, and matching world windsocks. Let players plan an altitude change. |
| Landing | Only downward speed and basket-center x are checked; horizontal speed/footprint are ignored. Failure feedback says only “TOO FAST!”. | Preview safe approach/descent, clearly mark the deck, and make forgiveness deliberate. Do not add a strict horizontal-speed rule without giving players a usable way to slow down. |
| Menus | Text-box title, repeated sky/ground background, long controls panel, course select exposes band/gust configuration. | Illustrated hero composition and logo, strong Start Adventure action, short contextual teaching, illustrated destinations. Keep engineering configuration out of player-facing copy. |
| HUD/type | Most `PixelFont` calls use default scale 1 (7 design px), despite the style guide's body scale 2. | Fewer labels, scale-2 essential text, clear cargo objective, meaningful heat/ballast state, larger pause target, proper hierarchy. |
| Narrow viewport | A 640×360 scene shrinks to ~420×236, only 31% of a 420×760 view's height. A 7 px glyph becomes ~4.6 px tall. | Treat narrow readability as a layout issue. Compact/reflow UI and use larger text. Choose a dedicated narrow presentation only if it fits the jam; avoid promising phone play without controls. |
| Progression | All courses selectable; progress lives in a static session dictionary; finishing the last loops straight back to the first. | Remember settings/best results, show a clear next destination, and end with a short Charlotte/bunny dedication. Keep replay freely available. |
| Audio | 11.25 s, 112 bpm procedural square/triangle loop documented as chiptune; giggle plays on losing health. | Music-box/toy-piano warmth, restrained wind/burner/creak, joyful reactions to success and gentle concern on damage. Audio style should support the dream world. Listening approval remains necessary. |

### Confirmed gameplay and settings problems

1. **Both bags dropped creates a calm-air descent trap.** Ambient lift is `0.30 × 210 = 63`; empty weight is 62. Even completely cold, the craft accelerates upward and settles around **+0.57 design px/s**. A WebGL probe from altitude 220 reached 221.78 after ~4 s without heat. Cooling cannot bring it back to the landing deck. Rebalance permanent weight loss so every loadout retains a controllable descent. Prefer this to adding a vent immediately.
2. **Cold descent and landing guidance disagree.** Two bags settle at **−24.55 px/s**, beyond the 18 px/s landing limit; one bag settles at **−11.99 px/s**. The autopilot spends one bag as its landing strategy, and docs call bags “the brake,” while the concept presents an emergency ascent. Feathering the burner can also slow a loaded descent, so a bag is not mathematically mandatory for every landing. Teach the actual choices and make normal landing possible without spending an emergency resource.
3. **Thermal lag is substantial and poorly signposted.** After 4 s of heat, a probe was at altitude 122.90 and climbing at 56.07 px/s. After 3 s with heat released it was at 235.01 and still climbing at 13.37 px/s. Preserve some anticipation, but teach it and tune reversal against a human approach task, not just “heating rises.”
4. **Volume 9 breaks audio.** Reproduced through the real settings UI: at 8, `audio()` reported `broken=0`; at 9 and 10 it reported `broken=1`. `VolumeMax` is 10, while `Sound.ApplyVolumes` and one-shots divide by 8. The failure is caught, so console-error smoke tests stay green. Burner volume also follows MusicVolume rather than SoundVolume. Normalize against one maximum, use independent buses, and test every endpoint/mute transition after a gesture.
5. **There is no useful course recovery boundary.** Simulation clamps the ground at zero but not horizontal travel; camera bounds do not constrain the craft. A debug probe at x=−500 remained off-course after 10 s with no recovery transition. This establishes missing protection, not the frequency of a naturally occurring failure. Add a forgiving return/checkpoint rule and test reachable overshoots plus exhausted ballast.
6. **The route proof turns hazards off.** `Autopilot.Deliver` and its look-ahead simulations call `SimRules.Step(..., hazards: false)`. Existing hazard tests prove individual hits, while delivery tests prove a wind route. They do not prove a playable route through the live hazard field. Preserve the wind proof, add at least one recorded player-legal route per stage with normal collisions, and state the distinction honestly.

### Documentation and completion claims need reconciliation

`STYLE.md`'s locked color table differs from current `Theme.cs`; several draw paths contain color literals outside Theme. Type-scale and shadow promises exceed actual use. I8 is ticked despite its own status deferring the logo to I6, which is also ticked; the current title remains font text. I11 has working procedural audio but is unticked and names ElevenLabs; credits still say sound is arriving. I13 has packaging documentation but remains unticked. Keep history, reconcile claims against observable behavior, and retain listening/submission gates until genuinely verified. Do not spend jam time renaming the architecture to match the old prose.

## Target for the first improved slice

In one short Nursery Skies flight, Charlotte and her bunny carry a teddy home to a recognizable nursery. The launch view shows the request and destination direction. A safe opening teaches “hold to rise, release early to settle”; an altitude marker shows which breeze will carry the basket forward or back. The destination is visually distinct, the approach communicates whether descent is gentle, and touchdown reunites the teddy with its owner. Charlotte smiles; a brief musical phrase celebrates; the next destination is offered clearly.

Three frames establish the visual bar: **title, mid-flight with destination, and delivery**. Each should read as the same world as the concept sheet at thumbnail size: pink rounded hero, warm wicker and wood, green/violet islands, cream clouds, blue depth, nursery architecture. Simplify the reference's detail, not its shape language, warmth, or composition. Do not paste the sheet into the background or treat a palette audit as art approval.

## Execution plan

The executable checkboxes are in `PLAN.md` (I16–I30). Estimates include focused verification and one small revision; keep every initiative within ~90 minutes. Asset time is uncertain: if a set does not reach the bar within its timebox, reduce its variations and keep one strong landmark.

| Order | Initiative | Timebox | Concrete scope and acceptance |
| --- | --- | --- | --- |
| 1 | I16 — Audio settings reliability | 60m | Normalize 0–10, correct burner bus/mute behavior, expose enough status to assert it. Browser test sweeps all values and checks `broken=0`, including music off/SFX on and the inverse. |
| 2 | I17 — Recoverable flight | 90m | Tune weight/lift/cooling so 0/1/2 bags can descend and land; keep ballast visibly useful. Add soft recovery outside the playable envelope. Core and browser regressions cover all loadouts, a missed approach, restart and overshoot. Steven flies launch/reversal/landing before proceeding. |
| 3 | I18 — Readable wind and approach | 90m | Shared wind ladder with current marker/neighbor arrows, world boundary cues, burner/cooling feedback, and a gentle/fast landing cue. No numeric debug wall or constantly moving forecast. Test marker transitions and approach states through the bridge; judge uncluttered screenshots. |
| 4 | I19 — Real delivery state | 90m | One teddy, pickup on the launch deck, carried sprite, matching nursery recipient, landing consumes cargo once. Expose cargo/objective state; prevent empty/wrong-target completion. Show request, pickup, and gratitude. Use existing shapes as temporary art until I21–I22. |
| 5 | I20 — Nursery tutorial route | 90m | One short authored route with a safe launch, useful altitude choice, readable destination and generous recovery. No forced damage in the teaching corridor. Record a normal-collision legal input replay, distinguish it from the wind-only autopilot proof. Fresh player should deliver within a few attempts; timing is a playtest target, not an automated boot-style assertion. |
| 6 | I21 — Charlotte and craft | 90m | Rework one envelope/basket silhouette, Charlotte/bunny portrait, and a minimal idle/happy/concerned set through the existing generator. Remove checker noise; preserve clear collision silhouette and burner/ropes/bags. Review at actual gameplay size, not only a zoomed sprite sheet. |
| 7 | I22 — Nursery environment assets | 90m | One nursery landmark, one small cottage/recipient, modular grass/rock/vine clusters, and rounded cloud variants. Keep `.aseprite`, export and generator in the repo. Review a small diorama; drop extra props before accepting weak silhouettes. |
| 8 | I23 — Compose the sky | 90m | Place those assets into a layered launch/route/arrival scene; remove the dominant ground slab; soften altitude transitions with stepped cloud/depth forms. Protect wind/hazard/deck contrast and existing calm parallax. Judge launch, high/low flight, approach and delivery, not one flattering pose. |
| 9 | I24 — Type, HUD and narrow readability | 90m | Shared type/layout correction and a cargo-led HUD with fewer labels. Keep world scaling rules; adapt UI for narrow view. Test focus/hit targets at both required sizes. Essential text must actually be readable at 420 wide. |
| 10 | I25 — Title and menu composition | 90m | One compact illustrated title/logo, clear Start Adventure and dedication, fewer controls on title, and illustrated course choices instead of configuration strings. Reuse the approved hero/landmarks and shared kit. Test real keyboard/pointer navigation at both sizes. |
| 11 | I26 — Voyage and closure | 60m | Distinct destination thumbnails, honest completed count (not inferred from selected level index), save best results/settings, and a simple final dedication with replay. An illustrated static ending is sufficient. Test reload, direct course selection and final completion without silently looping. |
| 12 | I27 — Toy Isles | 90m | Reuse the approved kit with blocks/toy landmark and one opposing-wind route. One visible delivery; hazards only after the lesson. Normal-collision replay and distinct screenshot required. |
| 13 | I28 — Cloud Garden | 90m | Reuse kit with garden/windmill landmark and one clearly telegraphed gust challenge. One delivery, useful shelter/approach. Normal-collision replay and distinct screenshot required. |
| 14 | I29 — Dream-world sound | 60m | Replace the most distracting loop/timbres with gentle music-box character and a restrained mix; move giggle to positive events. Use the established ElevenLabs/provenance workflow where available. Correct actual credits. Steven listens before this is done. No second music system. |
| 15 | I30 — Submission revalidation | 90m | Full gates, effect/reduced-motion replay comparison, active-hazard full voyage, settings endpoints, reload, pause/resume, focus loss, cold publish and archive-over-HTTP smoke. Re-measure transferred bytes and frame cadence; rAF timing is a cadence smoke check, not proof of GPU performance on every device. Review all new desktop/narrow shots and refresh entry copy/provenance. Reuse I13 packaging. |

I16–I25 are about **14.5 focused hours** at the full timeboxes; I26–I30 add **6.5 hours**, for **21 hours before contingency**. Re-estimate after the flight gate and first approved scene; reserve at least 2–3 hours for regressions and packaging. Use the repository's stated jam deadline when scheduling, but do not treat the original PLAN's “Now ~05:00” or remaining-hours estimate as current time.

The first delivery should be judged with Steven after I20 (controls/teaching) and after I25 (the full experience). If it is still confusing or visually far from the reference, do another bounded pass there before extending content.

## Cuts and safeguards

If the budget contracts, ship **one beautiful nursery delivery**. Omit the extra two stage reworks, elaborate map/unlocks, collectible stars, extra character frames, and elaborate ending animation before sacrificing the flight recovery fixes, readable objectives, hero/landmark art, audio correctness, or final WebGL validation. A static dedication is cheap. A screenshot full of repeated particles is not a replacement for composition.

Keep two inputs and authored wind; defer vent, fuel depletion, physics ropes, dynamic weather, five levels, elaborate story scenes, and touch controls. The sheet's “limited fuel” label is not a mandate to add a new resource. If supporting narrow layouts threatens the jam, retain an honest readable narrow shell with a landscape/fullscreen suggestion; do not call a tiny letterboxed screenshot phone support.

For each implementation initiative: one commit and push after `npm run check`, bridge assertions for changed behavior, and opened screenshots at 1280×720 plus 420×760 for UI. Rebaseline only after judging the result. A passing autopilot or identical screenshot is evidence of stability, not a substitute for the creative and human feel gates.
