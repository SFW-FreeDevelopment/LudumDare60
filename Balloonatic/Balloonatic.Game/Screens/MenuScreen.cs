using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The start screen (I7, first slice): who is flying, what the keys do, and one obvious way in.
    /// The sim does not run here — BalloonaticGame holds the clock while this screen is up — so the
    /// frame is byte-stable and a screenshot test can assert the controls list is actually legible.
    /// </summary>
    sealed class MenuScreen : IScreen
    {
        public string Id => SimState.ScreenMenu;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            // Ground-level sky: the two lowest bands of the world ramp, no camera, no parallax —
            // the menu is the calm frame before the moving one.
            canvas.Fill(0, 0, Sim.DesignWidth, 210, Theme.SkyMid);
            canvas.Fill(0, 210, Sim.DesignWidth, 120, Theme.SkyLow);
            canvas.Fill(0, 330, Sim.DesignWidth, 30, Theme.MossGreen);
            canvas.Fill(0, 330, Sim.DesignWidth, 2, Theme.GrassLight);
            canvas.Fill(0, 348, Sim.DesignWidth, 12, Theme.WoodDark);

            // The world props standing where the player will meet them: an island, two clouds,
            // and Charlotte airborne over her own launch deck.
            Sprites.Draw(canvas, "cloud-mid", 40f, 66f);
            Sprites.Draw(canvas, "cloud-small", 500f, 104f);
            Sprites.Draw(canvas, "island-launch", 40f, 330f - 54f);
            Sprites.Draw(canvas, "island-delivery", 500f, 330f - 54f);
            Sprites.Draw(canvas, "balloon-0", 300f, 60f);

            // Title. The Aseprite logo sprite is I6's brass-and-stitch pass; until it exists the
            // name rides the panel the same way every other readout does.
            int w = 268;
            int x = (Sim.DesignWidth - w) / 2;
            Kit.Panel(canvas, new Rectangle(x, 34, w, 52), "OLD TECH SKIES");
            PixelFont.DrawCentered(canvas, "CHARLOTTE'S BALLOONATIC", x + w / 2, 52, Theme.TagCream);
            PixelFont.DrawCentered(canvas, "A HOT-AIR DELIVERY OVER THE HILLS", x + w / 2, 68, Theme.SignCream);

            // Controls: the four verbs of the game, keys on the left, plain words on the right.
            int cw = 316;
            int cx = (Sim.DesignWidth - cw) / 2;
            Kit.Panel(canvas, new Rectangle(cx, 132, cw, 76), "CONTROLS");
            int row = 150;
            DrawControlRow(canvas, cx + 20, row, "HEAT", "HOLD SPACE OR LEFT MOUSE");
            DrawControlRow(canvas, cx + 20, row + 12, "BALLAST", "PRESS B OR RIGHT MOUSE");
            DrawControlRow(canvas, cx + 20, row + 24, "STEER", "ALTITUDE SELECTS THE WIND");
            DrawControlRow(canvas, cx + 20, row + 36, "LAND", "EASE ONTO A DECK, SLOW AND STRAIGHT");
            PixelFont.DrawCentered(canvas, "R FLY AGAIN   /   ESC QUIT", x + 134, 196, Theme.SignCream);

            // The way in, stated once, no blink (blinking breaks byte-stable screenshots and adds
            // nothing a still tag cannot say).
            Kit.Tag(canvas, Sim.DesignWidth / 2 - 51, 240, "PRESS ENTER OR CLICK TO FLY");
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        static void DrawControlRow(DesignCanvas canvas, int x, int y, string key, string what)
        {
            PixelFont.Draw(canvas, key, x, y, Theme.GrassLight);
            PixelFont.Draw(canvas, what, x + 66, y, Theme.SignCream);
        }
    }
}
