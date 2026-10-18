using System;
using Balloonatic.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Balloonatic.Rendering
{
    /// <summary>
    /// Renders the game at the fixed 640x360 design resolution and presents it scaled into whatever
    /// the window or canvas actually is: fit the available area, preserving aspect ratio and using
    /// nearest-neighbour sampling. Letterboxing is only needed when the aspect ratios differ.
    /// One code path for the desktop head and the WebGL head, so what I see in a browser is what the
    /// entry shows.
    ///
    /// The target behind the design grid is <see cref="Px"/> times larger (1280x720): world drawing
    /// takes float design coordinates and snaps them to whole pixels of that finer grid, so a camera
    /// moving at a gentle 15 px/s advances in half-design-pixel steps instead of whole ones. That is
    /// the cure for the slow-motion strobe the feel playtest caught — motion smoothness without ever
    /// sampling art between pixels, so everything stays hard-edged pixel art. HUD text stays on the
    /// coarse integer grid: type must not wobble.
    /// </summary>
    sealed class DesignCanvas : IDisposable
    {
        /// <summary>Design-pixel grid everything snaps to, and the gutter every panel and HUD block uses.</summary>
        public const int Grid = 8;

        /// <summary>Internal pixels per design pixel. Positions quantise to 1/Px of a design pixel.</summary>
        public const float Px = 2f;

        readonly Game _game;
        readonly GraphicsDevice _device;
        readonly SpriteBatch _spriteBatch;
        readonly Texture2D _pixel;
        RenderTarget2D? _target;
        float _scale = 1f;
        int _offsetX, _offsetY;

        public DesignCanvas(Game game, GraphicsDevice device, SpriteBatch spriteBatch)
        {
            _game = game;
            _device = device;
            _spriteBatch = spriteBatch;
            _pixel = new Texture2D(device, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public int Width => Sim.DesignWidth;
        public int Height => Sim.DesignHeight;

        /// <summary>The scale last used to present. Tests assert the letterbox math through it.</summary>
        public float Scale => _scale;

        /// <summary>
        /// Pointer pixels (surface origin) to design coordinates — the inverse of the last Present, so
        /// menus hit-test the exact rectangles they drew at any letterbox offset on any head.
        /// </summary>
        public Vector2 ToDesign(int surfaceX, int surfaceY) =>
            new Vector2((surfaceX - _offsetX) / _scale, (surfaceY - _offsetY) / _scale);

        public Vector2 Center => new Vector2(Width / 2f, Height / 2f);

        /// <summary>Begin drawing in design pixels.</summary>
        public void BeginDesign()
        {
            int rtWidth = (int)(Width * Px);
            int rtHeight = (int)(Height * Px);
            if (_target == null || _target.Width != rtWidth || _target.Height != rtHeight)
            {
                _target?.Dispose();
                _target = new RenderTarget2D(_device, rtWidth, rtHeight, false, SurfaceFormat.Color, DepthFormat.None);
            }

            _device.SetRenderTarget(_target);
            _device.Clear(Theme.LetterBox);
            // Design coordinates in, internal pixels on the target: the scale transform is the whole
            // of the 2x arrangement, so every existing integer call site lands where it always did.
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null,
                              null, Matrix.CreateScale(Px, Px, 1f));
        }

        /// <summary>
        /// Snap to the internal pixel grid (multiples of 1/Px design px): the smallest motion that can
        /// be drawn without anti-aliasing, and identical on desktop and WASM because it is plain float
        /// rounding of numbers the simulation already fixed.
        /// </summary>
        static float Snap(float v) => MathF.Round(v * Px) / Px;

        /// <summary>
        /// A filled rectangle in design coordinates. Float in, whole internal pixels out — world art
        /// moves at half-pixel granularity through this call while the HUD, which passes integers,
        /// is pixel-identical to the coarse grid it was authored on.
        /// </summary>
        public void Fill(float x, float y, float w, float h, Color color)
        {
            x = Snap(x);
            y = Snap(y);
            w = Snap(w);
            h = Snap(h);
            // Vector2 position + scale rather than Rectangle: Rectangle would re-quantise to whole
            // design pixels and throw the half-pixel motion away again.
            _spriteBatch.Draw(_pixel, new Vector2(x, y), null, color, 0f, Vector2.Zero, new Vector2(w, h),
                              SpriteEffects.None, 0f);
        }

        public void Fill(Rectangle bounds, Color color) => Fill(bounds.X, bounds.Y, bounds.Width, bounds.Height, color);

        /// <summary>
        /// Draws a sprite whose pixels are internal resolution (1 sheet px = 1/Px design px): the
        /// source rectangle lands 1:1 on the 2x target at any snapped design position, so sprites
        /// ride the half-pixel camera without ever sampling between their own pixels.
        /// </summary>
        public void DrawSprite(Texture2D texture, Rectangle source, float x, float y, bool flip, Color? tint = null, int scale = 1)
        {
            x = Snap(x);
            y = Snap(y);
            _spriteBatch.Draw(texture, new Vector2(x, y), source, tint ?? Color.White, 0f, Vector2.Zero,
                              new Vector2(scale / Px), flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
        }

        /// <summary>One design-pixel border, and the frame that proves the letterbox is honest.</summary>
        public void Outline(Rectangle bounds, Color color)
        {
            Fill(bounds.X, bounds.Y, bounds.Width, 1, color);
            Fill(bounds.X, bounds.Bottom - 1, bounds.Width, 1, color);
            Fill(bounds.X, bounds.Y, 1, bounds.Height, color);
            Fill(bounds.Right - 1, bounds.Y, 1, bounds.Height, color);
        }

        public void Outline(Rectangle bounds, int thickness, Color color)
        {
            Fill(bounds.X, bounds.Y, bounds.Width, thickness, color);
            Fill(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness, color);
            Fill(bounds.X, bounds.Y, thickness, bounds.Height, color);
            Fill(bounds.Right - thickness, bounds.Y, thickness, bounds.Height, color);
        }

        /// <summary>Float form for world-space art; the coarse Rectangle overloads stay for the HUD.</summary>
        public void Outline(float x, float y, float w, float h, int thickness, Color color)
        {
            Fill(x, y, w, thickness, color);
            Fill(x, y + h - thickness, w, thickness, color);
            Fill(x, y, thickness, h, color);
            Fill(x + w - thickness, y, thickness, h, color);
        }

        /// <summary>
        /// A rectangle with its corners cut — pixel-art rounding. Everything with a shape in this game
        /// (panels, meters, tags, balloons) uses it, so the whole UI shares one silhouette language.
        /// </summary>
        public void CutRectangle(int x, int y, int w, int h, int cut, Color color) =>
            CutRectangle((float)x, y, w, h, cut, color);

        /// <summary>Float form for world-space art — the silhouette language at half-pixel positions.</summary>
        public void CutRectangle(float x, float y, float w, float h, int cut, Color color)
        {
            if (cut < 1)
            {
                Fill(x, y, w, h, color);
                return;
            }

            Fill(x, y + cut, w, h - cut * 2, color);
            Fill(x + cut, y, w - cut * 2, cut, color);
            Fill(x + cut, y + h - cut, w - cut * 2, cut, color);
        }

        /// <summary>Finish design drawing and blit it scaled and centered onto the real backbuffer.</summary>
        public void Present()
        {
            _spriteBatch.End();
            if (_target == null)
                return;

            // The drawing buffer is the truth about how many pixels exist: on the WebGL head the viewport
            // can stay smaller than the canvas unless the head is told the canvas size.
            PresentationParameters pp = _device.PresentationParameters;
            int surfaceWidth = pp.BackBufferWidth > 0 ? pp.BackBufferWidth : _device.Viewport.Width;
            int surfaceHeight = pp.BackBufferHeight > 0 ? pp.BackBufferHeight : _device.Viewport.Height;

            float scale = Math.Min(surfaceWidth / (float)Width, surfaceHeight / (float)Height);
            _scale = scale;

            int drawW = (int)Math.Round(Width * scale);
            int drawH = (int)Math.Round(Height * scale);
            var destination = new Rectangle((surfaceWidth - drawW) / 2, (surfaceHeight - drawH) / 2, drawW, drawH);
            _offsetX = destination.X;
            _offsetY = destination.Y;

            _device.SetRenderTarget(null);
            _device.Clear(Theme.LetterBox);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
            _spriteBatch.Draw(_target, destination, Color.White);
            _spriteBatch.End();
        }

        public void Dispose() => _target?.Dispose();
    }
}
