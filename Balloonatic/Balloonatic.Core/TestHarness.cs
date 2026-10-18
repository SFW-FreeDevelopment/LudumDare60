using System;
using System.Globalization;
using System.Text;

namespace Balloonatic.Core
{
    /// <summary>
    /// The single contract between the game and anything watching from outside: Playwright through
    /// <c>window.__GAME</c>, and the xunit suite directly. One string in, one string out, no
    /// serialization types, so the exact same code path is exercised by both kinds of test.
    ///
    /// Protocol (all replies are strings; errors are <c>error:…</c>):
    ///   ready | version | screen | seed | tick | hash | state
    ///   reset [seedHex]      -> ok:&lt;hash&gt;   also clears input and pauses, so a scripted replay
    ///                            cannot race the render loop; "resume" hands time back to the game
    ///   restart              -> ok:&lt;hash&gt;   player-style restart: same seed and screen, unpaused
    ///   advance              -> ok:&lt;hash&gt;   next authored level (wraps after the last), fresh run
    ///                            on a derived seed, unpaused — the R key after a delivery
    ///   set heat=0|1         -> ok:heat=…   (also: ballast=1 queues one sandbag drop)
    ///   step [n]             -> &lt;hash&gt;     (advances n steps, 1..3600, ignoring the paused flag)
    ///   pause | resume       -> ok:paused=0|1
    ///   deliver [sec]        -> ok:delivered | error:not-delivered  (autopilot lands on the deck;
    ///                            steps directly, so it works paused and is deterministic either way)
    ///
    /// A step is only ever taken here: the game loop calls <see cref="AutoStep"/> and the bridge calls
    /// <see cref="Handle"/> for "step". Nothing else can advance time, which is what makes replays and
    /// screenshots reproducible.
    /// </summary>
    public sealed class TestHarness
    {
        public const ulong DefaultSeed = 0xC0FFEE1234UL;
        public const int MaxStepsPerCall = 3600;

        SimState _state;
        SimInput _input;
        bool _pendingBallast;
        ulong _runSeed = DefaultSeed;

        public TestHarness(ulong seed = DefaultSeed)
        {
            _state = CreateState(seed);
            _runSeed = _state.Seed;
        }

        public SimState State => _state;
        public bool Paused { get; private set; }

        static SimState CreateState(ulong seed) => new SimState
        {
            Seed = seed == 0 ? DefaultSeed : seed,
            WindAssist = AppSettings.WindAssist,   // a new run inherits the assist switch
        };

        /// <summary>Advance one step if the game loop owns time (i.e. tests are not driving it).</summary>
        public void AutoStep()
        {
            if (Paused)
                return;
            StepOnce();
        }

        /// <summary>
        /// Live player input from a head's device poll. Ignored while paused so a scripted Playwright
        /// run and a stray keyboard key can never fight over the same step.
        /// </summary>
        public void SetLiveInput(bool heat, bool queueBallastDrop = false)
        {
            if (Paused)
                return;
            _input.Heat = heat;
            if (queueBallastDrop)
                _pendingBallast = true;
        }

        void StepOnce()
        {
            SimInput input = _input;
            if (_pendingBallast)
            {
                input.DropBallast = true;
                _pendingBallast = false;
            }
            SimRules.Step(_state, input);
        }

        public string Handle(string? call, string? arg)
        {
            string name = (call ?? string.Empty).Trim().ToLowerInvariant();
            string a = (arg ?? string.Empty).Trim();

            switch (name)
            {
                case "ready":
                    return "1";
                case "version":
                    return Sim.ProtocolVersion;
                case "screen":
                    return a.Length == 0 ? _state.Screen : RequestScreen(a);
                case "seed":
                    return _state.Seed.ToString("x16", CultureInfo.InvariantCulture);
                case "tick":
                    return _state.Tick.ToString(CultureInfo.InvariantCulture);
                case "temperature":
                    return _state.Temperature.ToString("0.0000", CultureInfo.InvariantCulture);
                case "altitude":
                    return _state.Altitude.ToString("0.00", CultureInfo.InvariantCulture);
                case "velocity":
                    return _state.VelocityY.ToString("0.00", CultureInfo.InvariantCulture);
                case "x":
                    return _state.X.ToString("0.00", CultureInfo.InvariantCulture);
                case "velocityx":
                    return _state.VelocityX.ToString("0.00", CultureInfo.InvariantCulture);
                case "ballast":
                    return _state.BallastHeld.ToString(CultureInfo.InvariantCulture);
                case "drops":
                    return _state.DropsSeen.ToString(CultureInfo.InvariantCulture);
                case "level":
                    return a.Length == 0 ? _state.LevelId : SelectLevel(a);
                case "delivered":
                    return _state.Delivered ? "1" : "0";
                case "complete":
                    return _state.Complete ? "1" : "0";
                case "stars":
                    return _state.Stars.ToString(CultureInfo.InvariantCulture);
                case "bounces":
                    return _state.Bounces.ToString(CultureInfo.InvariantCulture);
                case "hearts":
                    return _state.Hearts.ToString(CultureInfo.InvariantCulture);
                case "hazards":
                    return DescribeHazards();

                case "warp":
                {
                    // Script a collision: park the balloon at a place with still air, vulnerable now
                    // (the launch grace is a launch thing, not a teleport thing). Screenshots and the
                    // mirror spec stand hazards up in front of the balloon with this.
                    string[] parts = a.Split('=');
                    if (parts.Length != 2
                        || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                        || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float alt))
                        return "error:bad-arg:warp";
                    _state.X = x;
                    _state.Altitude = alt;
                    _state.VelocityX = 0f;
                    _state.VelocityY = 0f;
                    _state.HurtTick = -SimTuning.InvulnTicks;
                    return "ok:" + SimRules.HashHex(_state);
                }

                case "hash":
                    return SimRules.HashHex(_state);
                case "state":
                    return StateJson();

                case "settings":
                    return a.Length == 0 ? AppSettings.Describe() : ApplySetting(a);

                case "reset":
                {
                    ulong seed = DefaultSeed;
                    if (a.Length > 0 && !TryParseSeed(a, out seed))
                        return "error:bad-arg:reset";
                    _state = CreateState(seed);
                    _runSeed = _state.Seed;
                    _input = SimInput.None;
                    _pendingBallast = false;
                    Paused = true;
                    return "ok:" + SimRules.HashHex(_state);
                }

                case "set":
                    return HandleSet(a);

                case "step":
                {
                    int count = 1;
                    if (a.Length > 0 && (!int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out count) || count < 1))
                        return "error:bad-arg:step";
                    count = Math.Min(count, MaxStepsPerCall);
                    for (int i = 0; i < count; i++)
                        StepOnce();
                    return SimRules.HashHex(_state);
                }

                case "pause":
                    Paused = true;
                    return "ok:paused=1";
                case "resume":
                    Paused = false;
                    return "ok:paused=0";

                case "restart":
                {
                    RestartRun();
                    return "ok:" + SimRules.HashHex(_state);
                }

                case "advance":
                {
                    AdvanceRun();
                    return "ok:" + SimRules.HashHex(_state);
                }

                case "deliver":
                case "fly":
                {
                    int seconds = 120;
                    if (a.Length > 0 && (!int.TryParse(a, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds) || seconds < 1))
                        return "error:bad-arg:deliver";
                    seconds = Math.Min(seconds, 300);
                    bool delivered = Autopilot.Deliver(_state, LevelCatalog.Get(_state.LevelId), seconds, hazards: name == "fly");
                    return delivered ? "ok:delivered" : "error:not-delivered";
                }

                default:
                    return "error:unknown-call:" + name;
            }
        }

        /// <summary>
        /// Restarts the run the way a player's R key does: same level, same seed — a replay of the same
        /// sky — with the balloon back on the launch deck and the run flags cleared. Unlike "reset" it
        /// does not pause and does not change screens, so live play resumes on the same frame it stopped.
        /// The seed is kept on purpose: two runs of a level with one seed are directly comparable.
        /// </summary>
        public void RestartRun()
        {
            string screen = _state.Screen;
            string levelId = _state.LevelId;
            _state = CreateState(_runSeed);   // the run's own seed: a restart replays the same sky
            _state.Screen = screen;
            _state.LevelId = levelId;
            _input = SimInput.None;
            _pendingBallast = false;
        }

        /// <summary>
        /// Moves to the next authored level — the R key after a delivery. The course list wraps, so the
        /// last level sends the player back to the first for another flight. A fresh run: launch deck,
        /// full ballast, cleared run flags, on a seed derived from the run's own (a playthrough from the
        /// same first seed replays identically, but level two is not a replay of level one's sky).
        /// Like RestartRun it neither pauses nor changes screens.
        /// </summary>
        public void AdvanceRun()
        {
            int index = 0;
            for (int i = 0; i < LevelCatalog.Levels.Length; i++)
                if (LevelCatalog.Levels[i].Id == _state.LevelId)
                    index = i;

            int nextIndex = (index + 1) % LevelCatalog.Levels.Length;
            LevelDef next = LevelCatalog.Levels[nextIndex];
            // Pure integer mix — never object hashcodes, which differ per process and would make the
            // same playthrough seed grow a different sky on desktop than in the browser.
            _runSeed = Sim.NextSeed(_runSeed ^ ((ulong)(nextIndex + 1) * 0x9E3779B97F4A7C15UL));

            string screen = _state.Screen;
            _state = CreateState(_runSeed);
            _state.Screen = screen;
            _state.LevelId = next.Id;
            _input = SimInput.None;
            _pendingBallast = false;
        }

        /// <summary>
        /// Records a screen change request. The game applies it and writes the real screen id into state, so
        /// a caller cannot pretend the game is on a screen that does not exist: <c>screen()</c> only ever
        /// reports what is actually being drawn.
        /// </summary>
        string RequestScreen(string id)
        {
            if (id.Length == 0 || id.Length > 32)
                return "error:bad-arg:screen";
            _state.RequestedScreen = id;
            return "ok:requested=" + id;
        }

        /// <summary>
        /// Switches the authored level and re-launches the balloon: position, temperature and ballast
        /// reset, seed and tick stay so a scripted replay remains comparable across the switch.
        /// Public because the level-select screen's row press is the same verb as the test's
        /// <c>__GAME.level()</c> — one code path for choosing a course.
        /// </summary>
        public string SelectLevel(string id)
        {
            if (!LevelCatalog.TryGet(id, out LevelDef level))
                return "error:unknown-level:" + id;

            _state.LevelId = level.Id;
            _state.X = 0f;
            _state.VelocityX = 0f;
            _state.Altitude = SimTuning.DeckTopAltitude;
            _state.VelocityY = 0f;
            _state.Temperature = SimTuning.TempAmbient;
            _state.BallastHeld = SimTuning.BagCapacity;
            return "ok:level=" + level.Id;
        }

        /// <summary>
        /// <c>settings</c> reads the mirrored settings string; <c>settings=key=value</c> applies one.
        /// Assist is the setting the running state can feel, so it syncs immediately — a paused player
        /// who turns it on lands a calmer sky on the very next frame.
        /// </summary>
        string ApplySetting(string arg)
        {
            int eq = arg.IndexOf('=');
            if (eq <= 0)
                return "error:bad-arg:settings";
            string key = arg.Substring(0, eq).Trim().ToLowerInvariant();
            string value = arg.Substring(eq + 1).Trim().ToLowerInvariant();
            string? error = AppSettings.TrySet(key, value);
            if (error != null)
                return error;
            _state.WindAssist = AppSettings.WindAssist;
            return "ok:" + AppSettings.Describe();
        }

        string HandleSet(string arg)
        {
            int eq = arg.IndexOf('=');
            if (eq <= 0)
                return "error:bad-arg:set";
            string key = arg.Substring(0, eq).Trim().ToLowerInvariant();
            string raw = arg.Substring(eq + 1).Trim().ToLowerInvariant();
            bool on = raw == "1" || raw == "true" || raw == "on";
            bool off = raw == "0" || raw == "false" || raw == "off";
            if (!on && !off)
                return "error:bad-arg:set";

            switch (key)
            {
                case "heat":
                    _input.Heat = on;
                    return "ok:heat=" + (on ? 1 : 0);
                case "ballast":
                    if (on)
                        _pendingBallast = true;
                    return "ok:ballast=" + (on ? 1 : 0);
                default:
                    return "error:unknown-key:" + key;
            }
        }

        static bool TryParseSeed(string text, out ulong seed) =>
            ulong.TryParse(text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text.Substring(2) : text,
                           NumberStyles.HexNumber, CultureInfo.InvariantCulture, out seed);

        /// <summary>
        /// The authored hazards as live coordinates — <c>rock:x,y;bird:x,y;storm:cx,cy</c> at the
        /// current tick, from the same position functions the sim collides against and the renderer
        /// draws. The JS side locates a rock through this to script a collision without duplicating
        /// any level data.
        /// </summary>
        string DescribeHazards()
        {
            LevelDef level = LevelCatalog.Get(_state.LevelId);
            StringBuilder sb = new StringBuilder(96);
            for (int i = 0; i < level.Rocks.Length; i++)
            {
                (float x, float alt) = level.RockAt(i, _state.Tick);
                AppendPoint(sb, "rock", x, alt);
            }
            for (int i = 0; i < level.Birds.Length; i++)
            {
                (float x, float alt, int _) = level.BirdAt(i, _state.Tick);
                AppendPoint(sb, "bird", x, alt);
            }
            for (int i = 0; i < level.Storms.Length; i++)
            {
                StormZone zone = level.Storms[i];
                AppendPoint(sb, "storm", (zone.FromX + zone.ToX) / 2f, (zone.FromAltitude + zone.ToAltitude) / 2f);
            }
            return sb.Length == 0 ? "none" : sb.ToString();
        }

        static void AppendPoint(StringBuilder sb, string kind, float x, float altitude)
        {
            if (sb.Length > 0)
                sb.Append(';');
            sb.Append(kind).Append(':')
              .Append(x.ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
              .Append(altitude.ToString("0.0", CultureInfo.InvariantCulture));
        }

        string StateJson()
        {
            LevelDef level = LevelCatalog.Get(_state.LevelId);
            StringBuilder sb = new StringBuilder(128);
            sb.Append('{')
              .Append("\"version\":\"").Append(Sim.ProtocolVersion).Append("\",")
              .Append("\"screen\":\"").Append(_state.Screen).Append("\",")
              .Append("\"tick\":").Append(_state.Tick.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"seed\":\"").Append(_state.Seed.ToString("x16", CultureInfo.InvariantCulture)).Append("\",")
              .Append("\"paused\":").Append(Paused ? "true" : "false").Append(',')
              .Append("\"heat\":").Append(_input.Heat ? "true" : "false").Append(',')
              .Append("\"heating\":").Append(_state.Heating ? "true" : "false").Append(',')
              .Append("\"temperature\":").Append(_state.Temperature.ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"altitude\":").Append(_state.Altitude.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"velocityY\":").Append(_state.VelocityY.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"ballastHeld\":").Append(_state.BallastHeld.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"drops\":").Append(_state.DropsSeen.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"x\":").Append(_state.X.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"velocityX\":").Append(_state.VelocityX.ToString("0.00", CultureInfo.InvariantCulture)).Append(',')
              .Append("\"level\":\"").Append(_state.LevelId).Append("\",")
              .Append("\"delivered\":").Append(_state.Delivered ? "true" : "false").Append(',')
              .Append("\"complete\":").Append(_state.Complete ? "true" : "false").Append(',')
              .Append("\"stars\":").Append(_state.Stars.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"completedAtTick\":").Append(_state.CompletedAtTick.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"bounces\":").Append(_state.Bounces.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"hearts\":").Append(_state.Hearts.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"recoveries\":").Append(_state.Recoveries.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append("\"band\":").Append(FlightGuidance.BandIndex(_state, level)).Append(',')
              .Append("\"windDirection\":").Append(level.BandAt(_state.Altitude).Direction).Append(',')
              .Append("\"approach\":\"").Append(FlightGuidance.Approach(_state, level)).Append("\",")
              .Append("\"heatTrend\":\"").Append(FlightGuidance.HeatTrend(_state)).Append("\",")
              .Append("\"cargo\":\"").Append(_state.Cargo.ToString().ToLowerInvariant()).Append("\",")
              .Append("\"cargoStatus\":\"").Append(_state.CargoStatus).Append("\",")
              .Append("\"pickupTick\":").Append(_state.PickupTick).Append(',')
              .Append("\"recipient\":\"").Append(level.DestinationName).Append("\",")
              .Append("\"platforms\":").Append(level.Platforms.Length).Append(',')
              .Append("\"expression\":\"").Append(FlightGuidance.Expression(_state)).Append("\",")
              .Append("\"atCeiling\":").Append(_state.Altitude >= level.WorldHeight - .5f ? "true" : "false").Append(',')
              .Append("\"windAssist\":").Append(_state.WindAssist ? "true" : "false").Append(',')
              .Append("\"settings\":\"").Append(AppSettings.Describe()).Append("\",")
              .Append("\"hash\":\"").Append(SimRules.HashHex(_state)).Append('"')
              .Append('}');
            return sb.ToString();
        }
    }
}
