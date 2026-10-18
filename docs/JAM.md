# Ludum Dare 60 — Jam Context

Working notes for this jam. Decisions live here until they harden into `PLAN.md` / `docs/STYLE.md`.

## Event
- **Theme:** Old Tech
- **Jam starts:** Fri Oct 16, 2026
- **Compo ends:** Sun Oct 18, 2026, 18:00 EDT
- **Jam ends:** Mon Oct 19, 2026, 18:00 EDT (rate + play period runs after)
- **Deliverable:** browser-playable WebGL build (LD's web-first entry rule) — an in-browser play link
  plus the build archive for the entry page. Confirm the exact form fields when submitting.
- **Repo:** `git@github.com:SFW-FreeDevelopment/LudumDare60.git` (same org as LD51–LD59)

## This entry
- Steven's 15th jam entry: LD 40, 41, 42, then 49–59 consecutively, now 60.
- Engine: [KNI](https://github.com/kniEngine/kni) (Kensout.NET Instrumentality), MonoGame-compatible
  C# framework with a first-class WebGL backend.

## Toolchain baseline (verified on this machine, 2026-10-17)
| Item | Value |
| --- | --- |
| .NET SDKs installed | 3.1.421, 5.0.415, 6.0.300, 8.0.100/204/403, 9.0.200, **10.0.101 (default)** |
| KNI template pack | `nkast.Kni.Templates@4.3.9001` (installed via `dotnet new install`) |
| Relevant template short names | `kni-multiplatform` (shared library + Blazor/WebGL, SDL2 desktop, WinForms, Android, Oculus heads), `kni-blazor-gl` (web only), `kni-sdl2-gl`, `kni-winforms-dx11` |
| Node / npm | v24.12.0 / 11.6.2 |
| Test driver | Playwright (Chromium) against the served WebGL build |

KNI 4.3.9001 (Sep 2026) added the `dotnet new` templates, .NET 10 support, WebGL texture improvements
(including anisotropic filtering on WebGL), and a much more complete WebAudio backend — so sound in
the browser build is viable this jam, not a risk to plan around.

**Head plan:** scaffold from `kni-multiplatform` so shared code is one class library with a fast
Windows desktop head for iteration and a Blazor WebGL head that is the actual submission.

## Decisions (2026-10-17)
1. **Build to the Jam deadline** — Mon Oct 19, 18:00 EDT. Compo is not the target.
2. **Concept:** Steven has it; it gets written here as a one-paragraph pitch before `PLAN.md` is cut.
   The mechanic must come from "Old Tech", not just dress up a generic game in a CRT filter.
3. **Art:** generated in **Aseprite** (driven through the Aseprite MCP from an idempotent generator
   script). Committed: `.aseprite` sources plus their exported PNGs under `art/`.
4. **Audio:** generated with **ElevenLabs**, committed under `assets/audio/`, with each clip's origin
   recorded in `assets/audio/SOURCES.md` so the entry page can state provenance. Watch total download
   size — audio is usually what makes a WebGL build fat.
5. **Push policy:** `main` is pushed once per completed initiative.

## Current submission status
The three-course concept and improvement queue are implemented; current technical verification and
archive details live in `docs/SUBMISSION.md`. Steven approved the initial flight feel and completed
the listening review on October 18, 2026: *"Sounds are good"*. The game is now publicly playable at
https://stevengarberg.itch.io/charlottes-balloonatic (I36). The Ludum Dare page is prepared and saved
as an unpublished draft under I38; format/checklist/opt-out review and publication remain open.
The I31–I35 playtest corrections and refreshed archive are verified under I34 (`ld60-review-v3`).
Steven confirmed the world-sign jitter is fixed; the updated preview includes the clarified rest perch.

## References
- KNI engine: https://github.com/kniEngine/kni (templates: `nkast.Kni.Templates`)
- LD60 game page / entry list: https://ldjam.com/events/ludum-dare/60/games
- LD60 rules + countdown: https://ldjam.com/events/ludum-dare/60/rules
- Steven's previous entries: https://ldjam.com/users/steven-garberg/games
