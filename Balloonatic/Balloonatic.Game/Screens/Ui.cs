namespace Balloonatic.Screens
{
    /// <summary>
    /// One frame of UI intent handed to the screen on display. Everything in it is an <em>edge</em>
    /// (a press this frame, not a held key) so screens cannot miss a tap between updates or double-fire
    /// a hold, and the pointer is already in design pixels — the letterbox math is the game's problem,
    /// not every screen's. Screens keep focus and hover as their own fields: UI state is rendering, so
    /// it never enters the sim hash and every stepped frame still draws byte-identical.
    /// </summary>
    struct UiFrame
    {
        /// <summary>Pointer in design coordinates; only meaningful when <see cref="PointerInside"/>.</summary>
        public int PointerX;
        public int PointerY;
        public bool PointerInside;

        /// <summary>Left button went down this frame.</summary>
        public bool Click;

        public bool Up, Down, Left, Right, Enter;

        /// <summary>The R key went down this frame — the "again/on" key menus can honour too.</summary>
        public bool Restart;

        /// <summary>The back verb (Escape). What "back" means is each screen's own call.</summary>
        public bool Back;

        public readonly bool PointIsIn(int x, int y, int w, int h) =>
            PointerInside && PointerX >= x && PointerX < x + w && PointerY >= y && PointerY < y + h;
    }

    /// <summary>What a UI screen may ask the game to do — the only verbs a screen has.</summary>
    interface UiHost
    {
        Balloonatic.Core.SimState Flight { get; }
        void GoTo(string screen);

        /// <summary>Choose a course and launch it: the level-select row's whole job.</summary>
        void FlyLevel(string levelId);

        /// <summary>Advance the voyage one course (the world map's FLY ON: the course after the last flight).</summary>
        void FlyNextCourse();

        /// <summary>The pause panel's RESTART: the same run, the same sky, from the deck.</summary>
        void RestartFlight();

        /// <summary>Close the game. The browser head cannot — there it is a no-op, and the menu stays.</summary>
        void Quit();

        /// <summary>A setting the live run can feel (Wind Assist) just changed: sync the running state.</summary>
        void LiveSettingsChanged();
    }

    /// <summary>
    /// A screen that takes input. Draw stays input-free, and Tick touches only UI fields plus the
    /// host's verbs — so a screen reached through <c>__GAME.screen()</c> with no input ever drawn
    /// exactly what it would draw at boot, screenshots included.
    /// </summary>
    interface IUiScreen : IScreen
    {
        void Tick(UiFrame frame);
    }
}
