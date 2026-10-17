using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// Credits (I7), short enough to read in one look and honest: the tools are listed because they
    /// are the workshop, and the dedication leads because it is the reason. The heart is the font's
    /// own ♥ — the same five-by-seven cells as every other letter, so the love is typeset, not pasted.
    /// </summary>
    sealed class CreditsScreen : IUiScreen
    {
        readonly UiHost _host;
        bool _hover;

        public CreditsScreen(UiHost host) => _host = host;

        public string Id => SimState.ScreenCredits;

        public void Tick(UiFrame f)
        {
            var back = new Rectangle(250, 276, 140, 16);
            _hover = f.PointIsIn(back.X, back.Y, back.Width, back.Height);
            if (f.Enter || (f.Click && _hover))
                _host.GoTo(SimState.ScreenMenu);
            if (f.Back)
                _host.GoTo(SimState.ScreenMenu);
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            // Crate on calm sky like every other menu; the dedication is the first line inside it.
            Kit.MenuBackdrop(canvas);

            int w = 360;
            int x = (Sim.DesignWidth - w) / 2;
            Kit.Panel(canvas, new Rectangle(x, 40, w, 216), "CREDITS");
            PixelFont.DrawCenteredShadowed(canvas, "FOR CHARLOTTE ♥", x + w / 2, 56, Theme.HeartPink);

            int line = 82;
            DrawLine(canvas, x, line, "A GAME ABOUT WIND, WICKER AND ONE VERY BRAVE GIRL");
            DrawLine(canvas, x, line + 18, "ENGINE - KNI, RUNNING AS BLAZOR WEBASM");
            DrawLine(canvas, x, line + 36, "PIXEL ART - ASEPRITE, DRIVEN BY SCRIPT AND MCP");
            DrawLine(canvas, x, line + 54, "TYPE - A HAND-SET FIVE BY SEVEN FACE");
            DrawLine(canvas, x, line + 72, "SOUND - ELEVENLABS (ARRIVING NEXT FLIGHT)");
            DrawLine(canvas, x, line + 90, "TEST PILOTS - XUNIT AND PLAYWRIGHT");
            DrawLine(canvas, x, line + 108, "WIND - THE ONLY ONE WHO STEERS ANYWAY");

            PixelFont.DrawCenteredShadowed(canvas, "LUDUM DARE 60 - OLD TECH - OCTOBER 2026",
                x + w / 2, 236, Theme.SignCream);

            Kit.Button(canvas, new Rectangle(250, 276, 140, 16), "BACK",
                _hover ? Kit.UiState.Hover : Kit.UiState.Rest, focused: true);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        static void DrawLine(DesignCanvas canvas, int panelX, int y, string text) =>
            PixelFont.DrawCenteredShadowed(canvas, text, panelX + 180, y, Theme.TagCream);
    }
}
