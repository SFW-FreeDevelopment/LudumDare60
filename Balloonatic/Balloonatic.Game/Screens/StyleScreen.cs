using System.Collections.Generic;
using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The style lock, on screen: every colour, type size and component the game is allowed to use, drawn by
    /// the kit itself so the sheet cannot drift from the code. It is reachable from the harness
    /// (<c>__GAME.screen('style')</c>) which is how a screenshot of it is deterministic, and it is the thing
    /// to look at when judging whether the entry is on-palette before any gameplay art exists.
    /// </summary>
    sealed class StyleScreen : IScreen
    {
        public string Id => SimState.ScreenStyle;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            canvas.Fill(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.PanelBrown);

            DrawTitle(canvas);
            DrawPalette(canvas);
            DrawType(canvas);
            DrawReadouts(canvas);
            DrawButtons(canvas);
            DrawBalloon(canvas, state);
            DrawWorld(canvas, state);
            DrawLegend(canvas);

            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        static void DrawTitle(DesignCanvas canvas)
        {
            PixelFont.DrawCenteredShadowed(canvas, "CHARLOTTE'S BALLOONATIC", Sim.DesignWidth / 2, 12, Theme.TagCream, 2);
            PixelFont.DrawCentered(canvas, "STYLE LOCK  -  EVERY VISUAL DECISION, IN ONE PICTURE",
                Sim.DesignWidth / 2, 32, Theme.BalloonPink);
        }

        static void DrawPalette(DesignCanvas canvas)
        {
            var panel = new Rectangle(8, 48, 216, 104);
            Kit.Panel(canvas, panel, "PALETTE");

            IReadOnlyList<(string Name, Color Color)> swatches = Swatches();
            int x = panel.X + 8;
            int y = panel.Y + 14;
            for (int i = 0; i < swatches.Count && i < 24; i++)
            {
                int column = i % 6;
                int row = i / 6;
                var bounds = new Rectangle(x + column * 33, y + row * 26, 29, 22);
                canvas.Outline(bounds, Theme.Ink);
                canvas.Fill(new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2), swatches[i].Color);
            }
        }

        static void DrawType(DesignCanvas canvas)
        {
            var panel = new Rectangle(232, 48, 200, 104);
            Kit.Panel(canvas, panel, "TYPE");

            PixelFont.DrawShadowed(canvas, "FLOAT. EXPLORE.", panel.X + 10, panel.Y + 16, Theme.Cream, 2);
            PixelFont.DrawShadowed(canvas, "DELIVER DREAMS.", panel.X + 10, panel.Y + 36, Theme.Cream, 2);
            PixelFont.DrawShadowed(canvas, "BODY 10PX: DROP BALLAST TO RISE", panel.X + 10, panel.Y + 60, Theme.TagCream, 1);
            PixelFont.DrawShadowed(canvas, "WIND IS THE ONLY STEERING YOU GET", panel.X + 10, panel.Y + 72, Theme.WindMidBand, 1);
            PixelFont.Draw(canvas, "FOR CHARLOTTE ♥", panel.X + 10, panel.Y + 86, Theme.HeartPink, 1);
        }

        static void DrawReadouts(DesignCanvas canvas)
        {
            var panel = new Rectangle(440, 48, 192, 104);
            Kit.Panel(canvas, panel, "READOUTS");

            Kit.HeatMeter(canvas, new Rectangle(panel.X + 10, panel.Y + 16, 14, 68), 0.62f, true);
            Kit.Tag(canvas, panel.X + 6, panel.Y + 88, "HEAT");

            int x = panel.X + 40;
            Kit.Hearts(canvas, x, panel.Y + 18, 2, 3, 2);
            Kit.Tag(canvas, x - 2, panel.Y + 36, "HEARTS");

            Kit.Stars(canvas, x, panel.Y + 52, 2, 3, 2);
            Kit.Tag(canvas, x - 2, panel.Y + 70, "STARS");

            Kit.Sandbags(canvas, panel.X + 100, panel.Y + 18, 1, 2, 2);
            Kit.Tag(canvas, panel.X + 98, panel.Y + 36, "BALLAST");

            int windY = panel.Y + 52;
            Kit.WindChevrons(canvas, panel.X + 100, windY, +1, 1, Theme.WindLowBand, 2);
            Kit.WindChevrons(canvas, panel.X + 100, windY + 12, -1, 2, Theme.WindMidBand, 2);
            Kit.WindChevrons(canvas, panel.X + 100, windY + 24, +1, 3, Theme.WindHighBand, 2);
        }

        static void DrawButtons(DesignCanvas canvas)
        {
            var panel = new Rectangle(8, 164, 216, 112);
            Kit.Panel(canvas, panel, "BUTTONS");

            string[] labels = { "START ADVENTURE", "SETTINGS", "CREDITS", "FOR CHARLOTTE ♥" };
            Kit.UiState[] states = { Kit.UiState.Rest, Kit.UiState.Hover, Kit.UiState.Press, Kit.UiState.Rest };
            bool[] focused = { false, false, false, true };

            for (int i = 0; i < labels.Length; i++)
            {
                var bounds = new Rectangle(panel.X + 16, panel.Y + 16 + i * 22, 176, Kit.ButtonHeight);
                Kit.Button(canvas, bounds, labels[i], states[i], focused[i]);
            }
        }

        static void DrawBalloon(DesignCanvas canvas, SimState state)
        {
            var panel = new Rectangle(232, 164, 200, 112);
            Kit.Panel(canvas, panel, "BALLOON");

            Kit.Balloon(canvas, panel.X + 56, panel.Y + 16, false, state.Tick);
            Kit.Balloon(canvas, panel.X + 144, panel.Y + 16, true, state.Tick);
            PixelFont.Draw(canvas, "COASTING", panel.X + 24, panel.Bottom - 12, Theme.Cream);
            PixelFont.Draw(canvas, "BURNER", panel.X + 116, panel.Bottom - 12, Theme.FlameYellow);
        }

        static void DrawWorld(DesignCanvas canvas, SimState state)
        {
            var panel = new Rectangle(440, 164, 192, 112);
            Kit.Panel(canvas, panel, "WORLD");

            Kit.Island(canvas, panel.X + 12, panel.Y + 30, 72, 46);
            Kit.Cloud(canvas, panel.X + 100, panel.Y + 20, 64, Theme.CloudWhite);
            Kit.Windsock(canvas, panel.X + 150, panel.Y + 74, +1, 3);
            Kit.Tag(canvas, panel.X + 96, panel.Bottom - 22, "WINDSOCK");
            PixelFont.Draw(canvas, "FLOATING ISLAND", panel.X + 10, panel.Bottom - 12, Theme.Cream);
        }

        static void DrawLegend(DesignCanvas canvas)
        {
            var panel = new Rectangle(8, 284, 624, 68);
            Kit.Panel(canvas, panel, "PALETTE ROLES");

            IReadOnlyList<(string Name, Color Color)> swatches = Swatches();
            for (int i = 0; i < swatches.Count; i++)
            {
                int column = i % 4;
                int row = i / 4;
                int x = panel.X + 10 + column * 154;
                int y = panel.Y + 14 + row * 9;
                canvas.Fill(x, y + 1, 5, 5, swatches[i].Color);
                PixelFont.Draw(canvas, swatches[i].Name, x + 9, y, Theme.TagCream);
                string hex = "#" + swatches[i].Color.R.ToString("X2")
                    + swatches[i].Color.G.ToString("X2") + swatches[i].Color.B.ToString("X2");
                PixelFont.Draw(canvas, hex, x + 96, y, Theme.WoodLight);
            }
        }

        /// <summary>The palette in one list, named as the code names it. Also what the legend prints.</summary>
        static IReadOnlyList<(string Name, Color Color)> Swatches() => new (string, Color)[]
        {
            ("INK", Theme.Ink),
            ("PANEL", Theme.PanelBrown),
            ("WOOD DARK", Theme.WoodDark),
            ("WOOD MID", Theme.WoodMid),
            ("WOOD LIGHT", Theme.WoodLight),
            ("WICKER", Theme.WickerDark),
            ("WICKER LIT", Theme.WickerLight),
            ("TAG PAPER", Theme.TagCream),
            ("CREAM", Theme.Cream),
            ("NIGHT INDIGO", Theme.NightIndigo),
            ("NIGHT BLUE", Theme.NightBlue),
            ("SKY MID", Theme.SkyMid),
            ("SKY LOW", Theme.SkyLow),
            ("DUSK TEAL", Theme.DuskTeal),
            ("CLOUD SHDW", Theme.CloudLavender),
            ("CLOUD", Theme.CloudWhite),
            ("BALLOON PINK", Theme.BalloonPink),
            ("BALLOON LIGHT", Theme.BalloonPinkLight),
            ("BALLOON RED", Theme.BalloonRed),
            ("FLAME HOT", Theme.FlameRed),
            ("FLAME MID", Theme.FlameOrange),
            ("FLAME CORE", Theme.FlameYellow),
            ("LEAF", Theme.LeafGreen),
            ("ROCK", Theme.RockSlate),
        };
    }
}
