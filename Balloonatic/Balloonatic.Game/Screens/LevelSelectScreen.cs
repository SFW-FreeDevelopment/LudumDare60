using System.Text;
using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// Level select (I7): the three authored courses on wooden slats, each with the sky it flies under
    /// written out — par, band strengths as signed chevrons, gust count — so a player picks a course by
    /// its wind, not by its name. A row press chooses the course and launches it on the spot.
    /// </summary>
    sealed class LevelSelectScreen : IUiScreen
    {
        const int RowX = 158;
        const int RowY = 96;
        const int RowW = 280;
        const int RowH = 16;
        const int RowGap = 56;

        // Built once, not per frame: the draw loop stays allocation-free like everywhere else that runs
        // at 60 Hz, and these strings only change when an author edits the catalog.
        static readonly string[] Rows = BuildRows();

        readonly UiHost _host;
        int _focus;
        int _hover = -1;

        public LevelSelectScreen(UiHost host) => _host = host;

        public string Id => SimState.ScreenLevels;

        static string[] BuildRows()
        {
            string[] rows = new string[LevelCatalog.Levels.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                LevelDef level = LevelCatalog.Levels[i];
                var sb = new StringBuilder("PAR ");
                sb.Append(level.ParSeconds).Append("S   BANDS ");
                for (int b = 0; b < level.Bands.Length; b++)
                {
                    if (b > 0) sb.Append(' ');
                    sb.Append(level.Bands[b].Direction >= 0 ? '>' : '<')
                      .Append(level.Bands[b].Strength);
                }
                if (level.Gusts.Length > 0)
                    sb.Append("   GUSTS ").Append(level.Gusts.Length);
                rows[i] = sb.ToString();
            }
            return rows;
        }

        public void Tick(UiFrame f)
        {
            _hover = HitTest(f);
            if (f.Up) _focus = (_focus + Rows.Length - 1) % Rows.Length;
            if (f.Down) _focus = (_focus + 1) % Rows.Length;
            int picked = f.Enter ? _focus : f.Click && _hover >= 0 ? _hover : -1;
            if (picked >= 0)
                _host.FlyLevel(LevelCatalog.Levels[picked].Id);
            if (f.Back)
                _host.GoTo(SimState.ScreenMenu);
        }

        int HitTest(UiFrame f)
        {
            for (int i = 0; i < Rows.Length; i++)
                if (f.PointIsIn(RowX, RowY + i * RowGap, RowW, RowH))
                    return i;
            return -1;
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            // Same language as the title: a crate standing on calm sky, its paper tag doing the
            // labelling the way the sheet's sections do.
            Kit.MenuBackdrop(canvas);
            Kit.Panel(canvas, new Rectangle(150, 56, 340, 200), "CHOOSE COURSE");

            for (int i = 0; i < Rows.Length; i++)
            {
                int y = RowY + i * RowGap;
                var bounds = new Rectangle(RowX, y, RowW, RowH);
                bool isCurrent = string.Equals(LevelCatalog.Levels[i].Id, state.LevelId, System.StringComparison.OrdinalIgnoreCase);
                Kit.Button(canvas, bounds, LevelCatalog.Levels[i].NameUpper,
                    i == _hover ? Kit.UiState.Hover : Kit.UiState.Rest, focused: i == _focus);
                PixelFont.DrawCenteredShadowed(canvas, Rows[i], Sim.DesignWidth / 2, y + RowH + 4, Theme.SignCream);
                if (isCurrent)
                    Kit.Tag(canvas, RowX + RowW + 8, y + 2, "FLYING");   // sits inside the crate, beside the slat
            }

            PixelFont.DrawCenteredShadowed(canvas, "ENTER LAUNCHES. ESC GOES BACK.",
                Sim.DesignWidth / 2, 274, Theme.SignCream);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }
    }
}
