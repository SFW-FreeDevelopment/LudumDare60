using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// Credits (I7), short enough to read in one look; the dedication leads. The heart is the font's
    /// own ♥ — the same five-by-seven cells as every other letter, so the love is typeset, not pasted.
    /// </summary>
    sealed class CreditsScreen : IUiScreen
    {
        readonly UiHost _host;
        bool _hover;
        internal static readonly string[] Lines = { "A BRAVE GIRL. A BIG SKY.", "BY GIGABITE GAMES",
            "ENGINE: KNI / WEBGL", "ART: ASEPRITE", "LD60 / OLD TECH" };

        public CreditsScreen(UiHost host) => _host = host;

        public string Id => SimState.ScreenCredits;

        public void Tick(UiFrame f)
        {
            var back = UiLayout.Back;
            _hover = f.PointIsIn(back.X, back.Y, back.Width, back.Height);
            if (f.Enter || (f.Click && _hover))
                _host.GoTo(SimState.ScreenMenu);
            if (f.Back)
                _host.GoTo(SimState.ScreenMenu);
        }

        public void Draw(DesignCanvas canvas,SimState state) {
            Kit.MenuBackdrop(canvas);Kit.Panel(canvas,new Rectangle(48,16,544,336));
            int font=Kit.Body(canvas);
            PixelFont.DrawCenteredShadowed(canvas,"FOR CHARLOTTE ♥",320,32,Theme.HeartPink,Kit.Heading(canvas));
            for(int i=0;i<Lines.Length;i++)PixelFont.DrawCenteredShadowed(canvas,Lines[i],320,92+i*36,Theme.TagCream,font);
            Kit.Button(canvas,UiLayout.Back,"BACK",_hover?Kit.UiState.Hover:Kit.UiState.Rest,focused:true);
        }
    }
}
