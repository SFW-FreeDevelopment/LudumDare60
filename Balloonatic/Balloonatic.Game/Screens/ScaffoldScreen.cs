using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The I0 harness screen. It exists to prove four things end to end in a browser: the fixed-step
    /// clock runs from injected input, state reaches JavaScript as a stable hash, the design-resolution
    /// letterbox scales correctly at both tested viewports, and wind is legible before any of it is
    /// pretty. Everything on screen is derived from <paramref name="state"/> — including the drifting
    /// wind dashes, which move by tick rather than wall clock — so a stepped frame is byte-stable.
    /// Replaced screen by screen as initiatives land; the wind table is temporary until I3 authors
    /// bands per level.
    /// </summary>
    sealed class ScaffoldScreen : IScreen
    {
        const int BandHeight = Sim.DesignHeight / 3;
        const int HeatTicksForFullCeiling = 180;

        // Temporary stand-ins for level data: low, mid, high. Direction and strength are what the
        // dashes visualize; I3 moves them into per-level wind bands.
        static readonly (int dir, float speed, int length, Color color)[] Bands =
        {
            (+1, 0.35f, 10, Palette.WindLow),
            (-1, 0.70f, 14, Palette.WindMid),
            (+1, 1.30f, 18, Palette.WindHigh),
        };

        public string Id => SimState.ScreenScaffold;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            DrawSky(canvas);
            DrawWind(canvas, state);
            DrawBalloon(canvas, state);
            DrawHeatMeter(canvas, state);
            DrawBallast(canvas, state);
            DrawTickClock(canvas, state);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Palette.Frame);
        }

        static void DrawSky(DesignCanvas canvas)
        {
            canvas.Fill(0, 0, Sim.DesignWidth, BandHeight, Palette.BandHigh);
            canvas.Fill(0, BandHeight, Sim.DesignWidth, BandHeight, Palette.BandMid);
            canvas.Fill(0, BandHeight * 2, Sim.DesignWidth, BandHeight, Palette.BandLow);

            // One pixel-dark seam per band boundary: the player must be able to see exactly where a
            // band starts, because altitude is the only steering they get.
            canvas.Fill(0, BandHeight, Sim.DesignWidth, 1, Palette.Frame);
            canvas.Fill(0, BandHeight * 2, Sim.DesignWidth, 1, Palette.Frame);
        }

        static void DrawWind(DesignCanvas canvas, SimState state)
        {
            for (int band = 0; band < Bands.Length; band++)
            {
                (int dir, float speed, int length, Color color) = Bands[band];
                int span = Sim.DesignWidth + length * 2;
                for (int i = 0; i < 6; i++)
                {
                    float travel = (state.Tick * speed * dir) + i * (Sim.DesignWidth / 6f) + band * 37;
                    int x = (int)Wrap(travel, span) - length;
                    int y = band * BandHeight + 22 + i * 14;
                    canvas.Fill(x, y, length, 2, color);

                    // 2-pixel head on the leading edge reads as an arrow at design scale, which is how
                    // a player learns which way a band pushes without a HUD.
                    int headX = dir > 0 ? x + length - 2 : x;
                    canvas.Fill(headX, y - 1, 2, 4, color);
                }
            }
        }

        static void DrawBalloon(DesignCanvas canvas, SimState state)
        {
            // Placeholder until I2 owns vertical motion: the basket climbs as heat accumulates, which
            // is enough to see whether the clock, the hash and the draw path agree.
            float heat01 = state.HeatTicks >= HeatTicksForFullCeiling
                ? 1f
                : state.HeatTicks / (float)HeatTicksForFullCeiling;
            int x = 296;
            int y = 252 - (int)(heat01 * 196);

            canvas.Fill(x, y, 34, 22, Palette.Envelope);
            canvas.Fill(x + 11, y, 12, 22, Palette.EnvelopeAccent);
            canvas.Fill(x + 6, y + 22, 1, 6, Palette.Basket);
            canvas.Fill(x + 27, y + 22, 1, 6, Palette.Basket);
            canvas.Fill(x + 9, y + 28, 16, 10, Palette.Basket);

            if (state.Heating)
            {
                int flicker = state.Tick % 8 < 4 ? 3 : 5;
                canvas.Fill(x + 15, y + 22, 4, flicker, Palette.Flame);
            }
        }

        static void DrawHeatMeter(DesignCanvas canvas, SimState state)
        {
            int x = Sim.DesignWidth - 26;
            int y = 244;
            canvas.Fill(x - 1, y - 1, 14, 62, Palette.MeterBack);
            int filled = state.HeatTicks * 60 / HeatTicksForFullCeiling;
            if (filled > 60)
                filled = 60;
            canvas.Fill(x, y + 60 - filled, 12, filled, Palette.Flame);
            canvas.Outline(new Rectangle(x - 1, y - 1, 14, 62), Palette.Frame);
        }

        static void DrawBallast(DesignCanvas canvas, SimState state)
        {
            for (int i = 0; i < 2; i++)
            {
                bool spent = state.BallastDropped > i;
                canvas.Fill(10 + i * 12, Sim.DesignHeight - 20, 9, 11, spent ? Palette.MeterBack : Palette.Basket);
            }
        }

        static void DrawTickClock(DesignCanvas canvas, SimState state)
        {
            int y = Sim.DesignHeight - 8;
            for (int i = 0; i < 60; i++)
                canvas.Fill(120 + i * 7, y, 5, 4, i < state.Tick % 60 ? Palette.WindHigh : Palette.MeterBack);
        }

        static float Wrap(float value, float span)
        {
            float wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }
    }
}
