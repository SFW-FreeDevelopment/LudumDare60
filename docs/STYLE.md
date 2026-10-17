# Charlotte's Balloonatic — STYLE (locked)

The source of truth for every visual decision (AGENTS.md makes it one). The palette is **sampled from
`docs/reference/concept-sheet.png`** — `tools/sample-palette.ps1` regenerates the evidence in
`docs/reference/palette.txt` — and it exists in code in exactly one place:
`Balloonatic/Balloonatic.Game/Theme.cs`. No other C# file may hold a colour literal, and no screen may
draw a shape the kit does not own. If a screenshot and this file disagree, the screenshot is wrong.

## Resolution and scaling (locked)

| Decision | Value |
| --- | --- |
| Design canvas | **640 × 360** logical pixels, everything pixel-snapped to integer coordinates |
| Upscaling | nearest-neighbour, **whole-number scale while it fits** (1280×720 = ×2) |
| Small viewports | fractional scale allowed only below ×1 (a phone in the entry page) |
| Letterboxing | render-time, always, in `DesignCanvas`; the bars are `Theme.LetterBox` |
| Page canvas | sized to the page's CSS pixels by `shell.js`, which then tells the head that size |
| Two viewports under test | 1280×720 and 420×760 — both get screenshots at every initiative |

## Palette (locked)

37 named values in `Theme.cs`, grouped by what they paint. The names below are the names in code.

| Group | Name → hex |
| --- | --- |
| Chrome (wood, paper, ink) | `Ink` #21272B · `PanelBrown` #492C23 · `WoodDark` #372729 · `WoodMid` #8C4F2A · `WoodLight` #BD733E · `WickerDark` #815A3E · `WickerLight` #A8774A · `TagCream` #F1DBAB · `Cream` #F4EBD9 |
| Sky, by altitude | `NightIndigo` #1D1D44 · `NightBlue` #324787 · `SkyBlue` #4D8ACD · `SkyPale` #79A2E8 · `DuskTeal` #69BAC5 |
| Cloud mass | `CloudLavender` #B7B8E7 · `CloudWhite` #F5F6F9 |
| Balloon and burner | `BalloonPinkLight` #F4AEB9 · `BalloonPink` #EF7C8D · `BalloonRed` #D1244B · `FlameYellow` #FFC42E · `FlameOrange` #FF9321 · `FlameRed` #E84E1B |
| World | `LeafGreen` #36A565 · `GrassLight` #7CD487 · `MossGreen` #237A54 · `RockSlate` #5D769B · `RockDark` #3C4972 · `WaterPale` #7FD7E0 · `WaterCyan` #5EC6F1 |
| Charlotte and her things | `Skin` #F6D5B8 · `HairBrown` #8D5726 · `SandTan` #D2B383 |
| Information only | `StarGold` #F1BF37 (score, rating) · `HeartPink` #F73E6A (hearts) · `HeartEmpty` #74534F · `WindLowBand` #B7B8E7 · `WindMidBand` #FFC42E · `WindHighBand` #5EC6F1 |
| Frame | `Dim` #2A1F1C (meter wells) · `LetterBox` #0B0E17 |

Rules about them, not just a list:

- **`Ink` outlines every UI shape.** Nothing sits unoutlined on sky; that is what makes the sheet's art read
  as one picture, and it is the cheapest way to keep new art on-style.
- **Saturated colour is information.** Gold means score or rating, pink means a life, the flame ramp means
  burner heat, and the wind ramp (lavender → gold → cyan) means *which altitude band you are in*. Nothing
  decorative may borrow them, or the readouts stop meaning anything.
- **One light direction, always.** Anything raised uses `Theme.WoodRamp`: lit top and left, shaded bottom
  and right. Panels, buttons, slats, baskets and gauges all modelled the same way.
- No gradients except inside the heat gauge, no alpha below 0.85 except cloud haze, no anti-aliasing
  anywhere, no pure `#FFFFFF` and no pure `#000000`.

## Old Tech — carried by the machine, not by a filter

The theme is the balloon's 1890s technology: **burner flame, temperature gauge, sandbags, rope and wicker,
windsocks and wooden platforms**, drawn in the same 16-bit style as everything else, and visible on the
menu, the HUD and the game-over screen — not only during play.

Banned outright (docs/CONCEPT.md Amendment 2): scanlines, phosphor or bloom glow, curved glass, chromatic
aberration, dithered overlays, CRT green or amber. The entry must not look like a photograph of a screen.

## Type

One hand-authored **5×7 pixel font** (`Rendering/PixelFont.cs`): no font file in the payload, no content
pipeline dependency, identical pixels on desktop and WebGL. Lowercase input maps to uppercase; UI copy is
uppercase, like the sheet's own menus. The display lettering that needs personality — the logo — is drawn
as art in I8, not set in a font.

| Role | Font scale | Design px | Used for |
| --- | --- | --- | --- |
| Display | 4 | 20 | title logo (art, not type) |
| Title | 3 | 15 | screen titles, "DELIVERED!", ratings |
| Body | **2** | 10 | HUD labels, menu items — the minimum size for anything the player must read |
| Micro | 1 | 5 | legends, digits inside a gauge, nothing at 420 px width |

Every string over wood or sky gets a **one-pixel hard shadow in `Ink`, down and right** — never a blur.

## Grid, spacing, silhouette

- 8 px grid; positions and sizes are multiples of 8 wherever a component allows.
- Borders 2 px. Corners cut 1–2 px (`DesignCanvas.CutRectangle`) — the silhouette language for panels,
  meters, tags, buttons and the balloon envelope.
- Panel padding 8 px; buttons 16 px tall; HUD margin 8 px; gaps between HUD blocks 8 px.

## The kit — the only UI vocabulary

`Balloonatic/Balloonatic.Game/Rendering/Kit.cs`, used by every screen:

| Component | What it is | Where it appears |
| --- | --- | --- |
| `Panel` / `Tag` | wood-framed panel with a cream paper label | every screen's framing, section headings |
| `Button` | wooden slat with rest / hover / press, plus a cream focus ring in the 1 px gap outside | menus, settings |
| `HeatMeter` | vertical gauge, gradient fill, cream tick marks, flame cap while firing | HUD, style sheet |
| `Flame` | three-band flicker driven by tick, not wall clock | burner, gauges |
| `Hearts` / `Stars` / `Sandbags` | 8 px readout shapes with lit and spent states | HUD, delivery rating |
| `WindChevrons` | count = strength, point = direction, colour = altitude band | HUD, style sheet |
| `Windsock` | the same information in the world, on a post | levels, tutorial islands |
| `Balloon` | striped envelope, wicker basket, Charlotte and the bunny, burner flame | gameplay, menus |
| `Cloud` / `Island` | parallax cloud puffs, floating island with grass cap and rock body | world layers |

The style sheet screen (`__GAME.screen('style')`) draws all of it in one picture; committed screenshots of
it are what "on-palette" is checked against.

## Motion

| Event | Duration | Shape |
| --- | --- | --- |
| Hover | 90 ms | face lightens one ramp step, `>` marker slides in |
| Press | 1 frame + 2 frames | slat sinks 1 px into its own shadow |
| Screen change | 180 ms | slide 8 px + fade |
| Damage | 120 ms flash, 1.2 s invulnerability flicker | tint + blink |
| Delivery | 400 ms | rise, star sparkles, rating counts up |
| Wind gust | 600 ms | chevrons lengthen, windsock straightens |

Everything visible derives from the simulation tick rather than wall clock, so a stepped frame is
byte-reproducible. The **Reduced Motion** setting collapses slides and flashes to cross-fades and stops
screen shake.

## Judgement checklist (run at every initiative's screenshot review)

- [ ] Every colour traceable to `Theme.cs`; compare against `docs/screenshots/01-style-*.png`.
- [ ] Anything the player must read is ≥ 10 design px, shadowed, unclipped, not overlapping.
- [ ] Ink outlines present, one light direction, no default font, no default widget, no white flash.
- [ ] Old Tech readable in the frame itself — and no CRT artefacts sneaking in.
- [ ] 420×760 reviewed alongside 1280×720 whenever the player reads or presses something.
