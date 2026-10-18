using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;
namespace Balloonatic.Screens {
    sealed class LevelSelectScreen : IUiScreen {
        readonly UiHost _host; int _focus; int _hover=-1; bool _backHover;
        public LevelSelectScreen(UiHost host)=>_host=host;
        public string Id=>SimState.ScreenLevels;
        public void Tick(UiFrame f) {
            _hover=-1;
            for(int i=0;i<3;i++){Rectangle r=UiLayout.Course(i);if(f.PointIsIn(r.X,r.Y,r.Width,r.Height))_hover=i;}
            Rectangle b=UiLayout.Back;_backHover=f.PointIsIn(b.X,b.Y,b.Width,b.Height);
            if(f.Up)_focus=(_focus+3)%4;if(f.Down)_focus=(_focus+1)%4;
            int chosen=f.Enter?_focus:f.Click&&_backHover?3:f.Click?_hover:-1;
            if(chosen==3||f.Back)_host.GoTo(SimState.ScreenMenu);
            else if(chosen>=0)_host.FlyLevel(LevelCatalog.Levels[chosen].Id);
        }
        public void Draw(DesignCanvas canvas,SimState state) {
            Kit.MenuBackdrop(canvas);Kit.Panel(canvas,new Rectangle(48,16,544,336));
            PixelFont.DrawCenteredShadowed(canvas,"CHOOSE A SKY",320,28,Theme.TagCream,Kit.Heading(canvas));
            string[] lessons={"GENTLE BREEZES","OPPOSING WINDS","GARDEN GUSTS"};
            for(int i=0;i<3;i++){
                LevelDef level=LevelCatalog.Levels[i];Rectangle r=UiLayout.Course(i);
                Sprites.Draw(canvas,"island-delivery",78,r.Y+30);
                Sprites.Draw(canvas,WorldArt.Landmark(level,false),106,r.Y-21);
                Kit.Button(canvas,r,level.NameUpper,i==_hover?Kit.UiState.Hover:Kit.UiState.Rest,focused:i==_focus);
                PixelFont.DrawCenteredShadowed(canvas,lessons[i],r.X+r.Width/2,r.Bottom+7,Theme.SignCream,Kit.Body(canvas));
            }
            Kit.Button(canvas,UiLayout.Back,"BACK",_backHover?Kit.UiState.Hover:Kit.UiState.Rest,focused:_focus==3);
        }
    }
}
