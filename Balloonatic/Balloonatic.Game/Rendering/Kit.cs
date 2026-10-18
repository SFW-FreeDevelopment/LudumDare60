using System;
using Balloonatic.Core;
using Microsoft.Xna.Framework;

namespace Balloonatic.Rendering
{
    /// <summary>
    /// The one shared UI layer every screen draws with (AGENTS.md: no per-screen one-off styling). It knows
    /// the design grid, the wood-and-paper panel language sampled from the concept sheet, and the handful of
    /// saturated colours reserved for readouts. Nothing here reads wall clock, so a screen of kit art is
    /// pixel-reproducible from the state it is given.
    /// </summary>
    static class Kit
    {
        public static void WindLadder(DesignCanvas canvas, SimState state, LevelDef level)
        {
            Panel(canvas, new Rectangle(490, 8, 126, 112));
            PixelFont.DrawCenteredShadowed(canvas, "WIND", 553, 12, Theme.TagCream, 2);
            int current = FlightGuidance.BandIndex(state, level);
            for(int i=level.Bands.Length-1;i>=0;i--)
            {
                int y = 36 + (level.Bands.Length-1-i)*18;
                Color tint = i == 0 ? Theme.WindLowBand : i == 1 ? Theme.WindMidBand : Theme.WindHighBand;
                if(i==current)canvas.Outline(new Rectangle(498,y-2,110,17),Theme.TagCream);
                PixelFont.DrawShadowed(canvas, i==level.Bands.Length-1 ? "HIGH" : i==0 ? "LOW" : "MID",502,y,Theme.TagCream,2);
                WindChevrons(canvas,558,y+3,level.Bands[i].Direction,level.Bands[i].Strength,tint,2);
                if(i==current)canvas.Fill(600,y+1,4,4,Theme.Cream);
            }
        }
        public const int PanelPadding = 8;
        public const int ButtonHeight = 16;
        public const int TagHeight = 11;

        public enum UiState { Rest, Hover, Press }

        // ---------------------------------------------------------------- panels and tags

        /// <summary>
        /// Wood-framed panel: ink outline, warm frame lit top/left and shaded bottom/right, dark interior.
        /// The optional tag is the cream paper label the sheet's own sections use.
        /// </summary>
        public static void Panel(DesignCanvas canvas, Rectangle bounds, string? tag = null)
        {
            canvas.CutRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, 2, Theme.Ink);
            canvas.CutRectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2, 2, Theme.WoodDark);
            canvas.CutRectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4, 1, Theme.PanelBrown);

            canvas.Fill(bounds.X + 3, bounds.Y + 2, bounds.Width - 6, 1, Theme.WoodLight);
            canvas.Fill(bounds.X + 2, bounds.Y + 3, 1, bounds.Height - 6, Theme.WoodLight);
            canvas.Fill(bounds.X + 3, bounds.Bottom - 3, bounds.Width - 6, 1, Theme.Ink);
            canvas.Fill(bounds.Right - 3, bounds.Y + 3, 1, bounds.Height - 6, Theme.Ink);

            if (!string.IsNullOrEmpty(tag))
                Tag(canvas, bounds.X + DesignCanvas.Grid, bounds.Y - 1, tag!);
        }

        /// <summary>Cream paper tab with ink lettering, overlapping the top edge of a panel.</summary>
        public static void Tag(DesignCanvas canvas, int x, int y, string text)
        {
            int width = PixelFont.Width(text) + 6;
            var bounds = new Rectangle(x, y, width, TagHeight);
            canvas.CutRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, 1, Theme.Ink);
            canvas.Fill(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2, Theme.TagCream);
            canvas.Fill(bounds.X + 1, bounds.Bottom - 2, bounds.Width - 2, 1, Theme.SignCream);
            PixelFont.Draw(canvas, text, bounds.X + 3, bounds.Y + 2, Theme.Ink);
        }

        // ---------------------------------------------------------------- buttons

        /// <summary>
        /// Wooden slat button — the sheet's menu items. Rest / hover / press are filled shapes; focus is a
        /// cream ring in the 1 px gap outside, so keyboard-only play is as designed as pointing (AGENTS.md).
        /// </summary>
        public static void Button(DesignCanvas canvas, Rectangle bounds, string label, UiState state,
            bool focused = false)
        {
            int yShift = state == UiState.Press ? 1 : 0;
            var rect = new Rectangle(bounds.X, bounds.Y + yShift, bounds.Width, bounds.Height);

            Color face = state switch
            {
                UiState.Rest => Theme.WoodMid,
                UiState.Hover => Theme.WoodLight,
                _ => Theme.WoodDark,
            };
            Color text = state switch
            {
                UiState.Rest => Theme.Cream,
                UiState.Hover => Theme.TagCream,
                _ => Theme.WoodLight,
            };

            if (focused)
                canvas.Outline(new Rectangle(bounds.X - 2, bounds.Y - 2 + yShift, bounds.Width + 4, bounds.Height + 4), Theme.Cream);

            // Hard shadow under the slat; on press the slat has sunk into its own shadow.
            if (state != UiState.Press)
                canvas.CutRectangle(rect.X, rect.Bottom, rect.Width, 2, 1, Theme.Ink);

            canvas.CutRectangle(rect.X, rect.Y, rect.Width, rect.Height, 2, Theme.Ink);
            canvas.CutRectangle(rect.X + 1, rect.Y + 1, rect.Width - 2, rect.Height - 2, 1, face);
            canvas.Fill(rect.X + 3, rect.Y + 2, rect.Width - 6, 1, state == UiState.Press ? Theme.WoodMid : Theme.WoodLight);
            canvas.Fill(rect.X + 2, rect.Y + 3, 1, rect.Height - 6, state == UiState.Press ? Theme.WoodMid : Theme.WoodLight);
            canvas.Fill(rect.X + 3, rect.Bottom - 3, rect.Width - 6, 1, Theme.WoodDark);
            canvas.Fill(rect.Right - 3, rect.Y + 3, 1, rect.Height - 6, Theme.WoodDark);

            PixelFont.DrawCenteredShadowed(canvas, label, rect.X + rect.Width / 2,
                rect.Y + (rect.Height - PixelFont.CellHeight) / 2, text);

            if (state == UiState.Hover)
                PixelFont.Draw(canvas, ">", rect.X + 4, rect.Y + (rect.Height - PixelFont.CellHeight) / 2, Theme.TagCream);
        }

        // ---------------------------------------------------------------- readouts

        /// <summary>Vertical heat column, hottest colour at the top, flame cap while the burner is open.</summary>
        public static void HeatMeter(DesignCanvas canvas, Rectangle bounds, float fill01, bool lighting)
        {
            canvas.CutRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height, 2, Theme.Ink);
            canvas.Fill(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2, Theme.Dim);

            int inner = bounds.Height - 4;
            int filled = (int)Math.Round(MathHelper.Clamp(fill01, 0f, 1f) * inner);
            for (int row = 0; row < filled; row++)
            {
                // Gradient along the fill rather than a flat block: the gauge should read how close the
                // fabric is to too hot, the way a real thermometer does.
                float position = row / (float)Math.Max(1, inner);
                Color color = position > 0.72f ? Theme.FlameRed : position > 0.4f ? Theme.FlameOrange : Theme.FlameYellow;
                canvas.Fill(bounds.X + 2, bounds.Bottom - 2 - row, bounds.Width - 4, 1, color);
            }

            for (int tick = 0; tick <= 4; tick++)
                canvas.Fill(bounds.X + 2, bounds.Bottom - 2 - inner * tick / 4, 2, 1, Theme.Cream);

            if (lighting)
                Flame(canvas, bounds.X + bounds.Width / 2 - 2, bounds.Y - 5, 0);
        }

        /// <summary>Three-band flame; <paramref name="tick"/> drives the flicker so it derives from sim time.
        /// Float positions because the flame rides a balloon that moves at half-pixel granularity.</summary>
        public static void Flame(DesignCanvas canvas, float x, float y, int tick)
        {
            int widen = tick % 8 < 4 ? 0 : 1;
            canvas.Fill(x + 1, y, 2 + widen, 1, Theme.FlameYellow);
            canvas.Fill(x, y + 1, 4 + widen, 2, Theme.FlameOrange);
            canvas.Fill(x + 1, y + 3, 2, 1, Theme.FlameRed);
        }

        public static void Hearts(DesignCanvas canvas, int x, int y, int filled, int total, int scale = 1)
        {
            for (int i = 0; i < total; i++)
                Heart(canvas, x + i * (8 * scale), y, scale, i < filled);
        }

        public static void Heart(DesignCanvas canvas, int x, int y, int scale, bool filled)
        {
            Color fill = filled ? Theme.HeartPink : Theme.HeartEmpty;
            canvas.Fill(x + scale, y, 2 * scale, scale, fill);
            canvas.Fill(x + 4 * scale, y, 2 * scale, scale, fill);
            canvas.Fill(x, y + scale, 7 * scale, 2 * scale, fill);
            canvas.Fill(x + scale, y + 3 * scale, 5 * scale, scale, fill);
            canvas.Fill(x + 2 * scale, y + 4 * scale, 3 * scale, scale, fill);
            canvas.Fill(x + 3 * scale, y + 5 * scale, scale, scale, fill);
            if (filled)
            {
                canvas.Fill(x + 2 * scale, y + scale, scale, scale, Theme.Cream);
                canvas.Fill(x + 5 * scale, y + 4 * scale, scale, scale, Theme.BalloonPlum);
            }
        }

        public static void Stars(DesignCanvas canvas, int x, int y, int filled, int total, int scale = 1)
        {
            for (int i = 0; i < total; i++)
                Star(canvas, x + i * (8 * scale), y, scale, i < filled);
        }

        public static void Star(DesignCanvas canvas, int x, int y, int scale, bool filled)
        {
            Color fill = filled ? Theme.StarGold : Theme.HeartEmpty;
            canvas.Fill(x + 3 * scale, y, scale, scale, fill);
            canvas.Fill(x + 2 * scale, y + scale, 3 * scale, scale, fill);
            canvas.Fill(x, y + 2 * scale, 7 * scale, scale, fill);
            canvas.Fill(x + scale, y + 3 * scale, 5 * scale, scale, fill);
            canvas.Fill(x + 2 * scale, y + 4 * scale, 3 * scale, scale, fill);
            canvas.Fill(x + scale, y + 5 * scale, scale, scale, fill);
            canvas.Fill(x + 5 * scale, y + 5 * scale, scale, scale, fill);
            if (filled)
                canvas.Fill(x + 2 * scale, y + 2 * scale, scale, scale, Theme.Cream);
        }

        /// <summary>Ballast as sandbags: the sheet's most legible piece of old technology.</summary>
        public static void Sandbags(DesignCanvas canvas, int x, int y, int remaining, int total, int scale = 1)
        {
            for (int i = 0; i < total; i++)
            {
                bool held = i < remaining;
                Color body = held ? Theme.SandTan : Theme.Dim;
                int bx = x + i * (9 * scale);
                canvas.Fill(bx + scale, y, 3 * scale, scale, body);
                canvas.Fill(bx, y + scale, 5 * scale, 4 * scale, body);
                canvas.Fill(bx + scale, y + 5 * scale, 3 * scale, scale, body);
                if (held)
                {
                    canvas.Fill(bx, y + 2 * scale, 5 * scale, scale, Theme.BarkBrown);
                    canvas.Fill(bx + scale, y, scale, scale, Theme.TagCream);
                }
            }
        }

        /// <summary>
        /// Wind as chevrons: count reads strength, the point reads direction. This is the most important
        /// readout in the game — it is the only steering the player has. The ink under-draw is what keeps
        /// them legible on every sky value, including a band whose tint is close to the chevron's own.
        /// </summary>
        public static void WindChevrons(DesignCanvas canvas, float x, float y, int direction, int strength,
            Color color, int scale = 1)
        {
            foreach ((int dx, int dy) in new[] { (1, 0), (0, 1), (1, 1) })
                ChevronShape(canvas, x + dx * scale, y + dy * scale, direction, strength, Theme.Ink, scale);
            ChevronShape(canvas, x, y, direction, strength, color, scale);
        }

        static void ChevronShape(DesignCanvas canvas, float x, float y, int direction, int strength,
            Color color, int scale)
        {
            for (int i = 0; i < Math.Max(1, strength); i++)
            {
                float origin = x + i * 5 * scale;
                for (int step = 0; step < 3; step++)
                {
                    int column = direction >= 0 ? step : 2 - step;
                    canvas.Fill(origin + column * scale, y + step * scale, scale, scale, color);
                    canvas.Fill(origin + column * scale, y + (3 - step) * scale, scale, scale, color);
                }
            }
        }

        /// <summary>
        /// A windsock on a post: the same information as the chevrons but living in the world, so the theme
        /// is carried by the machinery and the player can read the band without looking at the HUD. Big
        /// enough to read at a glance, with bands that taper as the sock empties downwind.
        /// </summary>
        public static void Windsock(DesignCanvas canvas, float x, float y, int direction, int strength)
        {
            int pole = 22;
            canvas.Fill(x, y - pole, 1, pole, Theme.BarkBrown);
            canvas.Fill(x - 1, y - pole - 2, 3, 2, Theme.Ink);
            canvas.Fill(x - 1, y - pole - 1, 3, 1, Theme.StarGold);

            int length = 10 + Math.Clamp(strength, 1, 3) * 5;
            int height = 9;
            for (int i = 0; i < length; i++)
            {
                // A loaded sock hangs full; an empty one droops, so the taper follows strength too.
                float along = i / (float)length;
                int taper = (int)(along * (6 - strength));
                float sx = direction >= 0 ? x + 1 + i : x - 1 - i;
                for (int row = taper; row < height - taper; row++)
                {
                    Color color = (i / 4) % 2 == 0 ? Theme.TagCream : Theme.BalloonRed;
                    canvas.Fill(sx, y - pole + 2 + row, 1, 1, color);
                }
            }
        }

        // ---------------------------------------------------------------- balloon and world stand-ins

        /// <summary>
        /// The calm ground-level sky the menu family (title, courses, settings, credits) stands on:
        /// the two lowest world bands, a strip of turf, two clouds — the same frame the title was
        /// reviewed in, so every menu screen is the same place at the same hour.
        /// </summary>
        public static void MenuBackdrop(DesignCanvas canvas)
        {
            canvas.Fill(0, 0, canvas.Width, 210, Theme.SkyMid);
            canvas.Fill(0, 210, canvas.Width, 120, Theme.SkyLow);
            canvas.Fill(0, 330, canvas.Width, 30, Theme.MossGreen);
            canvas.Fill(0, 330, canvas.Width, 2, Theme.GrassLight);
            canvas.Fill(0, 348, canvas.Width, 12, Theme.WoodDark);
            Sprites.Draw(canvas, "cloud-mid", 36f, 60f);
            Sprites.Draw(canvas, "cloud-small", 506f, 96f);
        }

        static readonly int[] EnvelopeWidths = { 12, 20, 26, 30, 32, 32, 32, 30, 26, 20, 14 };

        /// <summary>
        /// The striped balloon, basket, Charlotte and her bunny, drawn from the kit until I8 hands over the
        /// Aseprite sprites. Stripes follow the sheet: pink and cream panels with a red centre panel.
        /// </summary>
        public static void Balloon(DesignCanvas canvas, float centerX, float top, bool heating, int tick)
        {
            int envelopeHeight = EnvelopeWidths.Length * 2;

            for (int row = 0; row < EnvelopeWidths.Length; row++)
            {
                int width = EnvelopeWidths[row];
                canvas.Fill(centerX - width / 2f - 1, top + row * 2 - 1, width + 2, 4, Theme.Ink);
            }

            Color[] stripes =
            {
                Theme.BalloonPinkLight, Theme.BalloonCream, Theme.BalloonPink, Theme.BalloonCream, Theme.BalloonRed,
                Theme.BalloonCream, Theme.BalloonPink, Theme.BalloonCream, Theme.BalloonPinkLight,
            };
            for (int row = 0; row < EnvelopeWidths.Length; row++)
            {
                int width = EnvelopeWidths[row];
                float left = centerX - width / 2f;
                bool crown = row < 2 || row > EnvelopeWidths.Length - 3;
                for (int stripe = 0; stripe < stripes.Length; stripe++)
                {
                    float x = left + stripe * width / (float)stripes.Length;
                    float next = left + (stripe + 1) * width / (float)stripes.Length;
                    Color color = crown ? Color.Lerp(stripes[stripe], Theme.Cream, 0.3f) : stripes[stripe];
                    canvas.Fill(x, top + row * 2, Math.Max(1f, next - x), 2, color);
                }
                // The fabric is round: the last column of every row sits in shade.
                canvas.Fill(left + width - 2, top + row * 2, 2, 2, Color.Lerp(stripes[stripes.Length - 1], Theme.BalloonPlum, 0.45f));
            }

            float basketTop = top + envelopeHeight + 6;
            canvas.Fill(centerX - 7, top + envelopeHeight - 2, 1, 8, Theme.WickerDark);
            canvas.Fill(centerX + 6, top + envelopeHeight - 2, 1, 8, Theme.WickerDark);

            // Charlotte and the bunny ride where the player can see them every frame — the tribute is in
            // the game, not only on the menu.
            canvas.Fill(centerX - 5, basketTop - 6, 6, 6, Theme.Skin);
            canvas.Fill(centerX - 5, basketTop - 7, 6, 2, Theme.HairBrown);
            canvas.Fill(centerX - 6, basketTop - 7, 1, 4, Theme.HairBrown);
            canvas.Fill(centerX - 4, basketTop - 4, 1, 1, Theme.Ink);
            canvas.Fill(centerX - 1, basketTop - 4, 1, 1, Theme.Ink);
            canvas.Fill(centerX - 6, basketTop - 8, 2, 2, Theme.HeartPink);
            canvas.Fill(centerX + 2, basketTop - 4, 4, 4, Theme.Cream);
            canvas.Fill(centerX + 3, basketTop - 6, 1, 2, Theme.Cream);
            canvas.Fill(centerX + 5, basketTop - 6, 1, 2, Theme.Cream);

            canvas.CutRectangle(centerX - 8, basketTop, 17, 11, 1, Theme.Ink);
            canvas.Fill(centerX - 7, basketTop + 1, 15, 9, Theme.WickerDark);
            for (int row = 0; row < 4; row++)
                canvas.Fill(centerX - 7, basketTop + 2 + row * 2, 15, 1, row % 2 == 0 ? Theme.WickerLight : Theme.WoodDark);
            canvas.Fill(centerX - 1, basketTop + 4, 2, 2, Theme.HeartPink);

            if (heating)
                Flame(canvas, centerX - 2, top + envelopeHeight + 1, tick);
        }

        /// <summary>Cloud puff built from cut rectangles; used by the parallax layers.</summary>
        public static void Cloud(DesignCanvas canvas, float x, float y, int width, Color highlight)
        {
            int h = Math.Max(8, width / 3);
            CutRectangle(canvas, x, y + h / 2, width, h / 2, Theme.CloudShade);
            CutRectangle(canvas, x + width / 8, y, width * 3 / 4, h, Theme.CloudLavender);
            CutRectangle(canvas, x + width / 5, y - 1, width / 2, 3, highlight);
        }

        /// <summary>Floating island: grass cap, flat top to land on, rock body tapering to a point.</summary>
        public static void Island(DesignCanvas canvas, float x, float y, int width, int height)
        {
            int rows = height - 6;
            canvas.Fill(x, y, width, 3, Theme.LeafGreen);
            canvas.Fill(x, y + 3, width, 2, Theme.MossGreen);
            canvas.Fill(x, y, width, 1, Theme.GrassLight);

            for (int row = 0; row < rows; row++)
            {
                int inset = row * width / (rows * 3);
                int w = Math.Max(2, width - inset * 2);
                canvas.Fill(x + inset, y + 5 + row, w, 1, Theme.RockSlate);
                canvas.Fill(x + inset, y + 5 + row, Math.Max(1, w / 5), 1, Theme.RockDark);
                canvas.Fill(x + inset + w - 1, y + 5 + row, 1, 1, Theme.RockDark);
            }
        }

        /// <summary>Wooden launch/delivery platform: posts, planks, and a lantern-ready flat top.</summary>
        public static void Platform(DesignCanvas canvas, Rectangle bounds)
        {
            canvas.Fill(bounds.X, bounds.Y, bounds.Width, 3, Theme.WoodLight);
            canvas.Fill(bounds.X, bounds.Y + 3, bounds.Width, 2, Theme.WoodMid);
            canvas.Fill(bounds.X, bounds.Y + 5, bounds.Width, 1, Theme.WoodDark);
            for (int post = 0; post < bounds.Width; post += 8)
            {
                canvas.Fill(bounds.X + post, bounds.Y + 6, 2, Math.Max(1, bounds.Height - 6), Theme.WoodDark);
                canvas.Fill(bounds.X + post, bounds.Y + 6, 1, Math.Max(1, bounds.Height - 6), Theme.WoodMid);
            }
        }

        /// <summary>Kit-wide cut rectangle, the silhouette language for every shape with thickness.</summary>
        public static void CutRectangle(DesignCanvas canvas, float x, float y, float w, float h, Color color, int cut = 2) =>
            canvas.CutRectangle(x, y, w, h, cut, color);
    }
}
