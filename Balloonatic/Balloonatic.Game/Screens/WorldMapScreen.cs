using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;
namespace Balloonatic.Screens {
    sealed class WorldMapScreen:IUiScreen {
        readonly UiHost _host; bool _hover;
        static readonly int[] NodeX={112,320,528};
        public WorldMapScreen(UiHost host)=>_host=host;
        public string Id=>SimState.ScreenWorldMap;
        static int Current(SimState state) {for(int i=0;i<LevelCatalog.Levels.Length;i++)if(LevelCatalog.Levels[i].Id==state.LevelId)return i;return 0;}
        public void Tick(UiFrame f) {
            Rectangle r=UiLayout.Voyage;_hover=f.PointIsIn(r.X,r.Y,r.Width,r.Height);
            if(f.Enter||f.Restart||f.Click&&_hover)_host.FlyNextCourse();
            if(f.Back)_host.GoTo(SimState.ScreenMenu);
        }
        public void Draw(DesignCanvas canvas,SimState state) {
            Kit.VoyageBackdrop(canvas);
            int current=Current(state),next=System.Math.Min(2,current+1),font=Kit.Body(canvas);
            bool last=current==2;
            PixelFont.DrawCenteredShadowed(canvas,last?"HOMEWARD BOUND":"THE VOYAGE",320,28,Theme.TagCream,Kit.Heading(canvas));
            PixelFont.DrawCenteredShadowed(canvas,AppProgress.Count+"/3 DELIVERED",320,68,Theme.SignCream,font);
            for(int gap=0;gap<2;gap++)for(int x=NodeX[gap]+72;x<NodeX[gap+1]-64;x+=10)canvas.Fill(x,208,4,2,Theme.StarGold);
            string[][] names={new[]{"NURSERY","SKIES"},new[]{"TOY","ISLES"},new[]{"CLOUD","GARDEN"}};
            for(int i=0;i<3;i++) {
                LevelDef level=LevelCatalog.Levels[i];int x=NodeX[i];
                Sprites.Draw(canvas,"island-delivery",x-60,180);
                Sprites.Draw(canvas,WorldArt.Landmark(level,false),x-6,129);
                Sprites.Draw(canvas,WorldArt.Parcel(level.Package),x-40,179);
                for(int row=0;row<2;row++)PixelFont.DrawCenteredShadowed(canvas,names[i][row],x,244+row*24,AppProgress.Delivered(level.Id)?Theme.TagCream:Theme.SignCream,font);
                for(int star=0;star<3;star++)Sprites.Draw(canvas,star<AppProgress.StarsFor(level.Id)?"star-on":"star-off",x-24+star*20,294,scale:2);
                if(i==next&&!last)Sprites.Draw(canvas,"balloon-0",x-30,92);
            }
            Kit.Button(canvas,UiLayout.Voyage,last?"HOMEWARD ♥":"FLY ON >",_hover?Kit.UiState.Hover:Kit.UiState.Rest,focused:true);
        }
    }
}
