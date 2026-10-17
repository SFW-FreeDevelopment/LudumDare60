using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The harness screen for the wind model, and the first frame where the world scrolls: the sky is
    /// painted per authored band, the ground and the delivery flag move by the balloon's downwind
    /// distance, and every mark derives from state — so a stepped frame is byte-stable and a screenshot
    /// can prove which band is carrying the balloon. I4 builds the real world screen on top of this; the
    /// scaffold stays as the physics test bed.
    /// </summary>
    sealed class ScaffoldScreen : IScreen
    {
        // The scaffold shows the first 400 px of altitude; the real camera arrives with I4.
        const float VisibleAltitude = 400f;
        const int HorizonY = 282;

        // One sky value per band, low to high. A level's bands are coloured by altitude, so the player
        // reads "which air am I in" from the background before they read a single gauge.
        static readonly Color[] BandSkies = { Theme.SkyLow, Theme.SkyMid, Theme.SkyHigh, Theme.NightBlue };

        public string Id => SimState.ScreenScaffold;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            LevelDef level = LevelCatalog.Get(state.LevelId);

            DrawBandedSky(canvas, level);
            DrawClouds(canvas, state);
            DrawBandWind(canvas, level, state);
            DrawGround(canvas, state);
            DrawBalloon(canvas, state);
            DrawHeatGauge(canvas, state);
            DrawBallast(canvas, state);
            DrawAltitudeTape(canvas, level, state);
            DrawCourseRuler(canvas, level, state);
            DrawTickClock(canvas, state);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        static void DrawBandedSky(DesignCanvas canvas, LevelDef level)
        {
            for (int i = 0; i < level.Bands.Length; i++)
            {
                WindBand band = level.Bands[i];
                int top = AltitudeToY(band.ToAltitude);
                int bottom = AltitudeToY(band.FromAltitude);
                canvas.Fill(0, top, Sim.DesignWidth, bottom - top, BandSkies[System.Math.Min(i, BandSkies.Length - 1)]);

                // Seam at the band's ceiling: exactly where the steering changes is visible to the pixel.
                canvas.Fill(0, top, Sim.DesignWidth, 1, Theme.Ink);
            }

            int topOfWorld = AltitudeToY(level.WorldHeight);
            if (topOfWorld > 0)
                canvas.Fill(0, 0, Sim.DesignWidth, topOfWorld, Theme.NightIndigo);
        }

        static void DrawClouds(DesignCanvas canvas, SimState state)
        {
            // Clouds drift with the wind they live in, and scroll with the balloon: parallax by altitude
            // is the readability trick from the sheet, and it is also physics information.
            for (int i = 0; i < 5; i++)
            {
                float altitude = 40f + i * 95f;
                WindBand band = LevelCatalog.Get(state.LevelId).BandAt(altitude);
                float drift = state.Tick * band.Direction * LevelDef.WindSpeed(band.Strength) * 0.5f;
                int x = WrapInt((int)(80 + i * 150 - drift - state.X * (0.25f + altitude / VisibleAltitude * 0.5f)),
                    Sim.DesignWidth + 260) - 130;
                int y = AltitudeToY(altitude) - 10;
                Kit.Cloud(canvas, x, y, 110, Theme.CloudWhite);
            }
        }

        static void DrawBandWind(DesignCanvas canvas, LevelDef level, SimState state)
        {
            foreach (WindBand band in level.Bands)
            {
                int middle = (AltitudeToY(band.FromAltitude) + AltitudeToY(band.ToAltitude)) / 2;
                Color color = BandColor(level, band);
                for (int i = 0; i < 3; i++)
                {
                    float travel = state.Tick * LevelDef.WindSpeed(band.Strength) * 0.06f * band.Direction;
                    int x = WrapInt(i * 240 + 60 + (int)travel, Sim.DesignWidth + 60) - 30;
                    Kit.WindChevrons(canvas, x, middle, band.Direction, band.Strength, color, 2);
                }

                // A windsock in the lowest band: the same truth, old-tech and in the world.
                if (band.FromAltitude <= SimTuning.GroundAltitude + 1f)
                    Kit.Windsock(canvas, 96, HorizonY, band.Direction, band.Strength);
            }
        }

        static void DrawGround(DesignCanvas canvas, SimState state)
        {
            canvas.Fill(0, HorizonY, Sim.DesignWidth, Sim.DesignHeight - HorizonY, Theme.MossGreen);
            canvas.Fill(0, HorizonY, Sim.DesignWidth, 2, Theme.GrassLight);

            // Fence posts every 64 world px: the scroll is the physics made visible.
            for (int world = -(int)state.X % 64; world < Sim.DesignWidth; world += 64)
            {
                canvas.Fill(world, HorizonY - 10, 2, 10, Theme.BarkBrown);
                canvas.Fill(world - 1, HorizonY - 11, 4, 1, Theme.BarkBrown);
            }

            LevelDef level = LevelCatalog.Get(state.LevelId);
            int flagX = (int)(level.CourseLength - state.X);
            if (flagX > -20 && flagX < Sim.DesignWidth + 20)
            {
                // The delivery column: a wooden sign on a post, cream board, ink text, ready for the
                // island art that replaces it in I8.
                canvas.Fill(flagX, HorizonY - 34, 2, 34, Theme.BarkBrown);
                canvas.CutRectangle(flagX - 14, HorizonY - 46, 30, 14, 1, Theme.Ink);
                canvas.Fill(flagX - 13, HorizonY - 45, 28, 12, Theme.TagCream);
                PixelFont.Draw(canvas, "GO", flagX - 8, HorizonY - 43, Theme.Ink);
            }
        }

        static void DrawBalloon(DesignCanvas canvas, SimState state)
        {
            // The balloon owns the centre of the frame; the world moves under it (I4 camera rules apply).
            int top = AltitudeToY(state.Altitude) - 56;
            Kit.Balloon(canvas, 320, top, state.Heating, state.Tick);
        }

        static void DrawHeatGauge(DesignCanvas canvas, SimState state)
        {
            var bounds = new Rectangle(Sim.DesignWidth - 26, 216, 14, 68);
            float fill = (state.Temperature - SimTuning.TempAmbient) / (SimTuning.TempMax - SimTuning.TempAmbient);
            Kit.HeatMeter(canvas, bounds, fill, state.Heating);
            Kit.Tag(canvas, Sim.DesignWidth - 40, 288, "HEAT");
        }

        static void DrawBallast(DesignCanvas canvas, SimState state)
        {
            Kit.Sandbags(canvas, 12, Sim.DesignHeight - 26, state.BallastHeld, SimTuning.BagCapacity);
            Kit.Tag(canvas, 12, Sim.DesignHeight - 40, "BALLAST");
        }

        /// <summary>
        /// The old-tech altitude tape, now drawn against the level: ticks along the tape, and the band
        /// boundaries marked in the wind's own colour, so the instrument and the sky say the same thing.
        /// </summary>
        static void DrawAltitudeTape(DesignCanvas canvas, LevelDef level, SimState state)
        {
            int x = Sim.DesignWidth - 60;
            int top = 96;
            int height = 188;
            canvas.Fill(x, top, 1, height, Theme.WoodDark);
            for (int mark = 0; mark <= 10; mark++)
            {
                int y = top + height - height * mark / 10;
                int width = mark % 5 == 0 ? 7 : 4;
                canvas.Fill(x + 1, y, width, 1, mark % 5 == 0 ? Theme.TagCream : Theme.WoodLight);
            }

            foreach (WindBand band in level.Bands)
            {
                int y = top + height - (int)(Clamp(band.FromAltitude, 0f, VisibleAltitude) / VisibleAltitude * height);
                canvas.Fill(x + 1, y, 9, 1, BandColor(level, band));
            }

            int indexY = top + height - (int)(Clamp(state.Altitude, 0f, VisibleAltitude) / VisibleAltitude * height);
            canvas.Fill(x - 4, indexY - 1, 4, 3, Theme.StarGold);
            Kit.Tag(canvas, x - 12, top - 14, "ALT");
        }

        /// <summary>Downwind distance as a wooden ruler with a gold index — the course, in px travelled.</summary>
        static void DrawCourseRuler(DesignCanvas canvas, LevelDef level, SimState state)
        {
            int y = Sim.DesignHeight - 22;
            int left = 120;
            canvas.Fill(left, y + 5, 400, 1, Theme.WoodDark);
            for (int world = 0; world <= (int)level.CourseLength; world += 100)
            {
                int x = left + (int)(world / level.CourseLength * 396f);
                canvas.Fill(x, y + 1, 1, 5, Theme.WoodLight);
            }

            int indexX = left + (int)(Clamp(state.X, 0f, level.CourseLength) / level.CourseLength * 396f);
            canvas.Fill(indexX - 1, y - 2, 3, 4, Theme.StarGold);
            Kit.Tag(canvas, left, y - 16, level.Name.ToUpperInvariant());
        }

        static void DrawTickClock(DesignCanvas canvas, SimState state)
        {
            int y = Sim.DesignHeight - 10;
            for (int i = 0; i < 60; i++)
                canvas.Fill(120 + i * 7, y, 5, 4, i < state.Tick % 60 ? Theme.StarGold : Theme.Dim);
        }

        static Color BandColor(LevelDef level, WindBand band)
        {
            int index = System.Array.IndexOf(level.Bands, band);
            return index <= 0 ? Theme.WindLowBand : index == 1 ? Theme.WindMidBand : Theme.WindHighBand;
        }

        static int AltitudeToY(float altitude) =>
            HorizonY - (int)(Clamp(altitude, 0f, VisibleAltitude) / VisibleAltitude * (HorizonY - 40f));

        static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        static int WrapInt(int value, int span)
        {
            int wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }
    }
}
