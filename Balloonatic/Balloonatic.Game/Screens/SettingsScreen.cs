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
        static readonly string[] RowLabels = { "MUSIC", "SOUND", "WIND ASSIST", "REDUCED MOTION" };

        const int ValueRows = 4;
        const int RestoreRow = 4;
        const int BackRow = 5;
        const int RowCount = 6;

        const int RowX=96, RowY=80, RowW=448, RowH=32, RowGap=36;

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

        public void Draw(DesignCanvas canvas,SimState state) {
            Kit.MenuBackdrop(canvas);Kit.Panel(canvas,new Rectangle(64,16,512,328));
            PixelFont.DrawCenteredShadowed(canvas,"SETTINGS",320,30,Theme.TagCream,Kit.Heading(canvas));
            int font=Kit.Body(canvas);
            for(int i=0;i<ValueRows;i++) {
                Rectangle r=UiLayout.Setting(i);
                canvas.CutRectangle(r.X,r.Y,r.Width,r.Height,2,i==_hover?Theme.WoodDark:Theme.Dim);
                if(i==_focus)canvas.Outline(new Rectangle(r.X-2,r.Y-2,r.Width+4,r.Height+4),2,Theme.Cream);
                PixelFont.Draw(canvas,RowLabels[i],r.X+32,r.Y+5,Theme.TagCream,font);
                PixelFont.Draw(canvas,"<",r.X+5,r.Y+5,Theme.SignCream,font);
                PixelFont.Draw(canvas,">",r.Right-21,r.Y+5,Theme.SignCream,font);
                string value=i==0?AppSettings.MusicVolume+"/10":i==1?AppSettings.SoundVolume+"/10":(i==2?AppSettings.WindAssist:AppSettings.ReducedMotion)?"ON":"OFF";
                PixelFont.DrawShadowed(canvas,value,r.Right-32-PixelFont.Width(value,font),r.Y+5,Theme.StarGold,font);
            }
            Kit.Button(canvas,UiLayout.Setting(RestoreRow),"RESTORE DEFAULTS",_hover==RestoreRow?Kit.UiState.Hover:Kit.UiState.Rest,_focus==RestoreRow);
            Kit.Button(canvas,UiLayout.Setting(BackRow),"BACK TO TITLE",_hover==BackRow?Kit.UiState.Hover:Kit.UiState.Rest,_focus==BackRow);
            PixelFont.DrawCenteredShadowed(canvas,"ARROWS ADJUST / 0 MUTES",320,312,Theme.SignCream,font);
        }
    }
}
