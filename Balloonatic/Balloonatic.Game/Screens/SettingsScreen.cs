using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// Settings (I7): the four switches the entry needs — two volumes (music and sound; the sounds
    /// themselves arrive with I11, the numbers are the contract it honours), Wind Assist for gentler
    /// air, and Reduced Motion, the promise to the motion-sickness rule that governs this whole entry.
    /// Changes apply the moment they are made. Keyboard and mouse reach every value: arrows walk the
    /// rows, Left/Right (or clicking the chevrons) moves the value, Enter lands on RESTORE and BACK.
    /// </summary>
    sealed class SettingsScreen : IUiScreen
    {
        static readonly string[] RowLabels = { "MUSIC VOL", "SOUND VOL", "WIND ASSIST", "REDUCED MOTION" };

        const int ValueRows = 4;
        const int RestoreRow = 4;
        const int BackRow = 5;
        const int RowCount = 6;

        const int PanelX = 150;
        const int RowX = 176;
        const int RowY = 92;
        const int RowW = 288;
        const int RowH = 16;
        const int RowGap = 26;

        readonly UiHost _host;
        int _focus;
        int _hover = -1;

        public SettingsScreen(UiHost host) => _host = host;

        public string Id => SimState.ScreenSettings;

        public void Tick(UiFrame f)
        {
            _hover = HitTest(f);
            if (f.Up) _focus = (_focus + RowCount - 1) % RowCount;
            if (f.Down) _focus = (_focus + 1) % RowCount;

            if (f.Left) Adjust(_focus, -1);
            if (f.Right) Adjust(_focus, +1);
            if (f.Enter) Activate(_focus);

            // Mouse: a row click focuses and activates; clicks on the chevron ends of a value row move
            // the value, which is what a chevron on screen invites.
            if (f.Click && _hover >= 0)
            {
                _focus = _hover;
                if (_hover < ValueRows)
                {
                    int y = RowY + _hover * RowGap;
                    if (f.PointIsIn(RowX, y, 20, RowH)) Adjust(_hover, -1);
                    else if (f.PointIsIn(RowX + RowW - 20, y, 20, RowH)) Adjust(_hover, +1);
                    else Activate(_hover);
                }
                else
                {
                    Activate(_hover);
                }
            }

            if (f.Back)
                _host.GoTo(SimState.ScreenMenu);
        }

        void Activate(int row)
        {
            switch (row)
            {
                case RestoreRow:
                    AppSettings.RestoreDefaults();
                    _host.LiveSettingsChanged();
                    break;
                case BackRow:
                    _host.GoTo(SimState.ScreenMenu);
                    break;
                default:
                    Adjust(row, +1);   // Enter nudges a value forward; volumes wrap, switches flip
                    break;
            }
        }

        void Adjust(int row, int delta)
        {
            switch (row)
            {
                case 0: AppSettings.MusicVolume = Wrap(AppSettings.MusicVolume + delta); break;
                case 1: AppSettings.SoundVolume = Wrap(AppSettings.SoundVolume + delta); break;
                case 2: AppSettings.WindAssist = !AppSettings.WindAssist; break;
                case 3: AppSettings.ReducedMotion = !AppSettings.ReducedMotion; break;
                default: return;
            }
            _host.LiveSettingsChanged();
        }

        static int Wrap(int v) => v < 0 ? AppSettings.VolumeMax : v > AppSettings.VolumeMax ? 0 : v;

        int HitTest(UiFrame f)
        {
            for (int i = 0; i < RowCount; i++)
                if (f.PointIsIn(RowX, RowY + i * RowGap, RowW, RowH))
                    return i;
            return -1;
        }

        public void Draw(DesignCanvas canvas, SimState state)
        {
            // Crate on calm sky, like the rest of the family; the rows ride inside it.
            Kit.MenuBackdrop(canvas);
            Kit.Panel(canvas, new Rectangle(150, 44, 340, 256), "SETTINGS");

            for (int i = 0; i < ValueRows; i++)
                DrawValueRow(canvas, i, RowY + i * RowGap, i == _focus, i == _hover);

            var restore = new Rectangle(RowX, RowY + RestoreRow * RowGap, RowW, RowH);
            var back = new Rectangle(RowX, RowY + BackRow * RowGap, RowW, RowH);
            Kit.Button(canvas, restore, "RESTORE DEFAULTS",
                _hover == RestoreRow ? Kit.UiState.Hover : Kit.UiState.Rest, focused: _focus == RestoreRow);
            Kit.Button(canvas, back, "BACK TO TITLE",
                _hover == BackRow ? Kit.UiState.Hover : Kit.UiState.Rest, focused: _focus == BackRow);

            PixelFont.DrawCenteredShadowed(canvas, "CHANGES APPLY AT ONCE. SOUNDS PLAY WHEN THEY SHIP.",
                Sim.DesignWidth / 2, 260, Theme.SignCream);
            PixelFont.DrawCenteredShadowed(canvas, "REDUCED MOTION STILLS THE LOOKBACK AND THE FLICKER.",
                Sim.DesignWidth / 2, 274, Theme.SignCream);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        void DrawValueRow(DesignCanvas canvas, int row, int y, bool focused, bool hovered)
        {
            var well = new Rectangle(RowX, y, RowW, RowH);
            canvas.CutRectangle(well.X, well.Y, well.Width, well.Height, 1, hovered ? Theme.WoodDark : Theme.Dim);
            if (focused)
                canvas.Outline(new Rectangle(well.X - 2, well.Y - 2, well.Width + 4, well.Height + 4), Theme.Cream);

            // Label starts clear of the left chevron — the arrow is a control, not an initial.
            PixelFont.Draw(canvas, RowLabels[row], well.X + 18, y + 4, Theme.TagCream);

            // Chevron ends: the same < > the wind readout uses, dimmed unless the row holds focus.
            Color arrow = focused ? Theme.TagCream : Theme.WoodLight;
            PixelFont.Draw(canvas, "<", well.X + 6, y + 4, arrow);
            PixelFont.Draw(canvas, ">", well.Right - 12, y + 4, arrow);

            switch (row)
            {
                case 0: DrawVolume(canvas, AppSettings.MusicVolume, well); break;
                case 1: DrawVolume(canvas, AppSettings.SoundVolume, well); break;
                case 2: DrawSwitch(canvas, AppSettings.WindAssist, well); break;
                case 3: DrawSwitch(canvas, AppSettings.ReducedMotion, well); break;
            }
        }

        /// <summary>Volume as a ten-notch strip: the number is for tests, the lit notches are for the eye.</summary>
        static void DrawVolume(DesignCanvas canvas, int value, Rectangle well)
        {
            int stripX = well.X + 150;
            for (int notch = 0; notch <= AppSettings.VolumeMax; notch++)
                canvas.Fill(stripX + notch * 5, well.Y + 6, 4, 4, notch < value ? Theme.FlameYellow : Theme.WoodDark);
            PixelFont.DrawShadowed(canvas, value + "/" + AppSettings.VolumeMax, well.Right - 52, well.Y + 4, Theme.TagCream);
        }

        static void DrawSwitch(DesignCanvas canvas, bool on, Rectangle well)
        {
            PixelFont.DrawShadowed(canvas, on ? "ON" : "OFF", well.X + 172, well.Y + 4,
                on ? Theme.StarGold : Theme.SignCream);
        }
    }
}
