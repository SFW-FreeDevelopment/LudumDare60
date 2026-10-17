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
    sealed class WorldScreen : IScreen
    {
        // Half a second of look-ahead: enough that +75 px/s of band wind pulls the view ~37 px ahead of
        // the craft, so speed is legible as framing, not only as a gauge.
        const float LookAheadSeconds = 0.5f;

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
            (int camX, int camAlt) = Camera(state, level);

            DrawSky(canvas, camAlt);
            DrawStars(canvas, camX, camAlt, state);
            DrawCloudLayer(canvas, level, state, camX, camAlt, far: true);
            DrawCloudLayer(canvas, level, state, camX, camAlt, far: false);
            DrawWindStreaks(canvas, level, state, camX, camAlt);
            DrawIslands(canvas, level, state, camX, camAlt);
            DrawBalloon(canvas, state, camX, camAlt);
            DrawHud(canvas, level, state);
            DrawLandingFeedback(canvas, level, state, camX, camAlt);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        /// <summary>
        /// The two moments the landing rule has to speak for itself: a bounce says why in one red tag,
        /// and a delivery stops the world behind a panel with the three stars it earned (PLAN I5 — the
        /// reason the player failed or what they won, always on screen).
        /// </summary>
        static void DrawLandingFeedback(DesignCanvas canvas, LevelDef level, SimState state, int camX, int camAlt)
        {
            if (state.Bounces > 0 && !state.Complete
                && state.Tick - state.BounceTick < SimTuning.BounceReasonSteps)
            {
                int bx = ScreenX((int)state.X, camX);
                int by = ScreenY(state.Altitude, camAlt) - 66;
                PixelFont.DrawCentered(canvas, "TOO FAST!", bx, by, Theme.FlameRed);
            }

            if (!state.Complete || !state.Delivered)
                return;

            int w = 210;
            int h = 96;
            int x = (Sim.DesignWidth - w) / 2;
            int y = 76;
            Kit.Panel(canvas, new Rectangle(x, y, w, h), "DELIVERY");
            PixelFont.DrawCentered(canvas, "CHARLOTTE IS SAFE", x + w / 2, y + 26, Theme.TagCream);

            // Frozen clock: the delivered time, not a live count — a score that keeps climbing is not a score.
            int seconds = state.CompletedAtTick / Sim.StepHz;
            string timeLine = "TIME " + seconds + "S   PAR " + level.ParSeconds + "S";
            PixelFont.DrawCentered(canvas, timeLine, x + w / 2, y + 42, seconds <= level.ParSeconds ? Theme.GrassLight : Theme.SignCream);
            Kit.Stars(canvas, x + w / 2 - 15, y + 56, state.Stars, 3, 2);

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
        static (int camX, int camAlt) Camera(SimState state, LevelDef level)
        {
            int worldLeft = -WorldMargin;
            int worldRight = (int)level.CourseLength + WorldMargin;
            float targetX = state.X + state.VelocityX * LookAheadSeconds;

            // Framing: the balloon rides the upper third (a 40 px lift) so the ground stays in frame
            // through launch, cruise, and landing up to ~220 px of altitude, and the view stops climbing
            // at the world roof. Pure state again — no render memory, byte-stable screenshots.
            int camAlt = (int)Clamp(state.Altitude + state.VelocityY * LookAheadSeconds * 0.5f - 40f, 0f, level.WorldHeight);

            // Framing bias toward the goal: until the delivery is behind the balloon, the camera leans
            // downwind, so the course ahead stays in frame and the islands are visible from launch.
            // Pure state again — no render memory, byte-stable screenshots.
            int courseBias = state.X < level.CourseLength ? Sim.DesignWidth / 4 : 0;
            int camX = (int)Clamp(targetX + courseBias, worldLeft + Sim.DesignWidth / 2, worldRight - Sim.DesignWidth / 2);
            return (camX, camAlt);
        }

        static int ScreenX(int worldX, int camX) => worldX - camX + Sim.DesignWidth / 2;
        static int ScreenY(float altitude, int camAlt) => Sim.DesignHeight / 2 - (int)(altitude - camAlt);

        static void DrawSky(DesignCanvas canvas, int camAlt)
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

        static void DrawStars(DesignCanvas canvas, int camX, int camAlt, SimState state)
        {
            for (int i = 0; i < 40; i++)
            {
                int wx = Wrap(Hash(i, 7) % (Sim.DesignWidth * 2), Sim.DesignWidth * 2);
                float alt = 380f + Hash(i, 13) % 400f;
                int x = wx - (int)(camX * StarsParallax) % (Sim.DesignWidth * 2);
                int y = ScreenY(alt, (int)(camAlt * StarsParallax));
                if (x < -2 || x > Sim.DesignWidth + 2 || y < 0 || y > Sim.DesignHeight)
                    continue;
                int twinkle = (Clock(state) / 20 + i) % 7 == 0 ? 0 : 1;
                canvas.Fill(Wrap(x, Sim.DesignWidth), y, twinkle, twinkle, i % 3 == 0 ? Theme.Cream : Theme.CloudLavender);
            }
        }

        static void DrawCloudLayer(DesignCanvas canvas, LevelDef level, SimState state, int camX, int camAlt, bool far)
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

                int x = Wrap(wx + (int)drift - (int)(camX * parallax), span) - span / 4;
                int y = ScreenY(altitude - (int)(camAlt * (1f - parallax)), camAlt);
                if (x < -160 || x > Sim.DesignWidth + 40 || y < -24 || y > Sim.DesignHeight + 20)
                    continue;

                int width = 70 + Hash(i, seedSalt + 2) % 70;
                Kit.Cloud(canvas, x, y, width, far ? Theme.CloudShade : Theme.CloudWhite);
            }
        }

        static void DrawWindStreaks(DesignCanvas canvas, LevelDef level, SimState state, int camX, int camAlt)
        {
            // One chevron train per band, locked to the world grid: count reads strength, the point
            // reads direction, and the ink under-draw keeps them legible on every sky value.
            for (int b = 0; b < level.Bands.Length; b++)
            {
                WindBand band = level.Bands[b];
                Color color = BandTint(b);
                float middle = (band.FromAltitude + band.ToAltitude) / 2f;
                int y = ScreenY(middle - (middle - camAlt) * 0.08f, camAlt);   // drift toward the camera plane
                if (y < 6 || y > Sim.DesignHeight - 6)
                    continue;

                float travel = Clock(state) * LevelDef.WindSpeed(band.Strength) * 0.04f * band.Direction;
                for (int wx = -640; wx <= (int)level.CourseLength + 640; wx += 160)
                {
                    int x = ScreenX(wx, camX) + (int)Wrap(travel, 160f * band.Direction) % 160 * band.Direction;
                    if (x < -40 || x > Sim.DesignWidth + 10)
                        continue;
                    Kit.WindChevrons(canvas, x, y, band.Direction, band.Strength, color, 2);
                }
            }
        }

        static void DrawIslands(DesignCanvas canvas, LevelDef level, SimState state, int camX, int camAlt)
        {
            int groundY = ScreenY(0f, camAlt);

            // The ground plane: a grass line with a soil skirt, only while the camera is near the floor.
            if (groundY < Sim.DesignHeight)
            {
                canvas.Fill(0, groundY, Sim.DesignWidth, Sim.DesignHeight - groundY, Theme.MossGreen);
                canvas.Fill(0, groundY, Sim.DesignWidth, 2, Theme.GrassLight);
                int soilTop = groundY + 18;
                canvas.Fill(0, soilTop, Sim.DesignWidth, Sim.DesignHeight - soilTop, Theme.WoodDark);
            }

            DrawIsland(canvas, level, state, camX, groundY, worldX: 0, name: "HOME", launch: true);
            DrawIsland(canvas, level, state, camX, groundY, worldX: (int)level.CourseLength, name: "GO", launch: false);
        }

        static void DrawIsland(
            DesignCanvas canvas, LevelDef level, SimState state, int camX, int groundY,
            int worldX, string name, bool launch)
        {
            int x = ScreenX(worldX, camX);
            if (x < -90 || x > Sim.DesignWidth + 90)
                return;

            // A grass-capped rock with a wooden deck: the sheet's floating islands, stand-in until I8.
            Kit.Island(canvas, new Rectangle(x - 60, groundY - 34, 120, 46));
            canvas.Fill(x - (int)SimTuning.DeckHalfWidth, groundY - 38, (int)SimTuning.DeckHalfWidth * 2, 4, launch ? Theme.WoodMid : Theme.WoodLight);

            // A sign on the deck: what this island is for, in the paper-and-ink language of the sheet.
            int signY = groundY - 52;
            canvas.Fill(x - 1, signY + 12, 2, 8, Theme.BarkBrown);
            canvas.CutRectangle(x - 16, signY - 2, 32, 14, 1, Theme.Ink);
            canvas.Fill(x - 15, signY - 1, 30, 12, Theme.TagCream);
            PixelFont.Draw(canvas, name, x - name.Length * 3 + 1, signY + 2, Theme.Ink);

            // The windsock flies off the windward post of the deck — the truth about this altitude, in world.
            WindBand band = level.BandAt(1f);
            Kit.Windsock(canvas, x + (band.Direction >= 0 ? -50 : 50), groundY - 36, band.Direction, band.Strength);

            if (!launch)
            {
                // Delivery bunting across the deck: gold pips so the target reads as the prize at a glance.
                for (int i = 0; i < 6; i++)
                    canvas.Fill(x - 45 + i * 18, groundY - 42, 3, 3, Theme.StarGold);
            }
        }

        static void DrawBalloon(DesignCanvas canvas, SimState state, int camX, int camAlt)
        {
            // The craft sits at its true world position; when the camera leads it, the offset reads as speed.
            int x = ScreenX((int)state.X, camX);
            int top = ScreenY(state.Altitude, camAlt) - 40;   // basket bottom rides the altitude line
            Kit.Balloon(canvas, x, top, state.Heating, Clock(state));
        }

        /// <summary>
        /// The clock the decorations run on: live while flying, stopped the instant the run completes.
        /// The sim keeps ticking after a delivery (its own determinism), but a results screen behind
        /// which clouds still sail and the flame still flickers tells the player the run is not over.
        /// </summary>
        static int Clock(SimState state) => state.Complete ? state.CompletedAtTick : state.Tick;

        static void DrawHud(DesignCanvas canvas, LevelDef level, SimState state)
        {
            // Heat gauge, right edge, flame cap at the hot end: the burner's own instrument.
            var bounds = new Rectangle(Sim.DesignWidth - 26, 150, 14, 68);
            float fill = (state.Temperature - SimTuning.TempAmbient) / (SimTuning.TempMax - SimTuning.TempAmbient);
            Kit.HeatMeter(canvas, bounds, fill, state.Heating);
            Kit.Tag(canvas, Sim.DesignWidth - 40, 222, "HEAT");

            // Ballast bags, bottom left: what's aboard, in things you could touch.
            Kit.Sandbags(canvas, 12, Sim.DesignHeight - 26, state.BallastHeld, SimTuning.BagCapacity);
            Kit.Tag(canvas, 12, Sim.DesignHeight - 40, "BALLAST");

            // Steering readout: the wind of the band the balloon is actually inside, big and central-left
            // where the eye already is. This is the HUD's only job.
            WindBand here = level.BandAt(state.Altitude);
            Kit.WindChevrons(canvas, 12, 12, here.Direction, here.Strength, BandTint(Index(level, here)), 3);
            Kit.Tag(canvas, 12, 30, "WIND");

            // Course ruler bottom: travelled vs the delivery column, with the goal as a gold notch.
            int left = 150;
            int width = 340;
            int y = Sim.DesignHeight - 14;
            canvas.Fill(left, y + 3, width, 1, Theme.WoodDark);
            int goalX = left + width - 2;
            canvas.Fill(goalX, y - 3, 3, 7, Theme.StarGold);
            int markX = left + (int)(Clamp(state.X, 0f, level.CourseLength) / level.CourseLength * (width - 4));
            canvas.Fill(markX, y, 3, 7, Theme.Cream);
            Kit.Tag(canvas, left, y - 14, level.Name.ToUpperInvariant());
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

        static float Wrap(float value, float span)
        {
            float wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }

        static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
