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
    public class BalloonaticGame : Game, UiHost
    {
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch = null!;
        DesignCanvas canvas = null!;
        readonly IScreen scaffoldScreen = new ScaffoldScreen();
        readonly IScreen styleScreen = new StyleScreen();
        readonly IScreen worldScreen;
        readonly IScreen menuScreen;
        readonly IScreen levelsScreen;
        readonly IScreen worldMapScreen;
        readonly IScreen settingsScreen;
        readonly IScreen creditsScreen;
        readonly IScreen pauseScreen;
        IScreen screen;
        double accumulatorSeconds;
        bool ballastWasPressed;
        bool restartWasPressed;

        /// <summary>
        /// Set once by the WebGL head: KNI's Blazor key pipeline dies after the first canvas click
        /// (see <see cref="WebKeys"/>), so that head answers keyboard questions from the bridge-fed
        /// table instead of the native state. The desktop head leaves this false and keeps its real
        /// keyboard; every key read in this file goes through <see cref="KeyIsDown"/>.
        /// </summary>
        public static bool UseWebKeyboard;

        // One-frame input edges: the UI thinks in presses, not holds (see UiFrame), so every device
        // snapshot is differenced against the last one and then remembered.
        bool enterWasPressed, escapeWasPressed, upWasPressed, downWasPressed, leftWasPressed, rightWasPressed;
        bool rWasPressed;

        // I11 audio edge cache: the director turns state into sound by watching for edges here,
        // renderer side — it reads state, never writes it, like every other decoration.
        int prevBounces, prevHearts, prevDrops;
        int prevPickupTick = -1;
        bool prevDelivered;
        bool mouseWasPressed;

        protected BalloonaticGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Harness = new TestHarness();
            Instance = this;

            // Screens take the game as their UiHost: they can ask for verbs (fly, restart, quit),
            // never for the simulation itself.
            worldScreen = new WorldScreen(this);
            menuScreen = new MenuScreen(this);
            levelsScreen = new LevelSelectScreen(this);
            worldMapScreen = new WorldMapScreen(this);
            settingsScreen = new SettingsScreen(this);
            creditsScreen = new CreditsScreen(this);
            pauseScreen = new PauseScreen((WorldScreen)worldScreen, this);

            // Live play opens on the start screen — the sim clock stays held until the player
            // asks to fly, so nothing can drift while the title is up. A test's `reset` returns
            // to the scaffold test bed. The screen field and the state must agree from the very
            // first frame, so both are set here.
            screen = menuScreen;
            Harness.State.Screen = SimState.ScreenMenu;

            // The simulation still fixed-steps itself (the accumulator below), so a tab that throttles
            // rAF or a test that steps manually cannot change its result. But the framework loop is
            // fixed too: presenting on a strict 60 Hz clock instead of free-running against vsync.
            // Free-running measured a steady 60 fps and still felt "out of sync" on the playtest —
            // its phase drifted against the display, and worst-case frames overran the 16.7 ms budget.
            IsFixedTimeStep = true;
            TargetElapsedTime = System.TimeSpan.FromSeconds(1.0 / 60.0);
            graphics.SynchronizeWithVerticalRetrace = true;
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
            SimState.ScreenMenu => menuScreen,
            SimState.ScreenLevels => levelsScreen,
            SimState.ScreenWorldMap => worldMapScreen,
            SimState.ScreenSettings => settingsScreen,
            SimState.ScreenCredits => creditsScreen,
            SimState.ScreenPause => pauseScreen,
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
            Sprites.Load(GraphicsDevice);   // the Aseprite sheet, from embedded bytes, once
        }

        protected override void UnloadContent()
        {
            canvas?.Dispose();
            base.UnloadContent();
        }

        // Pacing diagnostics, gated on BALLOON_DIAG=1: draws-per-second, sim steps per second and the
        // worst frame interval. "Feels laggy / out of sync" is a claim about timing, and timing gets
        // measured, not argued with. Zero cost when the variable is unset.
        readonly System.Diagnostics.Stopwatch diagClock = System.Diagnostics.Stopwatch.StartNew();
        double diagWindowStart;
        double diagWorstFrameMs;
        int diagPrevTick;

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();
            DiagPacing(gameTime);

            UiFrame frame = BuildUiFrame(keyboard, mouse);

            // The first deliberate touch — a press or a key — is the player saying they are here,
            // and the browser's definition of when audio may exist. Until then the audio stack
            // never loads, which also keeps a scripted test bed that only pokes the harness
            // (harness calls are not gestures) entirely out of the audio path.
            if (!Audio.Sound.Unlocked &&
                (frame.Click || frame.Enter || frame.Back || frame.Restart || frame.Up || frame.Down || frame.Left || frame.Right))
                Audio.Sound.Unlock();

            // Back is the dev head's kill switch on every screen. Escape belongs to whoever is
            // showing: pause the flight, step back out of a menu, quit from the title. On the test
            // beds it still quits, because those beds are why the key existed.
            if (KeyIsDown(keyboard, Keys.Back))
            {
                try { Exit(); }
                catch (System.PlatformNotSupportedException) { /* the browser head cannot close itself */ }
            }
            if (frame.Back && (screen == scaffoldScreen || screen == styleScreen))
            {
                try { Exit(); }
                catch (System.PlatformNotSupportedException) { }
            }

            if (screen is IUiScreen uiScreen)
                uiScreen.Tick(frame);

            if (!Harness.Paused && Harness.State.Screen == SimState.ScreenWorld)
            {
                // R is the "again" key, and its meaning follows the run: a delivered flight charts
                // the next course on the world map (I10 — the voyage between levels), anything else
                // replays this level's own sky on the same seed, so running out of hearts never skips
                // a course. Enter answers the same call the delivery panel just advertised. Edge-
                // triggered like the ballast key so holding cannot re-trigger every frame.
                bool restartPressed = KeyIsDown(keyboard, Keys.R);
                if (Harness.State.Complete && Harness.State.Delivered
                    && ((restartPressed && !restartWasPressed) || frame.Enter))
                    Harness.State.RequestedScreen = SimState.ScreenWorldMap;
                else if (restartPressed && !restartWasPressed)
                    Harness.RestartRun();
                restartWasPressed = restartPressed;

                // Heat is one verb: hold Space or hold the left mouse button. Sandbags drop on the
                // edge of B / right mouse, one bag per press. No left/right control exists by design.
                bool heat = KeyIsDown(keyboard, Keys.Space) || mouse.LeftButton == ButtonState.Pressed;
                bool ballastPressed = KeyIsDown(keyboard, Keys.B) || mouse.RightButton == ButtonState.Pressed;
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
                // The title and every menu — including pause — hold the clock, and nothing banks
                // catch-up while they hold it. Time runs on exactly one screen: the world.
                accumulatorSeconds = 0;
            }

            // The voyage record (I10): the moment a flight is home it is written on the chart paper,
            // from any head and whether paused or not — scripted deliveries count exactly like flown
            // ones. Rendering state only; the sim hash never sees it.
            if (Harness.State.Complete && Harness.State.Delivered)
                AppProgress.Mark(Harness.State.LevelId, Harness.State.Stars);

            ApplyScreenRequest();
            RunAudioDirector();

            // Every key has been read for this frame: release the fresh-press latches so a sub-frame
            // tap is exactly one frame of held, never a stuck key.
            if (UseWebKeyboard)
                WebKeys.PruneFresh();

            base.Update(gameTime);
        }

        /// <summary>
        /// One frame of UI intent: pointer in design coordinates, and only edges — a key is a press
        /// on the frame it goes down, never on the frames it is merely held. Screens cannot miss a
        /// tap between two updates, and cannot double-fire one.
        /// </summary>
        UiFrame BuildUiFrame(KeyboardState keyboard, MouseState mouse)
        {
            bool enter = KeyIsDown(keyboard, Keys.Enter);
            bool escape = KeyIsDown(keyboard, Keys.Escape);
            bool up = KeyIsDown(keyboard, Keys.Up) || KeyIsDown(keyboard, Keys.W);
            bool down = KeyIsDown(keyboard, Keys.Down) || KeyIsDown(keyboard, Keys.S);
            bool left = KeyIsDown(keyboard, Keys.Left) || KeyIsDown(keyboard, Keys.A);
            bool right = KeyIsDown(keyboard, Keys.Right) || KeyIsDown(keyboard, Keys.D);
            bool mouseDown = mouse.LeftButton == ButtonState.Pressed;
            bool r = KeyIsDown(keyboard, Keys.R);   // the "again/on" key, also offered to menus (world map)

            // Before the first Present the frame is unknown; a pointer outside every rectangle is the
            // honest answer, so no menu can hover on an invisible letterbox.
            Vector2 design = canvas != null ? canvas.ToDesign(mouse.X, mouse.Y) : new Vector2(-1f, -1f);

            UiFrame frame = new UiFrame
            {
                PointerX = (int)design.X,
                PointerY = (int)design.Y,
                PointerInside = design.X >= 0f && design.X < Sim.DesignWidth && design.Y >= 0f && design.Y < Sim.DesignHeight,
                Click = mouseDown && !mouseWasPressed,
                Enter = enter && !enterWasPressed,
                Restart = r && !rWasPressed,
                Back = escape && !escapeWasPressed,
                Up = up && !upWasPressed,
                Down = down && !downWasPressed,
                Left = left && !leftWasPressed,
                Right = right && !rightWasPressed,
            };

            enterWasPressed = enter;
            escapeWasPressed = escape;
            upWasPressed = up;
            downWasPressed = down;
            leftWasPressed = left;
            rightWasPressed = right;
            mouseWasPressed = mouseDown;
            rWasPressed = r;
            return frame;
        }

        /// <summary>
        /// The one keyboard read: native state on the desktop head; on the WebGL head the bridge-fed
        /// <see cref="WebKeys"/>, where a press that landed between two polls still counts as held
        /// for the Update that prunes it (the fresh-press latch).
        /// </summary>
        static bool KeyIsDown(KeyboardState native, Keys key) =>
            UseWebKeyboard ? WebKeys.IsDown(key) || WebKeys.FreshDown(key) : native.IsKeyDown(key);

        // ---- UiHost: everything a screen may ask for, and the one sync a settings change owes ----

        void UiHost.GoTo(string target) => Harness.State.RequestedScreen = target;

        void UiHost.FlyLevel(string levelId)
        {
            Harness.SelectLevel(levelId);
            Harness.State.RequestedScreen = SimState.ScreenWorld;
        }

        void UiHost.FlyNextCourse()
        {
            Harness.AdvanceRun();
            Harness.State.RequestedScreen = SimState.ScreenWorld;
        }

        void UiHost.RestartFlight()
        {
            Harness.RestartRun();
            Harness.State.RequestedScreen = SimState.ScreenWorld;
        }

        void UiHost.Quit()
        {
            try { Exit(); }
            catch (System.PlatformNotSupportedException) { /* the browser head cannot close itself */ }
        }

        /// <summary>
        /// Wind Assist is felt by the flight in the air, not just the next one: the running state takes
        /// the setting the moment it changes. The volumes reach nothing until I11 ships the sounds.
        /// </summary>
        void UiHost.LiveSettingsChanged()
        {
            Harness.State.WindAssist = AppSettings.WindAssist;
            Audio.Sound.ApplyVolumes();   // the sliders are live the frame they move, music and kit alike
        }

        static readonly bool DiagEnabled = System.Environment.GetEnvironmentVariable("BALLOON_DIAG") == "1";

        void DiagPacing(GameTime gameTime)
        {
            if (!DiagEnabled)
                return;

            double now = diagClock.Elapsed.TotalSeconds;
            double frameMs = gameTime.ElapsedGameTime.TotalMilliseconds;
            if (frameMs > diagWorstFrameMs)
                diagWorstFrameMs = frameMs;

            if (now - diagWindowStart < 1.0)
                return;

            System.Console.WriteLine(
                "[diag] draws/s=" + (FrameCount - LastDiagFrame) +
                " simSteps/s=" + (Harness.State.Tick - diagPrevTick) +
                " worstFrameMs=" + diagWorstFrameMs.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            diagWindowStart = now;
            diagWorstFrameMs = 0;
            diagPrevTick = Harness.State.Tick;
            LastDiagFrame = FrameCount;
        }

        int LastDiagFrame;

        /// <summary>Renderer-side audio readout for the bridge (<c>__GAME.audio()</c>).</summary>
        public string AudioStatus() => Audio.Sound.Status();
        public string AudioMixStatus() => Audio.Sound.MixStatus();

        /// <summary>
        /// Sound from state, once per Update, watching only for edges (I11). The music holds behind
        /// the pause veil and on the test beds; the burner is the one sound that is a state, not an
        /// event — it breathes while the sim says heat is on, and only on the world screen.
        /// Everything funnels through <see cref="Audio.Sound"/>, whose mute-first rules mean this
        /// method does honest work for free when audio is off, locked, or broken.
        /// </summary>
        void RunAudioDirector()
        {
            SimState s = Harness.State;
            bool onWorld = s.Screen == SimState.ScreenWorld;
            Audio.Sound.SetMusic(s.Screen is not (SimState.ScreenPause or SimState.ScreenScaffold or SimState.ScreenStyle));
            Audio.Sound.SetBurner(onWorld && s.Heating && !s.Complete);

            if (s.PickupTick >= 0 && prevPickupTick < 0 && !s.Delivered) Audio.Sound.Play("chime");
            if (s.Bounces > prevBounces) Audio.Sound.Play("creak");
            if (s.Hearts < prevHearts) Audio.Sound.Play("giggle");         // she laughs it off — losing is not a scold
            if (s.DropsSeen > prevDrops) Audio.Sound.Play("thump");
            if (s.Delivered && !prevDelivered)
            {
                Audio.Sound.Play("jingle");
                Audio.Sound.PlaySoon("chime", 42);                          // the stars ring in one after another
                Audio.Sound.PlaySoon("chime", 74);
                Audio.Sound.PlaySoon("chime", 106);
            }

            prevBounces = s.Bounces;
            prevHearts = s.Hearts;
            prevDrops = s.DropsSeen;
            prevDelivered = s.Delivered;
            prevPickupTick = s.PickupTick;
            Audio.Sound.Tick();
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
            DrawScreenFade(canvas);
            canvas.Present();
            FrameCount++;
            base.Draw(gameTime);
        }

        string fadedScreen = "";
        int fadeStartFrame;

        /// <summary>
        /// The ink wash between screens (I12): whenever a different screen is about to be presented,
        /// the frame opens under a dark veil that lifts over ~100 ms. Like hover and focus it is UI
        /// transition state — wall-clock, outside the sim — and under Reduced Motion the cut stays a
        /// cut. Screenshots settle past the wash before they shoot, so baselines never race it.
        /// </summary>
        void DrawScreenFade(DesignCanvas canvas)
        {
            if (screen.Id != fadedScreen)
            {
                fadedScreen = screen.Id;
                fadeStartFrame = FrameCount;
            }
            int age = FrameCount - fadeStartFrame;
            if (age < 6 && !AppSettings.ReducedMotion)
                canvas.Fill(0, 0, Sim.DesignWidth, Sim.DesignHeight,
                    new Color(11, 17, 41, 140 * (6 - age) / 6));
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
