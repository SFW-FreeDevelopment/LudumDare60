using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Balloonatic.Rendering
{
    /// <summary>
    /// A 5x7 pixel font, authored by hand rather than loaded: no content pipeline, no font file in the
    /// WebGL payload, identical pixels on every head, and legible at the design resolution's integer scales.
    /// Lowercase maps to uppercase — this is a 16-bit UI face, and the display lettering that needs real
    /// personality is drawn as art (the title logo), not set in a font.
    ///
    /// Rows are written as strings so the glyphs can be read and reviewed as pictures.
    /// </summary>
    static class PixelFont
    {
        public const int CellWidth = 5;
        public const int CellHeight = 7;
        public const int Advance = 6;
        public const int LineHeight = 9;

        static readonly Dictionary<char, string[]> Glyphs = BuildGlyphs();

        static Dictionary<char, string[]> BuildGlyphs()
        {
            var glyphs = new Dictionary<char, string[]>
            {
                ['&'] = Split(".##..|#..#.|#.#..|.##..|#.#.#|#..#.|.##.#"),
                ['A'] = Split(".###.|#...#|#...#|#####|#...#|#...#|#...#"),
                ['B'] = Split("####.|#...#|#...#|####.|#...#|#...#|####."),
                ['C'] = Split(".###.|#...#|#....|#....|#....|#...#|.###."),
                ['D'] = Split("###..|#..#.|#...#|#...#|#...#|#..#.|###.."),
                ['E'] = Split("#####|#....|#....|####.|#....|#....|#####"),
                ['F'] = Split("#####|#....|#....|####.|#....|#....|#...."),
                ['G'] = Split(".###.|#...#|#....|#.###|#...#|#...#|.###."),
                ['H'] = Split("#...#|#...#|#...#|#####|#...#|#...#|#...#"),
                ['I'] = Split("#####|..#..|..#..|..#..|..#..|..#..|#####"),
                ['J'] = Split("..###|...#.|...#.|...#.|...#.|#..#.|.##.."),
                ['K'] = Split("#...#|#..#.|#.#..|##...|#.#..|#..#.|#...#"),
                ['L'] = Split("#....|#....|#....|#....|#....|#....|#####"),
                ['M'] = Split("#...#|##.##|#.#.#|#.#.#|#...#|#...#|#...#"),
                ['N'] = Split("#...#|##..#|#.#.#|#..##|#...#|#...#|#...#"),
                ['O'] = Split(".###.|#...#|#...#|#...#|#...#|#...#|.###."),
                ['P'] = Split("####.|#...#|#...#|####.|#....|#....|#...."),
                ['Q'] = Split(".###.|#...#|#...#|#...#|#.#.#|#..#.|.##.#"),
                ['R'] = Split("####.|#...#|#...#|####.|#.#..|#..#.|#...#"),
                ['S'] = Split(".####|#....|#....|.###.|....#|....#|####."),
                ['T'] = Split("#####|..#..|..#..|..#..|..#..|..#..|..#.."),
                ['U'] = Split("#...#|#...#|#...#|#...#|#...#|#...#|.###."),
                ['V'] = Split("#...#|#...#|#...#|#...#|#...#|.#.#.|..#.."),
                ['W'] = Split("#...#|#...#|#...#|#.#.#|#.#.#|##.##|#...#"),
                ['X'] = Split("#...#|#...#|.#.#.|..#..|.#.#.|#...#|#...#"),
                ['Y'] = Split("#...#|#...#|.#.#.|..#..|..#..|..#..|..#.."),
                ['Z'] = Split("#####|....#|...#.|..#..|.#...|#....|#####"),

                ['0'] = Split(".###.|#...#|#..##|#.#.#|##..#|#...#|.###."),
                ['1'] = Split("..#..|.##..|..#..|..#..|..#..|..#..|.###."),
                ['2'] = Split(".###.|#...#|....#|...#.|..#..|.#...|#####"),
                ['3'] = Split("####.|....#|....#|.###.|....#|....#|####."),
                ['4'] = Split("...#.|..##.|.#.#.|#..#.|#####|...#.|...#."),
                ['5'] = Split("#####|#....|#....|####.|....#|....#|####."),
                ['6'] = Split("..##.|.#...|#....|####.|#...#|#...#|.###."),
                ['7'] = Split("#####|....#|...#.|..#..|.#...|.#...|.#..."),
                ['8'] = Split(".###.|#...#|#...#|.###.|#...#|#...#|.###."),
                ['9'] = Split(".###.|#...#|#...#|.####|....#|...#.|.##.."),

                ['.'] = Split(".....|.....|.....|.....|.....|.##..|.##.."),
                [','] = Split(".....|.....|.....|.....|.##..|.##..|.#..."),
                [':'] = Split(".....|.##..|.##..|.....|.##..|.##..|....."),
                ['!'] = Split("..#..|..#..|..#..|..#..|..#..|.....|..#.."),
                ['?'] = Split(".###.|#...#|....#|...#.|..#..|.....|..#.."),
                ['\''] = Split("..#..|..#..|.....|.....|.....|.....|....."),
                ['-'] = Split(".....|.....|.....|.###.|.....|.....|....."),
                ['/'] = Split("....#|....#|...#.|..#..|.#...|#....|#...."),
                ['+'] = Split(".....|..#..|..#..|#####|..#..|..#..|....."),
                ['='] = Split(".....|.....|#####|.....|#####|.....|....."),
                ['('] = Split("...#.|..#..|.#...|.#...|.#...|..#..|...#."),
                [')'] = Split(".#...|..#..|...#.|...#.|...#.|..#..|.#..."),
                ['%'] = Split("##..#|##..#|...#.|..#..|.#...|#..##|#..##"),
                ['*'] = Split(".....|#.#.#|.###.|#####|.###.|#.#.#|....."),
                ['>'] = Split(".#...|..#..|...#.|....#|...#.|..#..|.#..."),
                ['<'] = Split("...#.|..#..|.#...|#....|.#...|..#..|...#."),
                ['♥'] = Split(".#.#.|#####|.###.|..#..|.....|.....|....."),
            };

            return glyphs;
        }

        static string[] Split(string rows) => rows.Split('|');

        /// <summary>Width of a string in pixels at a given scale, for centring and for layout math.</summary>
        public static int Width(string text, int scale = 1)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            return text.Length * Advance * scale - scale;
        }

        public static int Height(int scale = 1) => CellHeight * scale;

        /// <summary>Draws text with its top-left at (x, y). Everything is pixel-snapped integers.</summary>
        public static void Draw(DesignCanvas canvas, string text, int x, int y, Color color, int scale = 1)
        {
            if (string.IsNullOrEmpty(text))
                return;

            int cursor = x;
            foreach (char raw in text)
            {
                char ch = char.ToUpperInvariant(raw);
                if (ch == ' ')
                {
                    cursor += Advance * scale;
                    continue;
                }

                string[] rows = Glyphs.TryGetValue(ch, out string[]? found) && found != null
                    ? found
                    : Glyphs['?'];

                for (int row = 0; row < CellHeight; row++)
                {
                    string cells = rows[row];
                    int runStart = -1;
                    for (int column = 0; column <= CellWidth; column++)
                    {
                        bool on = column < CellWidth && cells[column] == '#';
                        if (on && runStart < 0)
                            runStart = column;
                        if (!on && runStart >= 0)
                        {
                            // Horizontal runs are filled as one rectangle: fewer draws per glyph matters at
                            // 60 fps in a browser tab, and it is the same picture.
                            canvas.Fill(cursor + runStart * scale, y + row * scale,
                                (column - runStart) * scale, scale, color);
                            runStart = -1;
                        }
                    }
                }

                cursor += Advance * scale;
            }
        }

        /// <summary>
        /// Text as the sheet draws it: a hard one-pixel shadow down-right, so cream lettering stays readable
        /// on wood and no anti-aliasing is involved anywhere.
        /// </summary>
        public static void DrawShadowed(DesignCanvas canvas, string text, int x, int y, Color color,
            int scale = 1, Color? shadow = null)
        {
            Draw(canvas, text, x + scale, y + scale, shadow ?? Theme.Ink, scale);
            Draw(canvas, text, x, y, color, scale);
        }

        public static void DrawCentered(DesignCanvas canvas, string text, int centerX, int y, Color color,
            int scale = 1)
        {
            Draw(canvas, text, centerX - Width(text, scale) / 2, y, color, scale);
        }

        public static void DrawCenteredShadowed(DesignCanvas canvas, string text, int centerX, int y,
            Color color, int scale = 1, Color? shadow = null)
        {
            DrawShadowed(canvas, text, centerX - Width(text, scale) / 2, y, color, scale, shadow);
        }
    }
}
