using System;
using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The screen the game is actually played on: a scrolling world where altitude is steering. The camera
    /// is a pure function of state (position plus velocity look-ahead), which gives two things at once —
    /// it looks where the player is going because it never smooths something that is already continuous,
    /// and a stepped frame is byte-stable for screenshots without any render-side state to reset. Drawing
    /// this screen reads state and never writes it; the world screenshot test proves that with a hash.
    /// </summary>
    sealed class WorldScreen : IUiScreen
    {
        // Half a second of look-ahead: enough that +75 px/s of band wind pulls the view ~37 px ahead of
        // the craft, so speed is legible as framing, not only as a gauge. Reduced Motion gives this up
        // entirely — the look-ahead is the one thing the camera does on its own initiative.
        const float LookAheadSeconds = 0.5f;

        // Corner pause tab: the mouse-only player's way out of the sky, and the only button that lives
        // on the flight screen (everything else the flight says, the HUD already says).
        static readonly Rectangle PauseTab = new Rectangle(Sim.DesignWidth - 16, 8, 12, 14);

        readonly UiHost _host;
        bool _pauseHover;

        public WorldScreen(UiHost host) => _host = host;

        public void Tick(UiFrame f)
        {
            _pauseHover = f.PointIsIn(PauseTab.X, PauseTab.Y, PauseTab.Width, PauseTab.Height);
            if (f.Back || (f.Click && _pauseHover))
                _host.GoTo(SimState.ScreenPause);
        }

        // World margins either side of the course, so launch and delivery islands sit inside the frame.
        const int WorldMargin = 120;

        // Parallax factors per layer: 1.0 is locked to the world, lower values sit further away.
        // Deliberately far below the camera speed: the world itself already sweeps past at wind speed,
        // and background layers that ride along at near-world speed make the sky feel like it is flying
        // at the player (playtest: motion sickness at 2x scale). Distance should read as slowness.
        const float StarsParallax = 0.15f;
        const float FarCloudParallax = 0.25f;
        const float MidCloudParallax = 0.40f;

        // Stepped sky ramp by altitude: the higher the balloon, the older and darker the air gets.
        static readonly (float Altitude, Color Color)[] SkyRamp =
        {
            (0f, Theme.SkyLow),
            (140f, Theme.SkyMid),
            (280f, Theme.SkyHigh),
            (420f, Theme.NightBlue),
            (560f, Theme.NightIndigo),
        };

        public string Id => SimState.ScreenWorld;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            LevelDef level = LevelCatalog.Get(state.LevelId);
            (float camX, float camAlt) = Camera(state, level);

            DrawSky(canvas, camAlt);
            DrawStars(canvas, camX, camAlt, state);
            DrawCloudLayer(canvas, level, state, camX, camAlt, far: true);
            DrawCloudLayer(canvas, level, state, camX, camAlt, far: false);
            DrawStormBanks(canvas, level, state, camX, camAlt);       // I9: angry cloud banks at mid-cloud depth
            DrawWindStreaks(canvas, level, state, camX, camAlt);
            DrawIslands(canvas, level, state, camX, camAlt);
            DrawHazards(canvas, level, state, camX, camAlt);          // I9: rocks and birds, before the balloon
            DrawFallingBags(canvas, state, camX, camAlt);
            DrawBalloon(canvas, state, camX, camAlt);
            DrawCelebration(canvas, state, camX, camAlt);            // I12: post-delivery star sparkle
            DrawHud(canvas, level, state);
            DrawPauseTab(canvas);
            DrawLandingFeedback(canvas, level, state, camX, camAlt);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        /// <summary>The pause tab, drawn like every other raised thing: ink, wood, lit when pointed at.</summary>
        void DrawPauseTab(DesignCanvas canvas)
        {
            canvas.CutRectangle(PauseTab.X, PauseTab.Y, PauseTab.Width, PauseTab.Height, 2, Theme.Ink);
            canvas.CutRectangle(PauseTab.X + 1, PauseTab.Y + 1, PauseTab.Width - 2, PauseTab.Height - 2, 1,
                _pauseHover ? Theme.WoodLight : Theme.WoodMid);
            canvas.Fill(PauseTab.X + 3, PauseTab.Y + 3, 2, 8, Theme.TagCream);
            canvas.Fill(PauseTab.X + 7, PauseTab.Y + 3, 2, 8, Theme.TagCream);
        }

        /// <summary>
        /// The two moments the landing rule has to speak for itself: a bounce says why in one red tag,
        /// and a delivery stops the world behind a panel with the three stars it earned (PLAN I5 — the
        /// reason the player failed or what they won, always on screen).
        /// </summary>
        static void DrawLandingFeedback(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            if (!state.Complete && state.Recoveries > 0 && state.Tick - state.RecoveryTick < 180)
            {
                Kit.Panel(canvas, new Rectangle(166, 292, 308, 28));
                PixelFont.DrawCenteredShadowed(canvas, "TRY ANOTHER APPROACH", 320, 300, Theme.TagCream, 2);
            }
            else if (!state.Complete && FlightGuidance.Approach(state, level) != "travel")
            {
                string approach = FlightGuidance.Approach(state, level);
                Kit.Panel(canvas, new Rectangle(166, 292, 308, 28));
                string message = approach == "fast" ? "TAP HEAT TO SLOW DESCENT" : approach == "rising" ? "RELEASE EARLY TO SETTLE" : "GENTLE APPROACH";
                PixelFont.DrawCenteredShadowed(canvas, message, 320, 300,
                    approach == "gentle" ? Theme.GrassLight : Theme.TagCream, 2);
            }
            if (state.Bounces > 0 && !state.Complete
                && state.Tick - state.BounceTick < SimTuning.BounceReasonSteps)
            {
                int bx = Round(ScreenX(state.X, camX));
                int by = Round(ScreenY(state.Altitude, camAlt)) - 66;
                PixelFont.DrawCentered(canvas, "TOO FAST!", bx, by, Theme.FlameRed);
            }

            if (!state.Complete)
                return;

            if (!state.Delivered)
            {
                // The other ending (I9): hearts spent where she stands. Same crate, gentler words —
                // losing is not a scold, and the run is one R away from being a flight again.
                int ow = 210;
                int oh = 78;
                int ox = (Sim.DesignWidth - ow) / 2;
                Kit.Panel(canvas, new Rectangle(ox, 92, ow, oh), "OUT OF HEARTS");
                PixelFont.DrawCentered(canvas, "CHARLOTTE NEEDS A REST", ox + ow / 2, 92 + 26, Theme.TagCream);
                for (int s = 0; s < 3; s++)
                    Sprites.Draw(canvas, "star-off", ox + ow / 2 - 11 + s * 8, 92 + 42);
                PixelFont.DrawCentered(canvas, "PRESS R TO FLY AGAIN", ox + ow / 2, 92 + 62, Theme.WoodLight);
                return;
            }

            int w = 210;
            int h = 96;
            int x = (Sim.DesignWidth - w) / 2;
            int y = 76;
            Kit.Panel(canvas, new Rectangle(x, y, w, h), "DELIVERY");
            PixelFont.DrawCentered(canvas, level.DeliveredMessage, x + w / 2, y + 26, Theme.TagCream);

            // Frozen clock: the delivered time, not a live count — a score that keeps climbing is not a score.
            int seconds = state.CompletedAtTick / Sim.StepHz;
            string timeLine = "TIME " + seconds + "S   PAR " + level.ParSeconds + "S";
            PixelFont.DrawCentered(canvas, timeLine, x + w / 2, y + 42, seconds <= level.ParSeconds ? Theme.GrassLight : Theme.SignCream);
            for (int s = 0; s < 3; s++)
                Sprites.Draw(canvas, s < state.Stars ? "star-on" : "star-off", x + w / 2 - 11 + s * 8, y + 55);

            // Tell the player exactly what R will do, because R does different things after a delivery
            // than it does mid-crash: the next course, or one more flight when there are no more courses.
            bool lastCourse = LevelCatalog.Levels[LevelCatalog.Levels.Length - 1].Id == state.LevelId;
            PixelFont.DrawCentered(
                canvas,
                lastCourse ? "PRESS R TO FLY AGAIN" : "PRESS R FOR NEXT LEVEL",
                x + w / 2, y + 80, Theme.WoodLight);
        }

        /// <summary>
        /// Camera centre in world coordinates. The only smoothing the game needs: the balloon's own
        /// velocity leads the view, and the edges of the world hold it back politely.
        /// </summary>
        static (float camX, float camAlt) Camera(SimState state, LevelDef level)
        {
            // Reduced Motion (I7 setting): the look-ahead goes away, so the camera only ever sits where
            // the balloon is — no lead, no sway, the calmest framing the game can show.
            float look = AppSettings.ReducedMotion ? 0f : LookAheadSeconds;
            float worldLeft = -WorldMargin;
            float worldRight = level.CourseLength + WorldMargin;
            float targetX = state.X + state.VelocityX * look;

            // Reserve the instrument strip above the envelope, including on a fast approach.
            // Bounded look-ahead keeps the camera calm and never hides the passenger behind HUD.
            float verticalLead = Clamp(state.VelocityY * look * 0.5f, -6f, 6f);
            float camAlt = Clamp(state.Altitude + verticalLead + 24f, 0f, level.WorldHeight);

            // Framing bias toward the goal: until the delivery is behind the balloon, the camera leans
            // downwind, so the course ahead stays in frame and the islands are visible from launch.
            // Pure state again — no render memory, byte-stable screenshots.
            float courseBias = state.X < level.CourseLength ? Sim.DesignWidth / 4 : 0;
            float camX = Clamp(targetX + courseBias, worldLeft + Sim.DesignWidth / 2, worldRight - Sim.DesignWidth / 2);

            // I9 hurt shake: one pixel, half a second, a pure function of (Tick - HurtTick) so a
            // stepped frame stays byte-stable. It is a bruise, not an earthquake — and Reduced
            // Motion keeps it out entirely, leaving the balloon's own flicker to carry the news.
            int hurtAge = state.Tick - state.HurtTick;
            if (!AppSettings.ReducedMotion && state.Hearts < SimTuning.HeartsStart
                && hurtAge >= 0 && hurtAge < SimTuning.ShakeTicks)
                camX += (hurtAge / 3 % 2 == 0 ? 1f : -1f) * (1f - hurtAge / (float)SimTuning.ShakeTicks);

            return (camX, camAlt);
        }

        static float ScreenX(float worldX, float camX) => worldX - camX + Sim.DesignWidth / 2f;
        static float ScreenY(float altitude, float camAlt) => Sim.DesignHeight / 2f - (altitude - camAlt);

        /// <summary>Text and only text keeps the coarse grid: type must not wobble by half a pixel.</summary>
        static int Round(float v) => (int)MathF.Round(v);

        static void DrawSky(DesignCanvas canvas, float camAlt)
        {
            // Quantised to 4 px rows: a stepped ramp reads as pixel art rather than a gradient, and the
            // 4 px quantum is cheap enough to repaint every frame on WebGL.
            for (int y = 0; y < Sim.DesignHeight; y += 4)
            {
                float altitude = camAlt + (Sim.DesignHeight / 2 - y);
                canvas.Fill(0, y, Sim.DesignWidth, 4, SkyColor(altitude));
            }
        }

        static Color SkyColor(float altitude)
        {
            for (int i = SkyRamp.Length - 1; i >= 0; i--)
                if (altitude >= SkyRamp[i].Altitude)
                    return SkyRamp[i].Color;
            return SkyRamp[0].Color;
        }

        static void DrawStars(DesignCanvas canvas, float camX, float camAlt, SimState state)
        {
            for (int i = 0; i < 40; i++)
            {
                int wx = Wrap(Hash(i, 7) % (Sim.DesignWidth * 2), Sim.DesignWidth * 2);
                float alt = 380f + Hash(i, 13) % 400f;
                float x = WrapF(wx - camX * StarsParallax, Sim.DesignWidth * 2);
                float y = ScreenY(alt, camAlt * StarsParallax);
                if (x < -2 || x > Sim.DesignWidth + 2 || y < 0 || y > Sim.DesignHeight)
                    continue;
                int twinkle = (Clock(state) / 20 + i) % 7 == 0 ? 0 : 1;
                canvas.Fill(WrapF(x, Sim.DesignWidth), y, twinkle, twinkle, i % 3 == 0 ? Theme.Cream : Theme.CloudLavender);
            }
        }

        static void DrawCloudLayer(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt, bool far)
        {
            float parallax = far ? FarCloudParallax : MidCloudParallax;
            int seedSalt = far ? 101 : 202;
            int count = far ? 7 : 5;
            int span = Sim.DesignWidth * 2;

            for (int i = 0; i < count; i++)
            {
                int wx = Hash(i, seedSalt) % span;
                float altitude = 20f + Hash(i, seedSalt + 1) % (int)level.WorldHeight;
                WindBand band = level.BandAt(altitude);

                // Clouds are objects in the sky: they drift with the band they live in, which is the
                // player's ambient wind read — and the drift is state-derived (tick), so frames are stable.
                // A slow suggestion of drift, not the band's full speed: big pale shapes crossing fast
                // are exactly what the eye cannot ignore and the stomach cannot forgive.
                float drift = Clock(state) * band.Direction * LevelDef.WindSpeed(band.Strength) * (far ? 0.06f : 0.10f);

                float x = WrapF(wx + drift - camX * parallax, span) - span / 4f;
                float y = ScreenY(altitude - camAlt * (1f - parallax), camAlt);
                if (x < -160 || x > Sim.DesignWidth + 40 || y < -24 || y > Sim.DesignHeight + 20)
                    continue;

                // Three sheet clouds, mirrored and depth-tinted: the far layer sits behind a
                // lavender veil so parity between the two reads as distance, not duplication.
                string[] tiers = { "cloud-small", "cloud-mid", "cloud-large" };
                string tier = tiers[Hash(i, seedSalt + 2) % 3];
                Vector2 size = Sprites.Size(tier);
                Sprites.Draw(canvas, tier, x - size.X / 2, y, (i & 1) == 1,
                             far ? Theme.CloudShade : (Color?)null);
            }
        }

        static void DrawWindStreaks(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            // One chevron train per band, locked to the world grid: count reads strength, the point
            // reads direction, and the ink under-draw keeps them legible on every sky value.
            for (int b = 0; b < level.Bands.Length; b++)
            {
                WindBand band = level.Bands[b];
                Color color = BandTint(b);
                float middle = (band.FromAltitude + band.ToAltitude) / 2f;
                float y = ScreenY(middle - (middle - camAlt) * 0.08f, camAlt);   // drift toward the camera plane
                if (y < 6 || y > Sim.DesignHeight - 6)
                    continue;

                // Signed slow drift, wrapped into one 160 px cell: the trains repeat every 160 px of
                // world, so wrapping the phase never visibly jumps — and it carries the band's sign,
                // so each train slides the way its air blows at its own crawl.
                float travel = Clock(state) * LevelDef.WindSpeed(band.Strength) * 0.04f * band.Direction;
                float phase = WrapF(travel, 160f);
                for (int wx = -640; wx <= (int)level.CourseLength + 640; wx += 160)
                {
                    float x = ScreenX(wx, camX) + phase;
                    if (x < -40 || x > Sim.DesignWidth + 10)
                        continue;
                    Kit.WindChevrons(canvas, x, y, band.Direction, band.Strength, color, 2);
                }
            }
        }

        static void DrawIslands(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            float groundY = ScreenY(0f, camAlt);

            // The ground plane: a grass line with a soil skirt, only while the camera is near the floor.
            if (groundY < Sim.DesignHeight)
            {
                canvas.Fill(0, groundY, Sim.DesignWidth, Sim.DesignHeight - groundY, Theme.MossGreen);
                canvas.Fill(0, groundY, Sim.DesignWidth, 2, Theme.GrassLight);
                float soilTop = groundY + 18;
                canvas.Fill(0, soilTop, Sim.DesignWidth, Sim.DesignHeight - soilTop, Theme.WoodDark);
            }

            DrawIsland(canvas, level, state, camX, groundY, worldX: 0, name: "HOME", launch: true);
            DrawIsland(canvas, level, state, camX, groundY, worldX: (int)level.CourseLength, name: level.DestinationName, launch: false);
            foreach (RestDeck deck in level.Platforms)
            {
                float x = ScreenX(deck.X, camX), y = ScreenY(deck.Altitude, camAlt);
                Kit.Island(canvas, x - deck.HalfWidth, y + 4, (int)deck.HalfWidth * 2, 24);
                Kit.Platform(canvas, new Rectangle(Round(x - deck.HalfWidth), Round(y), (int)deck.HalfWidth * 2, 5));
                WindBand wind = level.BandAt(deck.Altitude + 1f);
                Kit.Windsock(canvas, x + 20, y, wind.Direction, wind.Strength);
                Kit.Tag(canvas, Round(x)-30, Round(y)+10, "BREEZE PERCH");
            }
        }

        static void DrawIsland(
            DesignCanvas canvas, LevelDef level, SimState state, float camX, float groundY,
            float worldX, string name, bool launch)
        {
            float x = ScreenX(worldX, camX);
            if (x < -90 || x > Sim.DesignWidth + 90)
                return;

            // The sheet island: plank deck (exactly as wide as the sim's landing tolerance), trees
            // standing on the surface, grass cap, rock tapering to a point — delivery carries gold
            // bunting and a waterfall. The deck line sits 16 design px below the sprite's top edge,
            // so the draw offset puts it where it always was: groundY - 38.
            Sprites.Draw(canvas, launch ? "island-launch" : "island-delivery", x - 60f, groundY - 54f);

            // A sign on the deck: what this island is for, in the paper-and-ink language of the sheet.
            // The board glides with the island; its lettering holds the coarse grid (Round) — text that
            // half-steps inside a moving board looks broken, a board gliding under still text looks built.
            float signY = groundY - 52;
            int xi = Round(x);
            canvas.Fill(x - 1, signY + 12, 2, 8, Theme.BarkBrown);
            int signWidth = Math.Max(32, PixelFont.Width(name) + 8);
            canvas.CutRectangle(x - signWidth / 2, signY - 2, signWidth, 14, 1, Theme.Ink);
            canvas.Fill(x - signWidth / 2 + 1, signY - 1, signWidth - 2, 12, Theme.TagCream);
            PixelFont.Draw(canvas, name, xi - name.Length * 3 + 1, Round(signY) + 2, Theme.Ink);
            if (launch && state.CargoStatus == "waiting")
                Sprites.Draw(canvas, "gift", x + 34, groundY - 51);
            if (!launch)
            {
                Sprites.Draw(canvas, "heart-hud", x + 35, groundY - 54);
                Kit.Tag(canvas, xi + 10, Round(groundY) - 78, state.Delivered ? "THANK YOU!" : level.RequestLabel);
            }

            // The windsock flies off the windward post of the deck — the truth about this altitude,
            // in world. The sheet sock inflates from its post downwind; a band blowing left mirrors it.
            WindBand band = level.BandAt(1f);
            // Two frames of cloth snap, only for bands strong enough to snap cloth; the frozen
            // results clock stops the flutter with everything else, because the world stands still.
            string sock = band.Strength switch
            {
                1 => "windsock-1",
                _ => $"windsock-{band.Strength}-{Clock(state) / 12 % 2}",
            };
            Vector2 sockSize = Sprites.Size(sock);
            float postX = x + (band.Direction >= 0 ? -50 : 50);
            Sprites.Draw(canvas, sock, band.Direction >= 0 ? postX - 1 : postX - sockSize.X + 1,
                         groundY - 37, band.Direction < 0);
        }

        /// <summary>
        /// Every dropped sandbag falls where it was released: the sim recorded the release (tick,
        /// position), and a constant visual fall rate derived from the clock draws it — the player
        /// must see the bag leave the basket, because the bag IS the brake, and an invisible brake
        /// teaches nothing. Gone at the ground or after a few seconds, whichever screen first.
        /// </summary>
        static void DrawFallingBags(DesignCanvas canvas, SimState state, float camX, float camAlt)
        {
            const float FallSpeed = 70f;      // design px/s, visual only — no physics pretence
            int clock = Clock(state);
            for (int i = 0; i < state.DropTick.Length; i++)
            {
                if (state.DropsSeen <= i)
                    continue;                 // slot never used
                int age = clock - state.DropTick[i];
                if (age < 0 || age > clock)   // clock is frozen on the results screen: a bag released
                    continue;                 // before the freeze hangs on screen, as the world stops
                float alt = state.DropAltitude[i] - FallSpeed * age / Sim.StepHz;
                if (alt < 0f)
                    continue;                 // it landed off-screen; the ground owns it now
                Vector2 size = Sprites.Size("bag-falling");
                float bx = ScreenX(state.DropX[i], camX) - size.X / 2;
                float by = ScreenY(alt, camAlt) - size.Y / 2;
                Sprites.Draw(canvas, "bag-falling", bx, by);

                // I12 sand puffs: the release shakes a three-mote cloud that spreads for a fifth of a
                // second, and the fall trails a single grain every other tick — the sand is visibly
                // leaving the basket, which is the brake being spent. Pure clock math, no new state.
                if (age < 12)
                {
                    float ry = ScreenY(state.DropAltitude[i], camAlt);
                    float spread = 1f + age / 4f;
                    int[] offX = { -4, 3, 0 }, offY = { -2, -3, 4 };
                    for (int p = 0; p < 3; p++)
                        canvas.Fill(ScreenX(state.DropX[i], camX) + offX[p] * spread,
                            ry + offY[p] * spread, 2, 2, age < 6 ? Theme.SandTan : Theme.WickerDark);
                }
                else if (age % 2 == 0)
                {
                    canvas.Fill(bx + size.X / 2 - 1, by - 3, 1, 1, Theme.SandTan);
                }
            }
        }

        /// <summary>
        /// I9 storm banks: the art <em>is</em> the box — the row of angry clouds is drawn from the
        /// zone's own bounds, so where the sprites end is exactly where the buffet stops. Beneath
        /// the middle, a three-chevron arrow shows which way the bank is pushing this very tick,
        /// read from the same square wave the sim is feeling.
        /// </summary>
        static void DrawStormBanks(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            for (int i = 0; i < level.Storms.Length; i++)
            {
                StormZone zone = level.Storms[i];
                float midAlt = (zone.FromAltitude + zone.ToAltitude) / 2f;
                Vector2 size = Sprites.Size("storm-cloud");
                for (float cx = zone.FromX + size.X / 2f; cx < zone.ToX; cx += size.X - 10f)
                    Sprites.Draw(canvas, "storm-cloud",
                        ScreenX(cx, camX) - size.X / 2f, ScreenY(midAlt, camAlt) - size.Y / 2f);

                float push = level.StormWindAt((zone.FromX + zone.ToX) / 2f, midAlt, state.Tick);
                PixelFont.DrawCentered(canvas, push >= 0f ? ">>>" : "<<<",
                    Round(ScreenX((zone.FromX + zone.ToX) / 2f, camX)), Round(ScreenY(zone.FromAltitude, camAlt) + 8),
                    Theme.TagCream);
            }
        }

        /// <summary>
        /// I9 rocks and birds. Both drawn from the same closed-form tick functions the collision
        /// test calls, so a rock's sprite and its temper are the same number, and the bird flaps on
        /// the shared decoration clock — stopped when the run is, like everything else.
        /// </summary>
        static void DrawHazards(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            for (int i = 0; i < level.Rocks.Length; i++)
            {
                (float x, float alt) = level.RockAt(i, state.Tick);
                Vector2 size = Sprites.Size("rock");
                Sprites.Draw(canvas, "rock", ScreenX(x, camX) - size.X / 2f, ScreenY(alt, camAlt) - size.Y / 2f);
            }
            for (int i = 0; i < level.Birds.Length; i++)
            {
                (float x, float alt, int dir) = level.BirdAt(i, state.Tick);
                Vector2 size = Sprites.Size("bird-0");
                Sprites.Draw(canvas, "bird", Clock(state) / 8 % 2,
                    ScreenX(x, camX) - size.X / 2f, ScreenY(alt, camAlt) - size.Y / 2f, flip: dir < 0);
            }
        }

        static void DrawBalloon(DesignCanvas canvas, SimState state, float camX, float camAlt)
        {
            // The craft sits at its true world position; when the camera leads it, the offset reads as
            // speed. The sheet balloon is anchored by its basket bottom on the altitude line, so it
            // visibly touches the deck at touchdown instead of hovering a hand above it. The burner
            // flickers only while the sim says heat is on — three frames, clock-driven, deterministic.
            float x = ScreenX(state.X, camX);
            float foot = ScreenY(state.Altitude, camAlt) + 4f;
            int frame = state.Heating ? 1 + Clock(state) / 8 % 2 : 0;
            Vector2 size = Sprites.Size("balloon-0");

            // I12 basket sway: the load leans one px into fast climbs and the other into fast drops,
            // still when the air is still — velocity in, so it is the same pixel in every replay, and
            // off entirely under Reduced Motion.
            if (!AppSettings.ReducedMotion && !state.Complete)
                x += MathF.Round(Math.Clamp(state.VelocityY / -120f, -1f, 1f));

            // I9 invulnerability: for the grace window after a hit the balloon blinks, from a pure
            // (Tick - HurtTick) phase so stepped frames stay byte-stable. Reduced Motion dims her
            // through the window instead of blinking it — same news, gentler for the eyes.
            int hurtAge = state.Tick - state.HurtTick;
            bool hurtWindow = state.Hearts < SimTuning.HeartsStart && hurtAge >= 0 && hurtAge < SimTuning.InvulnTicks;
            if (hurtWindow && !AppSettings.ReducedMotion && hurtAge / 4 % 2 == 1)
                return;
            Color? tint = hurtWindow && AppSettings.ReducedMotion ? new Color(150, 150, 150) : null;

            Sprites.Draw(canvas, "balloon", frame, x - size.X / 2, foot - size.Y, tint: tint);
            if (state.Cargo != CargoKind.None)
                Sprites.Draw(canvas, "gift", x + 18, foot - 12);

            // I12 burner puffs: while heat is on, three small puffs vent from the envelope crown on a
            // rolling 30-tick cycle — pure from the decoration clock, palette clouds, and gone the
            // moment the burner is. They read as hot air escaping, which is exactly what is happening.
            if (state.Heating && !state.Complete && !AppSettings.ReducedMotion)
            {
                int clock = Clock(state);
                float crown = foot - size.Y - 1f;
                for (int p = 0; p < 3; p++)
                {
                    int age = (clock + p * 10) % 30;
                    float jitter = ((p * 7919 + age * 31) % 7) - 3;
                    float w = 1.5f + age * 0.09f;
                    Color puff = age < 10 ? Theme.CloudWhite : age < 20 ? Theme.CloudLavender : Theme.CloudShade;
                    canvas.Fill(x + jitter - w / 2f, crown - age * 0.55f, w, w, puff);
                }
            }
        }

        /// <summary>Sparkle offsets: eight honest integer directions, no sines needed for a party.</summary>
        static readonly (int Dx, int Dy)[] SparkleDirs =
            { (2, 1), (2, -1), (-2, 1), (-2, -1), (1, 2), (1, -2), (-1, 2), (-1, -2) };

        /// <summary>
        /// The delivery party (I12): for a few seconds after a landing goes well, star motes drift out
        /// from around the envelope — gold, cream and cloud-white, blinking on a tick parity. Runs off
        /// <c>Tick - CompletedAtTick</c>, so it animates while the world is live and freezes exactly
        /// where a paused screenshot caught it. Reduced Motion draws the ring steady: same celebration,
        /// no twinkle.
        /// </summary>
        static void DrawCelebration(DesignCanvas canvas, SimState state, float camX, float camAlt)
        {
            if (!state.Complete || !state.Delivered)
                return;
            int age = state.Tick - state.CompletedAtTick;
            if (age < 0 || age > 240)
                return;

            bool calm = AppSettings.ReducedMotion;
            float bx = ScreenX(state.X, camX);
            float by = ScreenY(state.Altitude + 34f, camAlt);   // envelope crown height, where the joy is
            for (int i = 0; i < SparkleDirs.Length; i++)
            {
                if (!calm && (age + i) % 6 >= 4)
                    continue;                                   // each mote hides two ticks in six
                int phase = calm ? 6 : (age + i * 9) % 18;
                float r = 16f + phase * 2.4f;
                int w = phase > 8 ? 2 : 1;
                Color c = i % 3 == 0 ? Theme.StarGold : i % 3 == 1 ? Theme.TagCream : Theme.CloudWhite;
                canvas.Fill(bx + SparkleDirs[i].Dx * r * 0.6f - w / 2f,
                            by + SparkleDirs[i].Dy * r * 0.5f - w / 2f, w, w, c);
            }
        }

        /// <summary>
        /// The clock the decorations run on: live while flying, stopped the instant the run completes.
        /// The sim keeps ticking after a delivery (its own determinism), but a results screen behind
        /// which clouds still sail and the flame still flickers tells the player the run is not over.
        /// </summary>
        static int Clock(SimState state) => state.Complete ? state.CompletedAtTick : state.Tick;

        // Hearts: 3 at launch, one per rock or bird (I9). "heart-hud-off" is the empty slot the
        // moment the sim spends one.
        const int HeartsTotal = 3;

        /// <summary>
        /// The instrument panel of the concept sheet: a wooden crate panel top-left carrying Charlotte's
        /// portrait, hearts, the star rating and a gift-box delivery count; the wind readout top-centre;
        /// and the burner's own heat gauge down the right edge, capped with a live flame. Every number
        /// shown is mirrored in the __GAME state — heating, temperature, ballastHeld, stars, delivered,
        /// level, and the altitude that picks the band — so tests assert the HUD's data, not its pixels.
        /// </summary>
        static void DrawHud(DesignCanvas canvas, LevelDef level, SimState state)
        {
            int clock = Clock(state);

            // ---- crate panel, top left: who is flying, and how she is doing ----
            Kit.Panel(canvas, new Rectangle(8, 8, 118, 44));
            Sprites.Draw(canvas, "portrait", 13, 11);
            canvas.CutRectangle(39, 10, 68, 32, 1, Theme.Dim);              // inner well behind the counters
            for (int i = 0; i < HeartsTotal; i++)
                Sprites.Draw(canvas, i < state.Hearts ? "heart-hud" : "heart-hud-off", 42 + i * 16, 12);
            Sprites.Draw(canvas, "star-hud", 42, 28);
            PixelFont.DrawShadowed(canvas, "x" + state.Stars, 58, 31, Theme.TagCream);
            Sprites.Draw(canvas, "gift", 78, 27);
            PixelFont.DrawShadowed(canvas, DeliveriesDone(state) + "/" + LevelCatalog.Levels.Length, 94, 31, Theme.TagCream);

            // ---- steering readout, top centre: the wind of the band the balloon is actually inside.
            // This is the HUD's only real job — the sole steering the player has.
            Kit.WindLadder(canvas, state, level);
            string heat = FlightGuidance.HeatTrend(state);
            PixelFont.DrawShadowed(canvas, state.Delivered ? level.DeliveredMessage : state.Cargo == CargoKind.None ? level.CollectObjective : level.CarryObjective,
                146, 12, Theme.TagCream, 2);
            PixelFont.DrawShadowed(canvas, heat == "heating" ? "HEATING - HOLD SPACE" : heat == "cooling" ? "COOLING - LET GO EARLY" : "HOLD SPACE TO RISE", 146, 32, Theme.TagCream, 2);
            PixelFont.DrawShadowed(canvas, state.PickupTick >= 0 && state.Tick - state.PickupTick < 150 ? "PARCEL SAFELY ABOARD" : "RELEASE EARLY TO SETTLE", 146, 50, Theme.TagCream, 2);

            // ---- heat gauge, right edge: a brass instrument capped with a live flame, the way the
            // sheet draws the burner's own thermometer ----
            var gauge = new Rectangle(Sim.DesignWidth - 26, 142, 16, 74);
            canvas.CutRectangle(gauge.X, gauge.Y, gauge.Width, gauge.Height, 2, Theme.Ink);
            canvas.Fill(gauge.X + 1, gauge.Y + 1, gauge.Width - 2, gauge.Height - 2, Theme.Dim);
            canvas.Fill(gauge.X + 2, gauge.Y + 1, 1, gauge.Height - 2, Theme.RockSlate);           // metal rim
            canvas.Fill(gauge.Right - 3, gauge.Y + 1, 1, gauge.Height - 2, Theme.RockDark);

            float fill = (state.Temperature - SimTuning.TempAmbient) / (SimTuning.TempMax - SimTuning.TempAmbient);
            int inner = gauge.Height - 4;
            int filled = (int)Math.Round(MathHelper.Clamp(fill, 0f, 1f) * inner);
            for (int row = 0; row < filled; row++)
            {
                // Gradient along the fill: how close the fabric is to too hot, the way any thermometer reads.
                float position = row / (float)Math.Max(1, inner);
                Color color = position > 0.72f ? Theme.FlameRed : position > 0.4f ? Theme.FlameOrange : Theme.FlameYellow;
                canvas.Fill(gauge.X + 2, gauge.Bottom - 2 - row, gauge.Width - 4, 1, color);
                canvas.Fill(gauge.Right - 3, gauge.Bottom - 2 - row, 1, 1, Color.Lerp(color, Theme.Ink, 0.35f)); // glass shade
            }

            for (int tick = 0; tick <= 4; tick++)
                canvas.Fill(gauge.X + 2, gauge.Bottom - 2 - inner * tick / 4, 2, 1, Theme.Cream);
            float neutral = (SimTuning.Gravity + state.BallastHeld * SimTuning.BallastPerBag) / SimTuning.LiftGain;
            int neutralY = gauge.Bottom - 2 - (int)((neutral - SimTuning.TempAmbient) / (SimTuning.TempMax - SimTuning.TempAmbient) * inner);
            canvas.Fill(gauge.X - 3, neutralY, gauge.Width + 6, 1, Theme.Cream);

            canvas.Fill(gauge.X + 5, gauge.Bottom, 6, 2, Theme.RockSlate);   // burner nozzle under the well
            canvas.Fill(gauge.X + 6, gauge.Bottom + 2, 4, 1, Theme.RockDark);

            // The cap flame is still at rest and flickers while the burner fires; it rides on the
            // decoration clock, so a results screen behind a frozen run keeps a frozen flame.
            // Reduced Motion holds it to the still frame — flicker is motion too.
            int flameFrame = state.Heating && !AppSettings.ReducedMotion && clock % 12 >= 6 ? 1 : 0;
            Sprites.Draw(canvas, "flame-hud", flameFrame, gauge.X + 1, gauge.Y - 12);
            Kit.Tag(canvas, Sim.DesignWidth - 33, 222, "HEAT");

            // ---- ballast, bottom left: the actual sandbags aboard, in things you could touch. The
            // dropped slots keep their shape — an empty bag is still a bag on the rope. ----
            for (int i = 0; i < SimTuning.BagCapacity; i++)
            {
                bool held = i < state.BallastHeld;
                Sprites.Draw(canvas, "bag-falling", 12 + i * 7, Sim.DesignHeight - 33, false, held ? (Color?)null : Theme.WoodMid);
            }
            Kit.Tag(canvas, 12, Sim.DesignHeight - 44, "BALLAST");

            // ---- course ruler bottom: travelled vs the delivery column, with the goal as a gold notch ----
            int left = 150;
            int width = 340;
            int y = Sim.DesignHeight - 14;
            canvas.Fill(left, y + 3, width, 1, Theme.WoodDark);
            int goalX = left + width - 2;
            canvas.Fill(goalX, y - 3, 3, 7, Theme.StarGold);
            int markX = left + (int)(Clamp(state.X, 0f, level.CourseLength) / level.CourseLength * (width - 4));
            canvas.Fill(markX, y, 3, 7, Theme.Cream);
            Kit.Tag(canvas, left, y - 14, level.NameUpper);
        }

        /// <summary>
        /// Deliveries completed on this flight: the courses flown before this one, plus this one the
        /// moment its package lands. Mirrored by state.level + state.delivered.
        /// </summary>
        static int DeliveriesDone(SimState state)
        {
            for (int i = 0; i < LevelCatalog.Levels.Length; i++)
                if (string.Equals(LevelCatalog.Levels[i].Id, state.LevelId, StringComparison.OrdinalIgnoreCase))
                    return i + (state.Delivered ? 1 : 0);
            return 0;
        }

        static int Index(LevelDef level, WindBand band)
        {
            for (int i = 0; i < level.Bands.Length; i++)
                if (level.Bands[i].FromAltitude == band.FromAltitude)
                    return i;
            return 0;
        }

        static Color BandTint(int bandIndex) =>
            bandIndex <= 0 ? Theme.WindLowBand : bandIndex == 1 ? Theme.WindMidBand : Theme.WindHighBand;

        static int Hash(int i, int salt)
        {
            unchecked
            {
                uint h = (uint)i * 2654435761u + (uint)salt * 40503u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (int)(h & 0x7FFFFFFF);
            }
        }

        static int Wrap(int value, int span)
        {
            int wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }

        static float WrapF(float value, float span)
        {
            float wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }

        static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
