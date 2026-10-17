using Microsoft.Xna.Framework;

namespace Balloonatic
{
    /// <summary>
    /// Placeholder palette for the I0 harness. Initiative I1 replaces this file with <c>Theme.cs</c>
    /// carrying the sampled 16-color palette from docs/reference/concept-sheet.md, and it becomes the
    /// only place a color value may appear in C# (AGENTS.md, styling rules). No CRT filter anywhere:
    /// these are flat sky/prop colors, not phosphor ones.
    /// </summary>
    static class Palette
    {
        // Sky bands, low to high. Different tints per band are what make altitude readable.
        public static readonly Color BandLow = new Color(46, 58, 96);
        public static readonly Color BandMid = new Color(62, 78, 122);
        public static readonly Color BandHigh = new Color(92, 112, 158);

        public static readonly Color WindLow = new Color(126, 148, 186);
        public static readonly Color WindMid = new Color(198, 141, 106);
        public static readonly Color WindHigh = new Color(232, 213, 139);

        public static readonly Color Envelope = new Color(225, 106, 124);
        public static readonly Color EnvelopeAccent = new Color(250, 233, 221);
        public static readonly Color Basket = new Color(126, 86, 50);
        public static readonly Color Flame = new Color(255, 190, 92);

        public static readonly Color Letterbox = Color.Black;
        public static readonly Color Frame = new Color(28, 34, 56);
        public static readonly Color MeterBack = new Color(24, 29, 48);
    }
}
