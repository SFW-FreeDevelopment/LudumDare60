using Microsoft.Xna.Framework;

namespace Balloonatic.Rendering
{
    // The same rectangles drive drawing, hit testing and browser input tests.
    static class UiLayout
    {
        public static readonly Rectangle Result = new Rectangle(184, 248, 272, 36);
        public static readonly Rectangle Pause = new Rectangle(600, 8, 32, 28);
        public static Rectangle Menu(int i) => new Rectangle(280,140+i*40,320,32);
        public static Rectangle Setting(int i) => new Rectangle(96,80+i*36,448,32);
        public static Rectangle Course(int i) => new Rectangle(218,76+i*80,338,40);
        public static Rectangle PauseMenu(int i) => new Rectangle(176,108+i*44,288,36);
        public static readonly Rectangle Back = new Rectangle(224,312,192,32);
        public static readonly Rectangle Voyage=new Rectangle(200,312,240,32);
        public static readonly Rectangle Replay=new Rectangle(280,240,304,32);
        public static readonly Rectangle EndTitle=new Rectangle(280,284,304,32);
        static string Rect(Rectangle r) => r.X + "," + r.Y + "," + r.Width + "," + r.Height;
        public static string Describe(DesignCanvas canvas, string screen, bool complete) {
            string value="body="+Kit.Body(canvas)+";screen="+screen+";pause="+Rect(Pause)+(complete?";result="+Rect(Result):"");
            if(screen=="menu") for(int i=0;i<4;i++)value+=";"+new[]{"start","levels","settings","credits"}[i]+"="+Rect(Menu(i));
            if(screen=="settings") for(int i=0;i<6;i++)value+=";"+new[]{"music","sfx","assist","motion","defaults","back"}[i]+"="+Rect(Setting(i));
            if(screen=="levels") {for(int i=0;i<3;i++)value+=";course"+i+"="+Rect(Course(i));value+=";back="+Rect(Back);}
            if(screen=="credits")value+=";back="+Rect(Back);
            if(screen=="pause")for(int i=0;i<4;i++)value+=";"+new[]{"resume","restart","settings","title"}[i]+"="+Rect(PauseMenu(i));
            if(screen=="worldmap")value+=";voyage="+Rect(Voyage);
            if(screen=="ending")value+=";replay="+Rect(Replay)+";title="+Rect(EndTitle);
            return value;
        }
    }
}
