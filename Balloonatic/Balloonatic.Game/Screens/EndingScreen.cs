using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;
namespace Balloonatic.Screens {
    sealed class EndingScreen:IUiScreen {
        readonly UiHost _host; int _focus; int _hover=-1;
        public EndingScreen(UiHost host)=>_host=host;
        public string Id=>SimState.ScreenEnding;
        public void Tick(UiFrame f) {
            _hover=-1;Rectangle[] rows={UiLayout.Replay,UiLayout.EndTitle};
            for(int i=0;i<rows.Length;i++){Rectangle r=rows[i];if(f.PointIsIn(r.X,r.Y,r.Width,r.Height))_hover=i;}
            if(f.Up||f.Down)_focus=1-_focus;
            int action=f.Enter?_focus:f.Click?_hover:-1;
            if(action==0)_host.FlyLevel(LevelCatalog.DefaultId);
            if(action==1||f.Back)_host.GoTo(SimState.ScreenMenu);
        }
        public void Draw(DesignCanvas canvas,SimState state) {
            Kit.VoyageBackdrop(canvas);
            Sprites.Draw(canvas,"balloon-happy-0",42,68,scale:3);
            Sprites.Draw(canvas,"bag-falling",59,260,scale:3);Sprites.Draw(canvas,"bag-falling",182,260,scale:3);
            Kit.Panel(canvas,new Rectangle(256,48,352,284));int font=Kit.Body(canvas);
            PixelFont.DrawCenteredShadowed(canvas,"SWEET DREAMS,",432,72,Theme.TagCream,Kit.Heading(canvas));
            PixelFont.DrawCenteredShadowed(canvas,"CHARLOTTE.",432,112,Theme.HeartPink,Kit.Heading(canvas));
            PixelFont.DrawCenteredShadowed(canvas,"LOVE, MOM & DAD.",432,172,Theme.TagCream,font);
            PixelFont.DrawCenteredShadowed(canvas,"♥",432,208,Theme.HeartPink,font);
            Kit.Button(canvas,UiLayout.Replay,"FLY AGAIN",_hover==0?Kit.UiState.Hover:Kit.UiState.Rest,_focus==0);
            Kit.Button(canvas,UiLayout.EndTitle,"TITLE",_hover==1?Kit.UiState.Hover:Kit.UiState.Rest,_focus==1);
        }
    }
}
