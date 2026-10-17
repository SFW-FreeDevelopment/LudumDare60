using System;
using Balloonatic.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Balloonatic.Rendering
{
    /// <summary>
    /// Renders the game at the fixed 640x360 design resolution and presents it scaled into whatever
    /// the window or canvas actually is: integer scaling while it still fits, fractional only when the
    /// viewport is smaller (a phone in the LD page), always nearest-neighbour, always letterboxed.
    /// One code path for the desktop head and the WebGL head, so what I see in a browser is what the
    /// entry shows.
    /// </summary>
    sealed class DesignCanvas : IDisposable
    {
        readonly Game _game;
        readonly GraphicsDevice _device;
        readonly SpriteBatch _spriteBatch;
        readonly Texture2D _pixel;
        RenderTarget2D? _target;

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

        /// <summary>Last scale applied when presenting; reported by the harness so scaling bugs are visible.</summary>
        public float Scale { get; private set; } = 1f;

        public Vector2 Center => new Vector2(Width / 2f, Height / 2f);

        /// <summary>Begin drawing in design pixels.</summary>
        public void BeginDesign()
        {
            if (_target == null || _target.Width != Width || _target.Height != Height)
            {
                _target?.Dispose();
                _target = new RenderTarget2D(_device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None);
            }

            _device.SetRenderTarget(_target);
            _device.Clear(Palette.BandMid);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
        }

        public void Fill(int x, int y, int w, int h, Color color) =>
            _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);

        public void Fill(Rectangle bounds, Color color) => Fill(bounds.X, bounds.Y, bounds.Width, bounds.Height, color);

        /// <summary>One design-pixel line, for borders and the frame that shows the letterbox is honest.</summary>
        public void Outline(Rectangle bounds, Color color)
        {
            Fill(bounds.X, bounds.Y, bounds.Width, 1, color);
            Fill(bounds.X, bounds.Bottom - 1, bounds.Width, 1, color);
            Fill(bounds.X, bounds.Y, 1, bounds.Height, color);
            Fill(bounds.Right - 1, bounds.Y, 1, bounds.Height, color);
        }

        /// <summary>Finish design drawing and blit it scaled and centered onto the real backbuffer.</summary>
        public void Present()
        {
            _spriteBatch.End();
            if (_target == null)
                return;

            // The drawing-buffer size is the truth about how many pixels exist; on the WebGL head the
            // viewport can be narrower than the canvas if the head never told the device otherwise.
            PresentationParameters pp = _device.PresentationParameters;
            int surfaceWidth = pp.BackBufferWidth > 0 ? pp.BackBufferWidth : _device.Viewport.Width;
            int surfaceHeight = pp.BackBufferHeight > 0 ? pp.BackBufferHeight : _device.Viewport.Height;

            float scale = Math.Min(surfaceWidth / (float)Width, surfaceHeight / (float)Height);
            if (scale >= 1f)
                scale = (float)Math.Floor(scale);
            Scale = scale;

            int drawW = (int)Math.Round(Width * scale);
            int drawH = (int)Math.Round(Height * scale);
            var destination = new Rectangle((surfaceWidth - drawW) / 2, (surfaceHeight - drawH) / 2, drawW, drawH);

            _device.SetRenderTarget(null);
            _device.Clear(Palette.Letterbox);
            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
            _spriteBatch.Draw(_target, destination, Color.White);
            _spriteBatch.End();
        }

        public void Dispose() => _target?.Dispose();
    }
}
