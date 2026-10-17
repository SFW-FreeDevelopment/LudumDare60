using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The title screen: who is flying, the sheet's own ribbon under her name, four wooden slats for
    /// where to go, and the controls laid out like a launch checklist. The sim does not run here —
    /// BalloonaticGame holds the clock on every screen but the world — so the title frame is as still
    /// as a test frame, and the focus ring sits where the keyboard left it.
    /// </summary>
    sealed class MenuScreen : IUiScreen
    {
        // The button stack, in the order Up/Down walks it. FLY is first and starts focused: the
        // hand-off from title to sky is one Enter press, exactly as it was before menus existed.
        static readonly string[] Items = { "FLY", "CHOOSE COURSE", "SETTINGS", "CREDITS" };

        const int StackX = 220;
        const int StackY = 120;
        const int StackW = 200;
        const int StackH = 16;
        const int StackGap = 22;

        readonly UiHost _host;
        int _focus;
        int _hover = -1;

        public MenuScreen(UiHost host) => _host = host;

        public string Id => SimState.ScreenMenu;

        public void Tick(UiFrame f)
        {
            _hover = HitTest(f);
            if (f.Up) _focus = (_focus + Items.Length - 1) % Items.Length;
            if (f.Down) _focus = (_focus + 1) % Items.Length;
            if (f.Enter || (f.Click && _hover >= 0))
                Activate(f.Enter ? _focus : _hover);
            if (f.Back)
                _host.Quit();
        }

        void Activate(int index)
        {
            switch (index)
            {
                case 0: _host.GoTo(SimState.ScreenWorld); break;
                case 1: _host.GoTo(SimState.ScreenLevels); break;
                case 2: _host.GoTo(SimState.ScreenSettings); break;
                case 3: _host.GoTo(SimState.ScreenCredits); break;
            }
        }

        int HitTest(UiFrame f)
        {
            for (int i = 0; i < Items.Length; i++)
                if (f.PointIsIn(StackX, StackY + i * StackGap, StackW, StackH))
                    return i;
            return -1;
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            // The one backdrop the whole menu family shares (Kit.MenuBackdrop): the title is the same
            // calm place the courses are chosen from.
            Kit.MenuBackdrop(canvas);

            // The world props standing where the player will meet them, pushed to the sides so the
            // button stack rides open sky the way a clear approach runway does.
            Sprites.Draw(canvas, "island-launch", 28f, 330f - 54f);
            Sprites.Draw(canvas, "island-delivery", 508f, 330f - 54f);
            Sprites.Draw(canvas, "balloon-0", 156f, 52f);

            // Title, then the sheet's own ribbon under the name: FLOAT. EXPLORE. DELIVER DREAMS.
            int w = 268;
            int x = (Sim.DesignWidth - w) / 2;
            Kit.Panel(canvas, new Rectangle(x, 28, w, 52), "OLD TECH SKIES");
            PixelFont.DrawCentered(canvas, "CHARLOTTE'S BALLOONATIC", x + w / 2, 46, Theme.TagCream);
            PixelFont.DrawCentered(canvas, "A HOT-AIR DELIVERY OVER THE HILLS", x + w / 2, 62, Theme.SignCream);
            Kit.Tag(canvas, (Sim.DesignWidth - (PixelFont.Width("FLOAT. EXPLORE. DELIVER DREAMS.") + 6)) / 2,
                    88, "FLOAT. EXPLORE. DELIVER DREAMS.");

            // The four slats. Focus is a ring, hover is the lit slat, and both are drawn by the one
            // kit button the whole entry uses — keyboard and mouse arrive at the same picture.
            for (int i = 0; i < Items.Length; i++)
            {
                var bounds = new Rectangle(StackX, StackY + i * StackGap, StackW, StackH);
                Kit.UiState ui = i == _hover ? Kit.UiState.Hover : Kit.UiState.Rest;
                Kit.Button(canvas, bounds, Items[i], ui, focused: i == _focus);
            }

            // Controls: the four verbs of the game, keys on the left, plain words on the right.
            int cw = 316;
            int cx = (Sim.DesignWidth - cw) / 2;
            Kit.Panel(canvas, new Rectangle(cx, 214, cw, 76), "CONTROLS");
            int row = 232;
            DrawControlRow(canvas, cx + 20, row, "HEAT", "HOLD SPACE OR LEFT MOUSE");
            DrawControlRow(canvas, cx + 20, row + 12, "BALLAST", "PRESS B OR RIGHT MOUSE");
            DrawControlRow(canvas, cx + 20, row + 24, "STEER", "ALTITUDE SELECTS THE WIND");
            DrawControlRow(canvas, cx + 20, row + 36, "LAND", "EASE ONTO A DECK, SLOW AND STRAIGHT");
            PixelFont.DrawCentered(canvas, "ARROWS SELECT   ENTER CONFIRMS   ESC ESCAPES", x + w / 2, 278, Theme.SignCream);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        static void DrawControlRow(DesignCanvas canvas, int x, int y, string key, string what)
        {
            PixelFont.Draw(canvas, key, x, y, Theme.GrassLight);
            PixelFont.Draw(canvas, what, x + 66, y, Theme.SignCream);
        }
    }
}
