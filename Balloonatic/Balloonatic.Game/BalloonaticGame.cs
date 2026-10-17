using Balloonatic.Core;
using Balloonatic.Rendering;
using Balloonatic.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Balloonatic
{
    /// <summary>
    /// Shared game for every head. Owns three things only: the fixed-step simulation clock, the
    /// design-resolution canvas, and which screen is showing. Heads stay thin (see Platform*Game.cs).
    ///
    /// Time is never taken from wall clock by the simulation: <see cref="Update"/> accumulates real
    /// elapsed time into 60 Hz steps and hands them to <see cref="Harness"/>, and while a test owns
    /// time (<c>paused</c>) the accumulator is discarded instead of banking catch-up work. Rendering
    /// reads only <see cref="TestHarness.State"/>, so effects on or off cannot move the simulation and
    /// a stepped state always draws the same frame.
    /// </summary>
    public class BalloonaticGame : Game
    {
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch = null!;
        DesignCanvas canvas = null!;
        readonly IScreen scaffoldScreen = new ScaffoldScreen();
        readonly IScreen styleScreen = new StyleScreen();
        readonly IScreen worldScreen = new WorldScreen();
        IScreen screen;
        double accumulatorSeconds;
        bool ballastWasPressed;
        bool restartWasPressed;

        protected BalloonaticGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Harness = new TestHarness();
            Instance = this;

            // Live play opens on the world; a test's `reset` returns to the scaffold test bed. The
            // screen field and the state must agree from the very first frame, so both are set here.
            screen = worldScreen;
            Harness.State.Screen = SimState.ScreenWorld;

            // Fixed stepping is done here, not by the framework, so a browser tab that throttles
            // rAF or a test that steps manually cannot change the simulation's result.
            IsFixedTimeStep = false;
            InactiveSleepTime = System.TimeSpan.Zero;
        }

        /// <summary>The head currently running; the WebGL head reaches the harness through this.</summary>
        public static BalloonaticGame? Instance { get; private set; }

        public TestHarness Harness { get; }

        /// <summary>Frames presented. Renderer-side only, used to measure frame rate from a test.</summary>
        public int FrameCount { get; private set; }

        /// <summary>True once a frame has actually reached the screen, which is what "ready" means.</summary>
        public bool FirstFrameDrawn => FrameCount > 0;

        /// <summary>
        /// The screens that exist. I7 turns this into the menu flow; for now it is how the harness can ask
        /// for the style sheet and how an unknown screen falls back loudly instead of showing a blank frame.
        /// </summary>
        IScreen Resolve(string? id) => id switch
        {
            SimState.ScreenStyle => styleScreen,
            SimState.ScreenScaffold => scaffoldScreen,
            SimState.ScreenWorld => worldScreen,
            _ => scaffoldScreen,
        };

        /// <summary>Heads that own a window call this to pick a sane initial size.</summary>
        protected void ConfigureWindow(string title, int width, int height)
        {
            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
            Window.Title = title;
        }

        /// <summary>
        /// Keeps the drawing surface matched to the pixels the head actually has — a resized desktop
        /// window, or a browser canvas that is whatever the LD page iframe happens to be. Letterboxing
        /// is then a render-time decision, not a windowing one.
        /// </summary>
        public void SetSurfaceSize(int width, int height)
        {
            if (width <= 0 || height <= 0)
                return;
            if (graphics.PreferredBackBufferWidth == width && graphics.PreferredBackBufferHeight == height)
                return;

            graphics.PreferredBackBufferWidth = width;
            graphics.PreferredBackBufferHeight = height;
            try
            {
                graphics.ApplyChanges();
            }
            catch (System.Exception)
            {
                // A head mid-transition can refuse a reset; the next frame retries.
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            canvas = new DesignCanvas(this, GraphicsDevice, spriteBatch);
        }

        protected override void UnloadContent()
        {
            canvas?.Dispose();
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            if (keyboard.IsKeyDown(Keys.Escape) || keyboard.IsKeyDown(Keys.Back))
            {
                try { Exit(); }
                catch (System.PlatformNotSupportedException) { /* the browser head cannot close itself */ }
            }

            if (!Harness.Paused)
            {
                // R is the "again" key, and its meaning follows the run: a delivered flight advances to
                // the next course (the last one loops back to the first), anything else replays this
                // level's own sky on the same seed. Edge-triggered like the ballast key so holding it
                // cannot re-trigger every frame.
                bool restartPressed = keyboard.IsKeyDown(Keys.R);
                if (restartPressed && !restartWasPressed)
                {
                    if (Harness.State.Complete)
                        Harness.AdvanceRun();
                    else
                        Harness.RestartRun();
                }
                restartWasPressed = restartPressed;

                // Heat is one verb: hold Space or hold the left mouse button. Sandbags drop on the
                // edge of B / right mouse, one bag per press. No left/right control exists by design.
                bool heat = keyboard.IsKeyDown(Keys.Space) || mouse.LeftButton == ButtonState.Pressed;
                bool ballastPressed = keyboard.IsKeyDown(Keys.B) || mouse.RightButton == ButtonState.Pressed;
                Harness.SetLiveInput(heat, ballastPressed && !ballastWasPressed);
                ballastWasPressed = ballastPressed;

                double step = Sim.StepSeconds;
                accumulatorSeconds += gameTime.ElapsedGameTime.TotalSeconds;
                // A throttled or backgrounded tab must not be able to bank a burst of catch-up steps.
                if (accumulatorSeconds > 0.25)
                    accumulatorSeconds = 0.25;
                while (accumulatorSeconds >= step)
                {
                    Harness.AutoStep();
                    accumulatorSeconds -= step;
                }
            }
            else
            {
                accumulatorSeconds = 0;
            }

            ApplyScreenRequest();

            base.Update(gameTime);
        }

        /// <summary>
        /// Screen changes are applied by the game, not by the caller: the harness can ask for any string it
        /// likes, and this is where an unknown one is resolved to a screen that exists. It runs while paused
        /// too, so a test can put the game on a screen and then step it.
        /// </summary>
        void ApplyScreenRequest()
        {
            string? requested = Harness.State.RequestedScreen;
            if (requested == null)
                return;

            Harness.State.RequestedScreen = null;
            screen = Resolve(requested);
            Harness.State.Screen = screen.Id;
        }

        protected override void Draw(GameTime gameTime)
        {
            canvas.BeginDesign();
            screen.Draw(canvas, Harness.State);
            canvas.Present();
            FrameCount++;
            base.Draw(gameTime);
        }

        protected override void OnExiting(System.EventArgs args)
        {
            Instance = null;
            base.OnExiting(args);
        }

        /// <summary>
        /// Renderer geometry as the head actually sees it, for the harness and for diagnosing scaling
        /// bugs on a platform we cannot inspect with a debugger (WebGL).
        /// </summary>
        public string RenderMetrics()
        {
            PresentationParameters pp = GraphicsDevice.PresentationParameters;
            Viewport viewport = GraphicsDevice.Viewport;
            return "frames=" + FrameCount
                + ";viewport=" + viewport.Width + "x" + viewport.Height
                + ";backbuffer=" + pp.BackBufferWidth + "x" + pp.BackBufferHeight
                + ";client=" + Window.ClientBounds.Width + "x" + Window.ClientBounds.Height
                + ";scale=" + canvas.Scale.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
