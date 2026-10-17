using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The world map (I10): the voyage between courses, drawn as one crate-standing chart — the three
    /// authored islands as nodes, a dotted ink route between them, and Charlotte's balloon parked at
    /// the island the next flight will aim for. Courses carried home wear their stars; the sky is
    /// otherwise plain. One button, <c>FLY ON</c>, launches the next course (Enter and R answer it
    /// too, since that is what the delivery panel just told the player to press). Like every menu it
    /// holds the sim clock, and its only state is hover — stepped and paused frames draw identically.
    /// </summary>
    sealed class WorldMapScreen : IUiScreen
    {
        // Chart layout in design pixels: panel frame, then islands, labels, stars in that order.
        static readonly Rectangle Panel = new(60, 28, 520, 258);
        static readonly int[] NodeX = { 150, 320, 490 };
        const int IslandTop = 208;
        const int LabelY = 180;
        const int StarsY = 190;
        const int RouteY = 230;
        static readonly Rectangle Slat = new(270, 294, 100, 16);

        readonly UiHost _host;
        int _hover = -1;

        public WorldMapScreen(UiHost host) => _host = host;

        public string Id => SimState.ScreenWorldMap;

        /// <summary>
        /// The island the balloon waits at: the course after the one just flown, wrapping after the
        /// last — the same promise <see cref="TestHarness.AdvanceRun"/> keeps, read from state so a
        /// stepped or paused frame draws the same chart.
        /// </summary>
        static int NextIndex(SimState state)
        {
            int index = 0;
            for (int i = 0; i < LevelCatalog.Levels.Length; i++)
                if (LevelCatalog.Levels[i].Id == state.LevelId)
                    index = i;
            return (index + 1) % LevelCatalog.Levels.Length;
        }

        public void Tick(UiFrame f)
        {
            _hover = f.PointIsIn(Slat.X, Slat.Y, Slat.Width, Slat.Height) ? 0 : -1;
            if (f.Enter || f.Restart || (f.Click && _hover == 0))
                _host.FlyNextCourse();
            if (f.Back)
                _host.GoTo(SimState.ScreenMenu);
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            Kit.MenuBackdrop(canvas);
            Kit.Panel(canvas, Panel, "THE VOYAGE");

            int next = NextIndex(state);
            PixelFont.DrawCenteredShadowed(canvas, "NEXT: " + LevelCatalog.Levels[next].NameUpper,
                Sim.DesignWidth / 2, 60, Theme.SignCream);

            // The dotted route sits behind the islands, in the two gaps between them.
            for (int gap = 0; gap < 2; gap++)
                for (int x = NodeX[gap] + 66; x < NodeX[gap + 1] - 64; x += 8)
                    canvas.Fill(x, RouteY, 4, 2, Theme.Ink);

            for (int i = 0; i < LevelCatalog.Levels.Length; i++)
            {
                LevelDef level = LevelCatalog.Levels[i];
                bool home = AppProgress.Delivered(level.Id);
                int earned = AppProgress.StarsFor(level.Id);

                // The middle island is drawn as the delivery island — on the chart every stop is both.
                string sprite = i == 1 ? "island-delivery" : "island-launch";
                Vector2 islandSize = Sprites.Size(sprite);
                Sprites.Draw(canvas, sprite, NodeX[i] - islandSize.X / 2, IslandTop);

                PixelFont.DrawCenteredShadowed(canvas, level.NameUpper, NodeX[i], LabelY,
                    home ? Theme.TagCream : Theme.SignCream);

                // Stars are the chart's only score marks: lit for each the course ever paid out,
                // dimmed silhouettes for the rest, so a half-flown voyage reads at a glance.
                Vector2 starSize = Sprites.Size("star-hud");
                for (int s = 0; s < 3; s++)
                {
                    Color? tint = s < earned ? null : new Color(70, 70, 96);
                    Sprites.Draw(canvas, "star-hud",
                        NodeX[i] - (3 * starSize.X + 8) / 2 + s * (starSize.X + 4), StarsY, tint: tint);
                }

                if (i == next)
                {
                    // The waiting balloon sits above the label and stars, not across them: chart
                    // marks stay readable, the envelope alone says "this one is next".
                    Vector2 balloonSize = Sprites.Size("balloon-0");
                    Sprites.Draw(canvas, "balloon-0",
                        NodeX[i] - balloonSize.X / 2, LabelY - 8 - balloonSize.Y);
                }
            }

            Kit.Button(canvas, Slat, "FLY ON",
                _hover == 0 ? Kit.UiState.Hover : Kit.UiState.Rest, focused: true);
            PixelFont.DrawCenteredShadowed(canvas, "ENTER FLIES ON. ESC GOES BACK.",
                Sim.DesignWidth / 2, 320, Theme.SignCream);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }
    }
}
