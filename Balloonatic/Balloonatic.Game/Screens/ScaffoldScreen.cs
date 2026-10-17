using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The harness screen for the balloon model: the three sky values read as three altitudes, the balloon
    /// rises and sinks with the real simulation, and every mark on it derives from state — including the
    /// drifting wind chevrons, which move by tick rather than wall clock — so a stepped frame is
    /// byte-stable. I4 replaces it with the scrolling world screen; this stays as the harness's neutral
    /// backdrop and the physics test bed.
    /// </summary>
    sealed class ScaffoldScreen : IScreen
    {
        const int BandHeight = Sim.DesignHeight / 3;

        // How much of the 360 px frame one design pixel of altitude moves: the scaffold shows the first
        // 400 px of climb; the real camera arrives with the world screen in I4.
        const float VisibleAltitude = 400f;

        // Temporary stand-ins for level data: low, mid, high. I3 moves bands into per-level data.
        static readonly (int dir, float speed, int strength)[] BandMarks =
        {
            (+1, 0.35f, 1),
            (-1, 0.70f, 2),
            (+1, 1.30f, 3),
        };

        public string Id => SimState.ScreenScaffold;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            DrawSky(canvas);
            DrawWind(canvas, state);
            DrawBalloon(canvas, state);
            DrawHeatGauge(canvas, state);
            DrawBallast(canvas, state);
            DrawAltitudeTape(canvas, state);
            DrawTickClock(canvas, state);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        static void DrawSky(DesignCanvas canvas)
        {
            canvas.Fill(0, 0, Sim.DesignWidth, BandHeight, Theme.NightBlue);
            canvas.Fill(0, BandHeight, Sim.DesignWidth, BandHeight, Theme.SkyMid);
            canvas.Fill(0, BandHeight * 2, Sim.DesignWidth, BandHeight, Theme.SkyLow);

            // One pixel-dark seam per band boundary: the player has to see exactly where a band starts,
            // because altitude is the only steering they get.
            canvas.Fill(0, BandHeight, Sim.DesignWidth, 1, Theme.Ink);
            canvas.Fill(0, BandHeight * 2, Sim.DesignWidth, 1, Theme.Ink);

            Kit.Cloud(canvas, 40, 24, 90, Theme.CloudWhite);
            Kit.Cloud(canvas, 420, 150, 120, Theme.CloudWhite);
            Kit.Cloud(canvas, 120, 268, 140, Theme.CloudShade);
        }

        static void DrawWind(DesignCanvas canvas, SimState state)
        {
            Color[] bandColors = { Theme.WindLowBand, Theme.WindMidBand, Theme.WindHighBand };
            for (int band = 0; band < BandMarks.Length; band++)
            {
                (int dir, float speed, int strength) = BandMarks[band];
                int span = Sim.DesignWidth + 40;
                for (int i = 0; i < 5; i++)
                {
                    float travel = state.Tick * speed * dir + i * (Sim.DesignWidth / 5f) + band * 51;
                    int x = (int)Wrap(travel, span) - 20;
                    int y = band * BandHeight + 24 + i * 16;
                    Kit.WindChevrons(canvas, x, y, dir, strength, bandColors[band]);
                }
            }
        }

        static void DrawBalloon(DesignCanvas canvas, SimState state)
        {
            // Ground rests at y=282; pegged climb scrolls the balloon up the frame until the camera takes over.
            int top = 160 - (int)(Clamp(state.Altitude, 0f, VisibleAltitude) / VisibleAltitude * 150f);
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
        /// The old-tech altitude tape: a column of tick marks with a moving index, the kind of instrument a
        /// 1890s aeronaut would actually have read. It also lets a screenshot prove the balloon's height.
        /// </summary>
        static void DrawAltitudeTape(DesignCanvas canvas, SimState state)
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

            int indexY = top + height - (int)(Clamp(state.Altitude, 0f, VisibleAltitude) / VisibleAltitude * height);
            canvas.Fill(x - 4, indexY - 1, 4, 3, Theme.StarGold);
            Kit.Tag(canvas, x - 12, top - 14, "ALT");
        }

        static void DrawTickClock(DesignCanvas canvas, SimState state)
        {
            int y = Sim.DesignHeight - 10;
            for (int i = 0; i < 60; i++)
                canvas.Fill(120 + i * 7, y, 5, 4, i < state.Tick % 60 ? Theme.StarGold : Theme.Dim);
        }

        static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        static float Wrap(float value, float span)
        {
            float wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }
    }
}
