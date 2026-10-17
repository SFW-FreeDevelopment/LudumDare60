using Microsoft.Xna.Framework;

namespace Balloonatic
{
    /// <summary>
    /// The only place a colour value may appear in C# (AGENTS.md, styling rules). Named for the thing it
    /// paints, not for its hue. Every value is sampled from <c>docs/reference/concept-sheet.png</c> —
    /// run <c>tools/sample-palette.ps1</c> to regenerate <c>docs/reference/palette.txt</c> — except where a
    /// ramp needs a step the sheet does not happen to contain, which is marked INTERPOLATED below.
    ///
    /// There are no CRT colours here on purpose (docs/CONCEPT.md Amendment 2): these are flat daytime sky,
    /// warm wood and wicker colours. "Old Tech" comes from what the balloon is built out of, not from a
    /// filter laid over the picture.
    /// </summary>
    static class Theme
    {
        // ---- chrome: the entry's own framing is warm wood and cream paper, like the sheet's menus ----

        /// <summary>Outlines and lettering shadows in the art are this near-black navy, never pure black.</summary>
        public static readonly Color Ink = new Color(0x0B, 0x11, 0x29);
        public static readonly Color PanelBrown = new Color(0x37, 0x22, 0x29);    // panel interiors, page ground
        public static readonly Color WoodDark = new Color(0x65, 0x3A, 0x38);      // borders, shaded faces
        public static readonly Color WoodMid = new Color(0xB7, 0x63, 0x3D);       // button faces, slats, platforms
        public static readonly Color WoodLight = new Color(0xBD, 0x73, 0x3E);     // lit top and left edges, rope
        public static readonly Color WickerDark = new Color(0x87, 0x54, 0x49);    // basket body
        public static readonly Color WickerLight = new Color(0xD2, 0xB3, 0x83);   // basket weave highlight
        public static readonly Color TagCream = new Color(0xFB, 0xE8, 0xC7);      // paper labels, signs
        public static readonly Color SignCream = new Color(0xF6, 0xC2, 0x8B);     // warm paper, wood-grain lit
        public static readonly Color Cream = new Color(0xFA, 0xF6, 0xF9);         // text on wood, cloud tops

        // ---- sky: the bands are the game's steering, so they are locked, saturated and distinct ----
        public static readonly Color NightIndigo = new Color(0x1D, 0x1D, 0x44);   // above the top band
        public static readonly Color NightBlue = new Color(0x07, 0x24, 0x67);     // top band
        public static readonly Color SkyHigh = new Color(0x18, 0x49, 0xAA);       // upper middle band
        public static readonly Color SkyMid = new Color(0x37, 0x75, 0xD7);        // middle band
        public static readonly Color SkyLow = new Color(0x57, 0xA7, 0xFA);        // lower band
        public static readonly Color SkyPale = new Color(0x87, 0xA8, 0xF8);       // horizon haze
        public static readonly Color DuskTeal = new Color(0x69, 0xBA, 0xC5);      // distant haze, water light

        public static readonly Color CloudLavender = new Color(0xD7, 0xD7, 0xFB); // cloud body
        public static readonly Color CloudShade = new Color(0xB6, 0xB8, 0xFA);    // cloud underside
        public static readonly Color CloudWhite = new Color(0xFA, 0xF6, 0xF9);    // cloud tops, rim light

        // ---- the balloon: striped fabric, flame, rigging ----
        public static readonly Color BalloonCream = new Color(0xFA, 0xF6, 0xF9);
        public static readonly Color BalloonPinkLight = new Color(0xFB, 0x97, 0x96);
        public static readonly Color BalloonPink = new Color(0xF9, 0x67, 0x77);
        public static readonly Color BalloonRed = new Color(0xD1, 0x24, 0x4B);
        public static readonly Color BalloonPlum = new Color(0xA9, 0x2B, 0x64);   // fabric shadow, pink outline
        public static readonly Color FlameYellow = new Color(0xFF, 0xC2, 0x1D);   // burner core, gauge hot end
        public static readonly Color FlameOrange = new Color(0xFF, 0x93, 0x21);   // burner body
        public static readonly Color FlameRed = new Color(0xE8, 0x4E, 0x1B);      // burner edge, danger

        // ---- world: islands, water, planting ----
        public static readonly Color LeafGreen = new Color(0x36, 0xA5, 0x65);     // island caps, hedges
        public static readonly Color GrassLight = new Color(0x7C, 0xD4, 0x87);    // INTERPOLATED sunlit grass edge
        public static readonly Color MossGreen = new Color(0x23, 0x7A, 0x54);     // grass shadow
        public static readonly Color BarkBrown = new Color(0x8D, 0x57, 0x26);     // trunks, masts, posts
        public static readonly Color RockSlate = new Color(0x87, 0x99, 0xA5);     // island rock faces
        public static readonly Color RockDark = new Color(0x58, 0x56, 0x87);      // rock underside
        public static readonly Color WaterPale = new Color(0x7F, 0xD7, 0xE0);     // falls, foam
        public static readonly Color WaterCyan = new Color(0x5E, 0xC6, 0xF1);     // falls, stream

        // ---- Charlotte and her things ----
        public static readonly Color Skin = new Color(0xF6, 0xC2, 0x8B);
        public static readonly Color SkinShadow = new Color(0xF4, 0x85, 0x58);    // cheek, shading
        public static readonly Color HairBrown = new Color(0x8D, 0x57, 0x26);
        public static readonly Color DressPink = new Color(0xF9, 0x74, 0xB5);
        public static readonly Color SandTan = new Color(0xD2, 0xB3, 0x83);       // ballast sandbags

        // ---- readouts: the few saturated colours are reserved for information, never decoration ----
        public static readonly Color StarGold = new Color(0xF1, 0xBF, 0x37);      // stars, delivery rating
        public static readonly Color HeartPink = new Color(0xF7, 0x3E, 0x6A);     // hearts
        public static readonly Color HeartEmpty = new Color(0x74, 0x53, 0x4F);    // INTERPOLATED spent heart
        public static readonly Color WindLowBand = new Color(0xD7, 0xD7, 0xFB);   // wind marks, by altitude
        public static readonly Color WindMidBand = new Color(0xFF, 0xC2, 0x1D);
        public static readonly Color WindHighBand = new Color(0x5E, 0xC6, 0xF1);

        /// <summary>Meter wells and gauge backs: brown pushed toward black so the fill is the only light.</summary>
        public static readonly Color Dim = new Color(0x24, 0x1A, 0x1E);
        public static readonly Color LetterBox = new Color(0x07, 0x10, 0x22);     // outside the design frame

        /// <summary>The pause veil: dusk drawn over the held flight — one colour with an alpha, nothing more.</summary>
        public static readonly Color PauseVeil = new Color(11, 17, 41, 150);

        /// <summary>
        /// Shade steps for anything with thickness (panels, buttons, baskets): index 0 is the lit top/left
        /// edge, 3 the shadowed bottom/right. Everything raised uses this ramp so the whole UI is modelled
        /// the same way, with one light direction.
        /// </summary>
        public static readonly Color[] WoodRamp = { WoodLight, WoodMid, WoodDark, Ink };
    }
}
