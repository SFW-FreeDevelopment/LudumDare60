using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The I0 harness screen, restyled so the kit has something to prove before the game screens exist:
    /// the three wind bands are the three sky values, the dashes read direction and strength, and the
    /// balloon rises as heat accumulates. Everything is derived from state — including the drifting dashes,
    /// which move by tick rather than wall clock — so a stepped frame is byte-stable.
    /// Replaced by the level screen in I4; kept as the harness's neutral backdrop.
    /// </summary>
    sealed class ScaffoldScreen : IScreen
    {
        const int BandHeight = Sim.DesignHeight / 3;
        const int HeatTicksForFullCeiling = 180;

        // Temporary stand-ins for level data: low, mid, high. I3 moves bands into per-level data.
        static readonly (int dir, float speed, int strength, Color color)[] Bands =
        {
            (+1, 0.35f, 1, Theme.WindLowBand),
            (-1, 0.70f, 2, Theme.WindMidBand),
            (+1, 1.30f, 3, Theme.WindHighBand),
        };

        public string Id => SimState.ScreenScaffold;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            DrawSky(canvas);
            DrawWind(canvas, state);
            Kit.Balloon(canvas, 320, 150 - Rise(state) * 9 / 10, state.Heating, state.Tick);
            DrawHeatMeter(canvas, state);
            DrawBallast(canvas, state);
            DrawTickClock(canvas, state);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        static int Rise(SimState state) =>
            state.HeatTicks > HeatTicksForFullCeiling ? HeatTicksForFullCeiling : state.HeatTicks;

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
            for (int band = 0; band < Bands.Length; band++)
            {
                (int dir, float speed, int strength, Color color) = Bands[band];
                int span = Sim.DesignWidth + 40;
                for (int i = 0; i < 5; i++)
                {
                    float travel = state.Tick * speed * dir + i * (Sim.DesignWidth / 5f) + band * 51;
                    int x = (int)Wrap(travel, span) - 20;
                    int y = band * BandHeight + 24 + i * 16;
                    Kit.WindChevrons(canvas, x, y, dir, strength, color);
                }
            }
        }

        static void DrawHeatMeter(DesignCanvas canvas, SimState state)
        {
            var bounds = new Rectangle(Sim.DesignWidth - 26, 216, 14, 68);
            float fill = state.HeatTicks / (float)HeatTicksForFullCeiling;
            Kit.HeatMeter(canvas, bounds, fill, state.Heating);
            Kit.Tag(canvas, Sim.DesignWidth - 40, 288, "HEAT");
        }

        static void DrawBallast(DesignCanvas canvas, SimState state)
        {
            Kit.Sandbags(canvas, 12, Sim.DesignHeight - 26, 2 - state.BallastDropped, 2);
            Kit.Tag(canvas, 12, Sim.DesignHeight - 40, "BALLAST");
        }

        static void DrawTickClock(DesignCanvas canvas, SimState state)
        {
            int y = Sim.DesignHeight - 10;
            for (int i = 0; i < 60; i++)
                canvas.Fill(120 + i * 7, y, 5, 4, i < state.Tick % 60 ? Theme.StarGold : Theme.Dim);
        }

        static float Wrap(float value, float span)
        {
            float wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }
    }
}
