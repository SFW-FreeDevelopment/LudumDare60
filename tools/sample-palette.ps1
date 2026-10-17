<#
.SYNOPSIS
  Samples docs/reference/concept-sheet.png into the palette Theme.cs is allowed to use.
.DESCRIPTION
  Quantizes the sheet and prints (1) the most frequent, perceptually distinct colors and (2) an 8x8
  spatial mosaic of dominant colors, so palette roles (sky, cloud, wicker, balloon fabric, night) can
  be read from where a color appears rather than guessed. The sampling itself runs in compiled C#:
  175k GetPixel calls in PowerShell script code takes minutes and gets its arithmetic types wrong.
  Output is committed to docs/reference/palette.txt, so every color in Theme.cs traces back to real
  pixels in the art (AGENTS.md: styling comes from docs/STYLE.md, and STYLE.md comes from the sheet).
.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File tools/sample-palette.ps1 > docs/reference/palette.txt
#>
param(
    [string]$Sheet = "$PSScriptRoot\..\docs\reference\concept-sheet.png",
    [int]$Step = 3,
    [int]$Top = 24
)

$source = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

public static class PaletteSampler
{
    class Bucket
    {
        public int Count;
        public long R, G, B;
    }

    public static string Run(string path, int step, int top)
    {
        Bitmap bitmap = new Bitmap(path);
        int width = bitmap.Width;
        int height = bitmap.Height;

        Rectangle rect = new Rectangle(0, 0, width, height);
        BitmapData data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int[] pixels = new int[width * height];
        Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        bitmap.UnlockBits(data);

        Dictionary<int, Bucket> global = new Dictionary<int, Bucket>();
        Dictionary<long, Bucket>[] tiles = new Dictionary<long, Bucket>[64];
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = new Dictionary<long, Bucket>();

        for (int y = 0; y < height; y += step)
        {
            int tileY = y * 8 / height;
            if (tileY > 7) tileY = 7;
            for (int x = 0; x < width; x += step)
            {
                int argb = pixels[y * width + x];
                int r = (argb >> 16) & 0xFF;
                int g = (argb >> 8) & 0xFF;
                int b = argb & 0xFF;
                int bucketKey = ((r >> 4) << 8) | ((g >> 4) << 4) | (b >> 4);

                Bucket bucket;
                if (!global.TryGetValue(bucketKey, out bucket))
                {
                    bucket = new Bucket();
                    global[bucketKey] = bucket;
                }
                bucket.Count++;
                bucket.R += r;
                bucket.G += g;
                bucket.B += b;

                int tileX = x * 8 / width;
                if (tileX > 7) tileX = 7;
                long tileKey = tileY * 8 + tileX;
                Dictionary<long, Bucket> tile = tiles[tileY * 8 + tileX];
                Bucket tileBucket;
                if (!tile.TryGetValue(tileKey * 100000 + bucketKey, out tileBucket))
                {
                    // store the bucket key in the value so the tile average can reuse the global sums
                    tileBucket = bucket;
                    tile[tileKey * 100000 + bucketKey] = tileBucket;
                }
            }
        }

        List<Bucket> ranked = new List<Bucket>(global.Values);
        ranked.Sort(delegate(Bucket a, Bucket b) { return b.Count.CompareTo(a.Count); });

        List<Color> distinct = new List<Color>();
        List<int> counts = new List<int>();
        foreach (Bucket bucket in ranked)
        {
            Color color = Color.FromArgb((int)(bucket.R / bucket.Count), (int)(bucket.G / bucket.Count), (int)(bucket.B / bucket.Count));
            if (TooClose(distinct, color, 42))
                continue;
            distinct.Add(color);
            counts.Add(bucket.Count);
            if (distinct.Count >= top)
                break;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("sheet: " + path.Substring(path.LastIndexOf('/') + 1 < 0 ? 0 : path.LastIndexOfAny(new char[] { '/', '\\' }) + 1) +
                      "  size: " + width + " x " + height + "  sample step: " + step + " px");
        sb.AppendLine();
        sb.AppendLine("=== most frequent distinct colors (" + global.Count + " distinct 12-bit buckets) ===");
        for (int i = 0; i < distinct.Count; i++)
            sb.AppendLine(string.Format("#{0:X2}{1:X2}{2:X2}   rgb({3,3},{4,3},{5,3})   samples={6}",
                distinct[i].R, distinct[i].G, distinct[i].B, distinct[i].R, distinct[i].G, distinct[i].B, counts[i]));

        sb.AppendLine();
        sb.AppendLine("=== 8x8 mosaic: most frequent color per tile (row 0 = top of sheet) ===");
        for (int ty = 0; ty < 8; ty++)
        {
            sb.Append("row " + ty + ": ");
            for (int tx = 0; tx < 8; tx++)
            {
                Dictionary<long, Bucket> tile = tiles[ty * 8 + tx];
                Bucket best = null;
                long bestKey = 0;
                foreach (KeyValuePair<long, Bucket> pair in tile)
                {
                    if (best == null || pair.Value.Count > best.Count)
                    {
                        best = pair.Value;
                        bestKey = pair.Key;
                    }
                }
                if (best == null)
                {
                    sb.Append("-------- ");
                    continue;
                }
                int bucketKey = (int)(bestKey % 100000);
                Bucket globalBucket = global[bucketKey];
                sb.Append(string.Format("#{0:X2}{1:X2}{2:X2} ",
                    (int)(globalBucket.R / globalBucket.Count),
                    (int)(globalBucket.G / globalBucket.Count),
                    (int)(globalBucket.B / globalBucket.Count)));
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    static bool TooClose(List<Color> picked, Color color, double minDistance)
    {
        foreach (Color other in picked)
        {
            int dr = other.R - color.R;
            int dg = other.G - color.G;
            int db = other.B - color.B;
            if (Math.Sqrt(dr * dr + dg * dg + db * db) < minDistance)
                return true;
        }
        return false;
    }
}
'@

Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing
Add-Type -AssemblyName System.Drawing

[PaletteSampler]::Run((Resolve-Path $Sheet).Path, $Step, $Top)
