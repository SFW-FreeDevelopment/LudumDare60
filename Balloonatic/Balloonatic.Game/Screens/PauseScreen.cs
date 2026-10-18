using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// Pause (I7): the flight held mid-air — the world keeps its pose underneath (the sim clock is
    /// held on every screen but the world, so the balloon hangs where it was) under a veil of dusk
    /// and four honest slats. Reached by ESC or the corner button; ESC again lets go.
    /// </summary>
    sealed class PauseScreen : IUiScreen
    {
        static readonly string[] Items = { "RESUME", "RESTART", "SETTINGS", "QUIT TO TITLE" };

        readonly WorldScreen _world;
        readonly UiHost _host;
        int _focus;
        int _hover = -1;

        public PauseScreen(WorldScreen world, UiHost host)
        {
            _world = world;
            _host = host;
        }

        public string Id => SimState.ScreenPause;

        public void Tick(UiFrame f)
        {
            _hover = HitTest(f);
            if (f.Up) _focus = (_focus + Items.Length - 1) % Items.Length;
            if (f.Down) _focus = (_focus + 1) % Items.Length;
            int picked = f.Enter ? _focus : f.Click && _hover >= 0 ? _hover : -1;
            if (picked >= 0)
                Activate(picked);
            if (f.Back)
                _host.GoTo(SimState.ScreenWorld);   // ESC lets go of the pause, same as RESUME
        }

        void Activate(int index)
        {
            switch (index)
            {
                case 0: _host.GoTo(SimState.ScreenWorld); break;
                case 1: _host.RestartFlight(); break;
                case 2: _host.GoTo(SimState.ScreenSettings); break;
                case 3: _host.GoTo(SimState.ScreenMenu); break;
            }
        }

        int HitTest(UiFrame f)
        {
            for (int i = 0; i < Items.Length; i++)
                if (UiLayout.PauseMenu(i) is Rectangle r && f.PointIsIn(r.X,r.Y,r.Width,r.Height))
                    return i;
            return -1;
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            // The held flight, still and waiting, dimmed by dusk rather than a blur: one translucent
            // ink wash says "not now" without hiding where the balloon is — you will land it yourself.
            _world.Draw(canvas, state);
            canvas.Fill(0, 0, Sim.DesignWidth, Sim.DesignHeight, Theme.PauseVeil);

            Kit.Panel(canvas,new Rectangle(144,48,352,268));
            PixelFont.DrawCenteredShadowed(canvas,"PAUSED",320,64,Theme.TagCream,Kit.Heading(canvas));
            for(int i=0;i<Items.Length;i++)Kit.Button(canvas,UiLayout.PauseMenu(i),Items[i],i==_hover?Kit.UiState.Hover:Kit.UiState.Rest,focused:i==_focus);
        }
    }
}
