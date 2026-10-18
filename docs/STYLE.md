# Charlotte's Balloonatic — STYLE

The implemented style is locked here. Palette values live in `Balloonatic/Balloonatic.Game/Theme.cs`;
Aseprite's generator uses its matching `PAL`. New player-facing UI uses `Kit`, `UiLayout`, `PixelFont`
and the shared sprite atlas. The concept sheet guides silhouette, color and mood; its panels are not
an in-game texture. Current evidence is `docs/screenshots/21-*` through `30-*`.

## Canvas and pixels

- Design space: **640×360**. Internal sprite/render density: **1280×720**.
- World positions snap to half a design pixel; UI text snaps to whole design pixels. Moving world
  signs have lettering and board baked into one Aseprite sprite, sharing the platform grid.
- Nearest-neighbor presentation fills the largest 16:9 area available, including fractional scales.
  Steven's itch.io frame correction supersedes the original whole-number upscaling rule: a 960×540
  embed uses scale1.5 rather than surrounding a 640×360 game with unused space.
- Render-time letterboxing uses `Theme.LetterBox`. The canvas follows its page/container size.
- Review at 1280×720 and 420×760. Narrow presentation still requires keyboard or mouse input.

## Palette

These are the current values from Theme.cs, including the slate-blue cliffs restored in I22.

| Group | Name / hex |
| --- | --- |
| Wood, paper, ink | `Ink` #0B1129 · `PanelBrown` #372229 · `WoodDark` #653A38 · `WoodMid` #B7633D · `WoodLight` #BD733E · `WickerDark` #875449 · `WickerLight` #D2B383 · `TagCream` #FBE8C7 · `SignCream` #F6C28B · `Cream` #FAF6F9 |
| Sky | `NightIndigo` #1D1D44 · `NightBlue` #072467 · `SkyHigh` #1849AA · `SkyMid` #3775D7 · `SkyLow` #57A7FA · `SkyPale` #87A8F8 · `DuskTeal` #69BAC5 |
| Clouds | `CloudLavender` #D7D7FB · `CloudShade` #B6B8FA · `CloudWhite` #FAF6F9 |
| Balloon / flame | `BalloonCream` #FAF6F9 · `BalloonPinkLight` #FB9796 · `BalloonPink` #F96777 · `BalloonRed` #D1244B · `BalloonPlum` #A92B64 · `FlameYellow` #FFC21D · `FlameOrange` #FF9321 · `FlameRed` #E84E1B |
| Islands / water | `LeafGreen` #36A565 · `GrassLight` #7CD487 · `MossGreen` #237A54 · `BarkBrown` #8D5726 · `RockSlate` #5D769B · `RockDark` #585687 · `WaterPale` #7FD7E0 · `WaterCyan` #5EC6F1 |
| Charlotte / objects | `Skin` #F6C28B · `SkinShadow` #F48558 · `HairBrown` #8D5726 · `DressPink` #F974B5 · `SandTan` #D2B383 |
| Instruments | `StarGold` #F1BF37 · `HeartPink` #F73E6A · `HeartEmpty` #74534F · `WindLowBand` #D7D7FB · `WindMidBand` #FFC21D · `WindHighBand` #5EC6F1 |
| Wells / frame | `Dim` #241A1E · `LetterBox` #071022 |

Art pixels are opaque or transparent, palette-audited after every export. UI colors come from Theme.
Pause uses the ink veil at alpha150; the six-frame screen transition uses ink alpha140 down to zero.
Reduced-motion damage uses neutral RGB150 sprite modulation instead of blinking. White in the canvas
renderer is an identity texture/tint, rather than a visual palette color. The heat gauge is the sole
gradient; other shading uses discrete clusters, ramps and outlined shapes.

Within the HUD, pink reads as hearts, gold as rating/attention and lavender/gold/cyan identify wind.
Fabric pink, brass gold and flower colors also belong naturally to their world objects. Raised wood
uses lit top/left and shaded bottom/right edges; panel and glyph shadows use Ink.

## Subject and art

Old Tech reads through burner flame, thermometer, rope, wicker, sandbags, wooden decks and windsocks.
No CRT, scanlines, phosphor, chromatic aberration, curved-glass filter, bloom or dither overlay.

The hero is a rounded pink/cream balloon with visible rigging, a heart basket, Charlotte and her bunny.
Idle, happy and worried expressions share the portrait and full craft. Carried parcels use distinct
bottle/teddy/letter sprites. A nursery house, alphabet blocks and a flowered windmill identify the three
destinations in flight, on the course selector and on the map. Blue/purple tapering cliffs, greenery,
small flowers and cyan waterfalls sit over a sea of clouds. Dark gust clouds have a gold marker.

All raster art comes from the committed deterministic generator through Aseprite MCP, with named
slices, .aseprite source and PNG exports. `npm run art` is the re-export command. Shared selectors in
WorldArt keep parcel, landmark and reaction art consistent. The style diagnostic retains legacy kit
geometry for component inspection; player art comes from the atlas.

The WebGL cold-boot shell uses Aseprite crops from that same atlas. Its CSS palette and SVG labels
are generated from Theme and the game's 5×7 glyph masks. The shell stays visible until the renderer
and saved settings are ready, then gives way to the title; loading has no separate font or palette.

## Type and controls

The custom 5×7 pixel face is drawn in code. The title logo is a generated Aseprite slice with a cream
outline and pink shaded lettering. Body/heading scales come from Kit, rather than individual screens.

| Role | At design width or larger | Below design width |
| --- | --- | --- |
| Essential body and buttons | scale2 / 14 px high | scale3 / 21 design px high (~14 CSS px at 420 wide) |
| Screen heading | scale3 / 21 px high | scale4 / 28 design px high |
| Secondary world signs / tiny diagnostic tags | scale1–2 | nonessential; the HUD repeats the actual goal |

Text has an ink hard shadow when needed for contrast. The objective has a wood panel to keep world
marks out of its lettering. Rating appears on results and the map, rather than a zero-valued flight
counter. The flight HUD leads with cargo/destination and one short control or gust hint.

Main action buttons are 32–40 design pixels tall, with a warm lit hover face, arrow and cream keyboard
focus ring. The compact corner pause control is 32×28; the result action is 272×36. UiLayout supplies
the same bounds to drawing, hit testing and the browser bridge. Keyboard activation receives the same
screen change as a pointer press. Kit also owns a sunk pressed-state shape used in component previews.
Panels use 1–2 px cut corners, 2 px borders, an 8 px outer margin and the shared wood/paper language.

## Motion and feedback

- Simulation is fixed 60 Hz. World art reads state; it never writes physics or random state.
- Camera lead is bounded by velocity; Reduced Motion removes it and shake.
- Scenery parallax is slow (0.12–0.30); autonomous cloud drift is 0.6 design px/s and stops in Reduced Motion.
- Wind marks use seconds, direction and strength from the actual wind data.
- Screen changes use a six-render-frame ink fade (~100 ms at 60 Hz); Reduced Motion cuts directly.
- Burner, bird flaps, windsock cloth and mill sails derive from the flight clock and freeze in Reduced Motion.
- Damage uses the invulnerability blink, or a steady dim tint in Reduced Motion. Bird movement remains
  tied to collision positions. Dropped bags remain visible; decorative sand/puffs stop in Reduced Motion.
- Results freeze flight scenery at completion; delivery sparkles celebrate briefly, or form a static
  ring in Reduced Motion. Time/rating are the completed result, rather than a running counter.
- Audio feedback is gentle balloon machinery, a separate music-box loop and positive delivery cues;
  the audio provenance file owns the sound details and listening gate.

## Review

Open the screenshots. Check essential text at the actual narrow size, hero/hazard/deck separation,
landmark silhouettes, cargo/recipient clarity, focus states and unclipped actions. Green behavioral
checks do not approve aesthetics. Human flight and listening gates remain explicit in PLAN.md.
