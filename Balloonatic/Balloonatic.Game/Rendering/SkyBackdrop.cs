using System;
using System.Globalization;
using Balloonatic.Core;

namespace Balloonatic.Rendering
{
    // Scenery is deliberately slower than the craft. All time is seconds, never raw ticks.
    static class SkyBackdrop
    {
        public static float Drift(int tick) => AppSettings.ReducedMotion ? 0f : tick / (float)Sim.StepHz * .6f;
        static float Wrap(float x, float span) => (x % span + span) % span;

        public static string Describe(SimState state) => "floor=clouds;atmosphere=day;drift="
            + Drift(state.Complete ? state.CompletedAtTick : state.Tick).ToString("0.00", CultureInfo.InvariantCulture);

        public static void Draw(DesignCanvas canvas, float camX, float camAlt, int tick)
        {
            canvas.Fill(0, 0, 640, 360, Theme.SkyMid);
            float drift = Drift(tick);
            for (int i = 0; i < 8; i++)
            {
                float x = Wrap(i * 137 + 42 - camX * .12f + drift * .5f, 880) - 120;
                float y = 24 + (i * 53 % 190) + (camAlt - 180) * .08f;
                Sprites.Draw(canvas, i % 2 == 0 ? "cloud-mid" : "cloud-large", x, y, i % 3 == 0, Theme.CloudShade);
            }
            // Authored silhouettes, with buildings and falling water, establish distance and scale.
            for (int i = 0; i < 5; i++)
            {
                float x = Wrap(i * 193 + 96 - camX * .2f, 1000) - 110;
                float y = 70 + i * 61 % 130 + (camAlt - 180) * .16f;
                Sprites.Draw(canvas, "far-isle", x, y, i % 2 == 1);
            }
            for (int i = 0; i < 5; i++)
            {
                float x = Wrap(i * 173 + 27 - camX * .3f + drift, 940) - 140;
                float y = 142 + i * 49 % 148 + (camAlt - 180) * .23f;
                Sprites.Draw(canvas, "cloud-large", x, y, i % 2 == 0);
            }
            // A sea of clouds replaces the soil wall. It stays behind every collidable object.
            float bed = Math.Min(316, 180 + camAlt - 18);
            canvas.Fill(0, bed + 17, 640, 360, Theme.CloudLavender);
            for (int i = -1; i < 7; i++)
            {
                float x = i * 118 - Wrap(camX * .15f, 118);
                Sprites.Draw(canvas, "cloud-large", x, bed + (i % 2 == 0 ? -8 : 4));
            }
        }
    }
}
