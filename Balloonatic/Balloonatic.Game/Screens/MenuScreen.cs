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
        static readonly string[] Items = { "START ADVENTURE", "CHOOSE SKY", "SETTINGS", "CREDITS" };

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
                if (UiLayout.Menu(i) is Rectangle r && f.PointIsIn(r.X,r.Y,r.Width,r.Height))
                    return i;
            return -1;
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            Kit.MenuBackdrop(canvas);
            Sprites.Draw(canvas,"balloon-happy-0",48,78,scale:3);
            Sprites.Draw(canvas,"bag-falling",65,270,scale:3);
            Sprites.Draw(canvas,"bag-falling",188,270,scale:3);
            Sprites.Draw(canvas,"title-logo",266,16);
            PixelFont.DrawCenteredShadowed(canvas,"DELIVER DREAMS.",440,108,Theme.TagCream,Kit.Body(canvas));
            for(int i=0;i<Items.Length;i++)
                Kit.Button(canvas,UiLayout.Menu(i),Items[i],i==_hover?Kit.UiState.Hover:Kit.UiState.Rest,focused:i==_focus);
            PixelFont.DrawCenteredShadowed(canvas,"FOR CHARLOTTE ♥",440,324,Theme.HeartPink,Kit.Body(canvas));
            canvas.Outline(new Rectangle(0,0,640,360),Theme.Ink);
        }
    }
}
