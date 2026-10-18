using Microsoft.Xna.Framework;

namespace Balloonatic.Rendering
{
    // The same rectangles drive drawing, hit testing and browser input tests.
    static class UiLayout
    {
        public static readonly Rectangle Result = new Rectangle(184, 248, 272, 36);
        public static readonly Rectangle Pause = new Rectangle(600, 8, 32, 28);
        static string Rect(Rectangle r) => r.X + "," + r.Y + "," + r.Width + "," + r.Height;
        public static string Describe(DesignCanvas canvas, string screen, bool complete) => "body=" + Kit.Body(canvas)
            + ";screen=" + screen + ";pause=" + Rect(Pause) + (complete ? ";result=" + Rect(Result) : "");
    }
}
