# Charlotte's Balloonatic — Concept Handoff

Received 2026-10-17 from the planning agent that produced the concept sheet. Reproduced as the
design source of truth, with two local amendments noted below. Everything else is the handoff's text.

> **Amendment 1 — engine: KNI, not Unity.** The handoff names Unity. **That is a mistake for this
> project — we build in KNI** (shared C# core + Blazor WebGL head + a Windows desktop head for
> iteration). The LD entry must be WebGL, and KNI is the toolchain verified in this repo. No Unity
> project, scene files, or Inspector work happens here. Unity idioms in §12 translate directly:
> `BalloonController`/`WindSystem`/`DeliverySystem` → plain C# systems in the pure simulation core;
> `ScriptableObjects`/Inspector tuning → per-level config objects; scenes → level data files plus a
> thin KNI draw layer.
>
> **Amendment 2 — no CRT filter.** "Old Tech" here is the *balloon's primitive technology*, not a
> screen effect. No scanlines, no phosphor glow, no chromatic aberration, no curved-glass vignette,
> no palette quantization pass over the finished frame. The look is clean 16-bit pixel art per §9.
> Where this repo's rules docs mention CRT framing, §9 + this amendment win.

---

# Charlotte's Balloonatic

**Project:** Charlotte's Balloonatic
**Event:** Ludum Dare 60
**Theme:** Old Tech
**Developer:** GigaBite Games
**Engine:** Unity (preferred) → **KNI + WebGL in this build** (Amendment 1)
**Genre:** 2D Physics-Based Arcade / Precision Navigation / Delivery
**Visual Style:** Colorful, whimsical 16-bit-inspired pixel art
**Target Scope:** Playable and polished within a 48–72-hour game jam
**Status:** Concept and planning phase

## 1. Project Overview

**Charlotte's Balloonatic** is a charming 2D arcade game about a baby named Charlotte who travels
between floating islands in an old-fashioned hot-air balloon, accompanied by her stuffed bunny.

The player must navigate changing wind currents, manage altitude, avoid hazards, collect packages,
and deliver them to destinations scattered across a whimsical dream world.

The central gameplay hook is that **the player cannot directly control horizontal movement**.

Instead, the player must use the balloon's primitive flight technology to navigate:

- Apply heat to rise.
- Allow the balloon to cool to descend.
- Release limited ballast for emergency altitude adjustments.
- Exploit wind currents that move in different directions and at different speeds depending on altitude.
- Manage momentum to land accurately on small platforms.

The result should feel like a combination of a physics-based arcade game, a precision landing
challenge, and a cozy adventure.

**Elevator pitch:** *"One tiny passenger. One very old balloon. A whole world to explore."*

Guide baby Charlotte and her stuffed bunny through a colorful world of floating islands using nothing
but heat, ballast, and the wind.

**Tagline:** Float. Explore. Deliver Dreams.

## 2. Ludum Dare Theme Integration: Old Tech

The game must meaningfully incorporate the theme rather than simply referencing it aesthetically.
The central technology is an old-fashioned, manually operated hot-air balloon.

Unlike modern powered aircraft, this balloon has:

- No propeller.
- No engine-driven horizontal propulsion.
- No steering wheel.
- No computerized navigation.
- No direct left/right movement controls.

The player must use the physical limitations of early balloon flight as the core gameplay mechanic.

The balloon should visibly incorporate old technology:

- Wicker basket.
- Ropes and rigging.
- Fabric envelope with stitched panels.
- Simple burner mechanism.
- Manual burner valve.
- Sandbags for ballast.
- Mechanical temperature or pressure gauges.
- Windsocks and weather vanes for observing wind direction.

**Important:** The game should not become a conventional flying platformer. Its identity depends on
navigating by manipulating altitude and taking advantage of wind. The balloon technology should be
mechanically meaningful, not just visual decoration.

## 3. Story and Setting

Charlotte falls asleep in her nursery, holding her favorite stuffed bunny. In her dreams, she finds
herself aboard a magical old-fashioned hot-air balloon, floating among colorful islands in the clouds.
Her stuffed bunny accompanies her throughout the journey.

The floating islands contain whimsical locations inspired by childhood:

- Oversized toys.
- Building blocks.
- Stuffed animals.
- Musical mobiles.
- Windmills.
- Cozy cottages.
- Lighthouses.
- Cloud gardens.
- Star observatories.

Charlotte travels between these locations, helping their inhabitants by delivering lost toys,
packages, stars, and other special objects.

The story should be conveyed primarily through the environments and character animations, avoiding
extensive dialogue.

**Suggested ending:** After completing the final delivery, Charlotte floats back toward her nursery.
The camera transitions from the dream world to Charlotte sleeping peacefully in her crib. Closing
message: *"Sweet dreams, Charlotte. Love, Mom & Dad."* Optional for the jam build, desirable if time
permits.

## 4. Core Gameplay Loop

Each level follows a straightforward sequence:

1. Charlotte starts aboard her balloon at a designated launch platform.
2. The player identifies the destination and observes the wind currents.
3. The player controls the balloon's altitude to enter favorable wind layers.
4. Charlotte navigates obstacles and hazards while collecting optional stars.
5. The player collects or carries the level's required delivery item.
6. The player carefully approaches and lands on the destination platform.
7. The level ends with a delivery animation and a performance rating.

The goal is to make this loop immediately understandable while allowing increasingly difficult
navigation challenges.

### Primary Controls

**Heat / Burner** — Holding the heat button increases the balloon's internal temperature. Higher
temperature creates upward acceleration. Releasing the button allows the balloon to cool naturally.
Temperature should respond gradually rather than instantly.

**Cooling / Descent** — Without sufficient heat, the balloon begins descending. Descending should be
gradual and momentum-based. An optional vent control could allow faster cooling, but should not be
necessary for the initial prototype.

**Ballast** — The balloon begins each level with a limited number of sandbags. Dropping ballast
reduces weight, providing a temporary advantage in gaining altitude. Ballast cannot be recovered once
released. It must not cause the balloon to descend; dropping weight increases buoyancy.

**Wind** — Wind is the primary means of horizontal movement. Different altitude bands contain
different wind directions and strengths. The player must ascend or descend into the correct band to
travel horizontally. Horizontal velocity should have inertia rather than instantly matching wind speed.

### Critical Design Rule

**Do not implement conventional left/right directional movement.** The core challenge is selecting the
correct altitude and managing momentum.

## 5. Physics and Movement

The balloon should feel responsive enough for an arcade game while retaining believable momentum.
Realistic simulation is less important than satisfying gameplay.

### Suggested Simplified Model

Track: vertical position, horizontal position, vertical velocity, horizontal velocity, balloon
temperature, ballast remaining, current wind vector.

**Vertical movement** — Vertical acceleration is influenced by current balloon temperature, ambient
cooling, gravity / baseline descent, current ballast weight, and vertical drag. Heating should
gradually increase lift. The balloon should continue rising briefly after the player stops heating,
depending on its current velocity and temperature.

**Horizontal movement** — Horizontal movement is influenced by wind direction, wind strength,
horizontal drag, and current momentum. A simple model could interpolate horizontal velocity toward
the current wind layer's target velocity using configurable acceleration. Avoid instant velocity
changes when crossing between wind layers.

**Wind layers** — The first prototype should support three configurable altitude bands:

| Layer | Direction | Strength |
| --- | --- | --- |
| Low | Right | Slow |
| Middle | Left | Medium |
| High | Right | Fast |

Later levels can introduce additional variations. Wind should be visually represented using subtle
moving particles, arrows, clouds, or environmental animation.

**Implementation recommendation:** Make wind bands configurable per level rather than hardcoding their
behavior.

## 6. Deliveries and Objectives

Potential delivery types:

- **Lost Toy** — Deliver a stuffed animal to its home.
- **Baby Bottle** — Carry a bottle to a nighttime nursery.
- **Star** — Collect a fallen star and deliver it to an observatory.
- **Love Letter** — Deliver a letter to a lighthouse or cottage.
- **Special Package** — A more difficult delivery requiring careful handling or a challenging route.

**Initial jam scope:** Start with one standard delivery type. Other package types can initially be
visual variations. Avoid implementing unique physics or damage systems for every package until the main
gameplay is polished.

## 7. Hazards

**Recommended**

- **Floating Rocks** — Static obstacles; require maneuvering around or between them.
- **Birds** — Move along simple predefined paths; collisions may cause minor knockback or damage.
- **Storm Clouds** — Generate localized wind disturbances; can push the balloon off route.
- **Strong Wind Zones** — Apply greater horizontal acceleration; require anticipating momentum.

**Optional:** airships, lightning, moving mechanical platforms, rotating windmills, narrow cave passages.

Avoid introducing too many hazard types in the jam version.

## 8. Level Structure

### Level 1 — Nursery Skies
Gentle nighttime nursery dreamscape: soft clouds, hanging stars, floating cribs and nursery objects,
simple platforms, gentle wind currents. **Purpose:** teach heating, cooling, and basic landing.

### Level 2 — Toy Isles
Floating islands built from toys: oversized teddy bears, wooden blocks, toy trains, colorful play
structures. **Purpose:** introduce opposing wind currents and more complex deliveries.

### Level 3 — Cloud Garden
Bright floating gardens and windmills: lush vegetation, waterfalls, wooden bridges, moving environmental
elements. **Purpose:** introduce stronger winds and environmental obstacles.

### Level 4 — Moonlight Bay
Peaceful nighttime coastal islands: lighthouses, crescent moon, dark blue and purple skies, lantern-lit
docks, water below the clouds. **Purpose:** more precise navigation and narrow landing zones.

### Level 5 — Star Observatory
Magical observatory above the clouds: telescopes, celestial architecture, glowing stars, floating
ruins. **Purpose:** final navigation challenge combining previously learned mechanics.

**Scope note:** Five levels are a target, not a requirement. Three polished levels are preferable to
five unfinished ones.

## 9. Art Direction

Use the concept sheet as the primary visual reference (`docs/reference/concept-sheet.png`).

The desired art style is:

- Colorful 16-bit-inspired pixel art.
- Cute, expressive characters.
- Rounded, friendly proportions.
- Vibrant pastel environments.
- Strong silhouettes.
- Detailed but readable scenery.
- Soft atmospheric depth.
- Layered parallax backgrounds.
- Whimsical fantasy architecture.
- Cozy and playful presentation.

**Character design:** Charlotte is a cute, stylized baby with brown hair, wearing a pink outfit,
expressive and cheerful, clearly visible inside the balloon basket, accompanied by a small white or
cream-colored stuffed bunny. Suggested expressions: happy, surprised, excited, concerned, sleepy. The
stuffed bunny should remain recognizable and consistently accompany Charlotte.

**Balloon design:** red, pink and cream fabric envelope; rounded silhouette; brown wicker basket;
visible ropes; small decorative heart on the basket; simple burner; sandbags attached to the sides.
The balloon must stay recognizable at small gameplay sizes.

**Important art production constraint:** the concept image is aspirational key art, not a literal asset
specification. Gameplay sprites must be significantly simpler than the detailed promotional artwork.
Prioritize:

1. Character readability.
2. Balloon readability.
3. Clear collision boundaries.
4. Readable wind indicators.
5. Strong foreground/background separation.

Avoid excessive detail that interferes with gameplay.

## 10. UI and HUD

The interface should remain minimal during gameplay. Recommended HUD elements: Charlotte portrait,
remaining health or collision tolerance, collected stars, delivery objective, balloon heat indicator,
remaining ballast, current wind direction and strength.

**Heat meter:** a vertical gauge with a small flame icon, showing approximate temperature / available
lift, readable without precise numerical interpretation.

**Wind indicator:** a simple directional arrow near the top of the screen showing direction, relative
strength, and current altitude band.

**Delivery counter:** e.g. `Packages: 1/3`. For the initial prototype, each level may require only one
delivery.

**Menus:** Start Adventure, Level Select, Settings, Credits. Optional dedication: **For Charlotte ♥**.
Use the concept artwork as visual inspiration for title treatment and UI framing.

## 11. Animation and Feedback

**Charlotte:** idle blinking; gentle basket bobbing; happy reaction when collecting stars; surprised
reaction when colliding; celebratory animation on successful delivery.

**Balloon:** slight fabric deformation during ascent/descent; burner flame animation; gentle basket
swinging; sandbag release animation; subtle reaction to strong winds.

**Environment:** moving clouds; wind particles; fluttering flags; rotating windmills; sparkling stars;
waterfall animations.

**Audio direction:** light, whimsical music; soft acoustic instrumentation; music-box elements; gentle
wind ambience; burner ignition; basket creaking; star collection chimes; happy baby giggles; delivery
success jingle. Charming without becoming repetitive or distracting.

## 12. Technical Architecture

Implementation should be component-based and easy to tune (see Amendment 1 for the KNI mapping).
Suggested systems: **BalloonController** (heating, cooling, vertical acceleration, ballast, momentum,
collision responses), **WindSystem** (altitude-based wind bands, strength, direction, visualization,
localized gusts), **LevelManager** (initialization, objectives, completion, restart),
**DeliverySystem** (pickup detection, carried item, destination detection, success),
**CameraController** (smooth follow, velocity look-ahead, boundaries), **HUDController** (heat gauge,
wind indicator, objective progress, ballast inventory), **AudioManager** (music, environmental audio,
interaction SFX).

**Configuration:** expose physics parameters as data (Unity Inspector/ScriptableObjects → per-level
config objects here). Important tunables: heating rate, cooling rate, maximum temperature, lift
multiplier, gravity / baseline descent, horizontal wind acceleration, horizontal drag, vertical drag,
ballast lift contribution, wind band boundaries, wind strength. Avoid tightly coupling these values to
specific levels.

## 13. Minimum Viable Prototype

Before building menus, story sequences, or multiple levels, validate the movement mechanic. The first
prototype should contain: one balloon; a basic Charlotte placeholder sprite; one test environment; three
wind layers; heat control; natural cooling; optional ballast release; one pickup; one destination
platform; a restart button.

**Prototype success criteria:**

1. The player understands that altitude determines horizontal travel.
2. The player can intentionally move toward a target.
3. Changing wind layers feels predictable.
4. The balloon has satisfying momentum.
5. Landing requires skill but is not frustrating.
6. The player can complete a short delivery without extensive instructions.

**Do not expand scope until this feels fun.**

## 14. Development Priorities

1. **Core gameplay** — balloon physics, heat/cooling controls, wind layers, landing, delivery completion.
2. **Playability** — camera, collision feedback, restart flow, HUD, first complete level.
3. **Presentation** — Charlotte sprite, balloon artwork, backgrounds, particle effects, SFX, music.
4. **Content** — additional levels, stars, hazards, more deliveries, level selection.
5. **Polish** — title screen, transitions, character reactions, optional customization, dedication scene.

## 15. Scope Guardrails

Do not prioritize: online multiplayer, procedural world generation, extensive narrative dialogue, large
inventory systems, complex crafting, character progression trees, physics-based destruction, large-scale
world exploration, dozens of unique delivery mechanics, realistic fluid or atmospheric simulation.

The core game stays focused on one thing: **navigating a hot-air balloon through wind currents to
complete deliveries.** Everything else supports that.

## 16. Open Design Questions

Evaluated answers live in `PLAN.md` → "Design decisions"; the questions were: burner hold vs intensity;
active vent; fixed vs dynamic wind bands; landing by velocity vs trigger; collision consequence model;
stars optional or required; fixed vs scrolling camera; ballast in MVP or later; sprite resolution and
pixels-per-unit; forgiveness vs skill ceiling.

## 17. Instructions to the Planning Agent

1. Evaluate the proposed mechanics and identify technical or design risks.
2. Propose a realistic 48–72-hour development plan.
3. Design the simplest viable balloon physics implementation.
4. Define a scene/project architecture.
5. Recommend the minimum set of sprites, backgrounds, and UI assets.
6. Establish a practical milestone plan.
7. Identify what should be cut or simplified.
8. Prepare an implementation plan that begins with a playable movement prototype.

Do not begin implementing elaborate secondary systems before validating the core balloon controls.

**Creative goal:** a charming, polished, memorable game that meaningfully embraces "Old Tech" and serves
as a personal tribute to Charlotte.

**Guiding principle:** a simple game with exceptional feel and presentation is preferable to an
ambitious but unfinished game.

## Final Creative Vision

Charlotte's Balloonatic should feel like a tiny, magical childhood adventure. The player isn't piloting
a sophisticated aircraft — they're helping a little girl navigate an unpredictable sky in a beautiful but
antiquated balloon. The challenge comes from understanding the wind. The charm comes from Charlotte. And
the heart of the game is bringing a little bit of happiness to the world, one delivery at a time.

## Reference art

Concept sheet (attached to the handoff; also the source of the level themes, palette and character
design): drop it at `docs/reference/concept-sheet.png` so the Aseprite initiative can sample its palette.
