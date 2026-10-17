using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Balloonatic.Rendering
{
    /// <summary>
    /// The Aseprite sheet (art/export/sheet.png + slices.json, built by art/.art) loaded once from
    /// embedded resources, so the desktop head and the WebGL head read identical bytes through one
    /// stream and nothing depends on a file on disk. Sprites are authored at internal-pixel density
    /// (2 per design px); Draw takes design coordinates and lands whole internal pixels, matching
    /// DesignCanvas' half-pixel camera — crisp at every anchor, on both heads.
    /// </summary>
    static class Sprites
    {
        static readonly Dictionary<string, Rectangle> Tables = new Dictionary<string, Rectangle>(StringComparer.Ordinal);
        static Texture2D? _sheet;

        public static bool Ready => _sheet != null;

        /// <summary>Design-pixel size of a sprite; callers use it to centre or anchor.</summary>
        public static Vector2 Size(string name)
        {
            Rectangle r = Tables[name];
            return new Vector2(r.Width / DesignCanvas.Px, r.Height / DesignCanvas.Px);
        }

        public static void Load(GraphicsDevice device)
        {
            if (_sheet != null)
                return;

            Assembly asm = typeof(Sprites).Assembly;
            using (Stream json = OpenResource(asm, "slices.json"))
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                foreach (JsonElement e in doc.RootElement.EnumerateArray())
                {
                    string name = e.GetProperty("name").GetString()
                        ?? throw new InvalidOperationException($"slice entry without a name");
                    Tables[name] = new Rectangle(
                        e.GetProperty("x").GetInt32(), e.GetProperty("y").GetInt32(),
                        e.GetProperty("w").GetInt32(), e.GetProperty("h").GetInt32());
                }
            }

            using (Stream png = OpenResource(asm, "sheet.png"))
                _sheet = Texture2D.FromStream(device, png);
        }

        /// <summary>
        /// Draws a slice with its top-left at design coordinates (x, y). Frames are separate slices
        /// ("balloon-0", "balloon-1", …) — the caller picks the frame from sim state, so a still
        /// state always renders the same frame and screenshots stay byte-stable.
        /// </summary>
        public static void Draw(DesignCanvas canvas, string name, float x, float y, bool flip = false, Color? tint = null)
        {
            if (_sheet == null || !Tables.TryGetValue(name, out Rectangle r))
                return;
            canvas.DrawSprite(_sheet, r, x, y, flip, tint);
        }

        /// <summary>Frame-picked draw for animated sprites: picks "name-frameIndex".</summary>
        public static void Draw(DesignCanvas canvas, string name, int frame, float x, float y, bool flip = false, Color? tint = null) =>
            Draw(canvas, $"{name}-{frame}", x, y, flip, tint);

        static Stream OpenResource(Assembly asm, string suffix)
        {
            foreach (string n in asm.GetManifestResourceNames())
                if (n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return asm.GetManifestResourceStream(n)!;
            throw new InvalidOperationException($"embedded resource ending '{suffix}' is not in the assembly");
        }
    }
}
