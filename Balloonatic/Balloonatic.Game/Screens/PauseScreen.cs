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

        const int StackX = 244;
        const int StackY = 120;
        const int StackW = 152;
        const int StackH = 16;
        const int StackGap = 24;

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
                if (f.PointIsIn(StackX, StackY + i * StackGap, StackW, StackH))
                    return i;
            return -1;
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            // The held flight, still and waiting, dimmed by dusk rather than a blur: one translucent
            // ink wash says "not now" without hiding where the balloon is — you will land it yourself.
            _world.Draw(canvas, state);
            canvas.Fill(0, 0, Sim.DesignWidth, Sim.DesignHeight, Theme.PauseVeil);

            int w = 196;
            int x = (Sim.DesignWidth - w) / 2;
            Kit.Panel(canvas, new Rectangle(x, 90, w, 124), null);
            PixelFont.DrawCenteredShadowed(canvas, "PAUSED", x + w / 2, 102, Theme.TagCream);
            for (int i = 0; i < Items.Length; i++)
            {
                var bounds = new Rectangle(StackX, StackY + i * StackGap, StackW, StackH);
                Kit.Button(canvas, bounds, Items[i],
                    i == _hover ? Kit.UiState.Hover : Kit.UiState.Rest, focused: i == _focus);
            }
        }
    }
}
