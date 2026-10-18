using System;
using Balloonatic.Core;
using Balloonatic.Rendering;
using Microsoft.Xna.Framework;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The screen the game is actually played on: a scrolling world where altitude is steering. The camera
    /// is a pure function of state (position plus velocity look-ahead), which gives two things at once —
    /// it looks where the player is going because it never smooths something that is already continuous,
    /// and a stepped frame is byte-stable for screenshots without any render-side state to reset. Drawing
    /// this screen reads state and never writes it; the world screenshot test proves that with a hash.
    /// </summary>
    sealed class WorldScreen : IUiScreen
    {
        // Half a second of look-ahead: enough that +75 px/s of band wind pulls the view ~37 px ahead of
        // the craft, so speed is legible as framing, not only as a gauge. Reduced Motion gives this up
        // entirely — the look-ahead is the one thing the camera does on its own initiative.
        const float LookAheadSeconds = 0.5f;

        // Corner pause tab: the mouse-only player's way out of the sky, and the only button that lives
        // on the flight screen (everything else the flight says, the HUD already says).
        static readonly Rectangle PauseTab = UiLayout.Pause;

        readonly UiHost _host;
        bool _pauseHover;
        bool _resultHover;

        public WorldScreen(UiHost host) => _host = host;

        public void Tick(UiFrame f)
        {
            Rectangle result=UiLayout.Result;
            _resultHover=f.PointIsIn(result.X,result.Y,result.Width,result.Height);
            if (_host.Flight.Complete && (f.Enter || f.Click && _resultHover)) {
                if (_host.Flight.Delivered) _host.GoTo(SimState.ScreenWorldMap);
                else _host.RestartFlight();
                return;
            }
            _pauseHover = f.PointIsIn(PauseTab.X, PauseTab.Y, PauseTab.Width, PauseTab.Height);
            if (f.Back || (f.Click && _pauseHover))
                _host.GoTo(SimState.ScreenPause);
        }

        // World margins either side of the course, so launch and delivery islands sit inside the frame.
        const int WorldMargin = 120;

        public string Id => SimState.ScreenWorld;

        public void Draw(DesignCanvas canvas, SimState state)
        {
            LevelDef level = LevelCatalog.Get(state.LevelId);
            (float camX, float camAlt) = Camera(state, level);

            SkyBackdrop.Draw(canvas, camX, camAlt, Clock(state));
            DrawStormBanks(canvas, level, state, camX, camAlt);       // I9: angry cloud banks at mid-cloud depth
            DrawWindStreaks(canvas, level, state, camX, camAlt);
            DrawIslands(canvas, level, state, camX, camAlt);
            DrawHazards(canvas, level, state, camX, camAlt);          // I9: rocks and birds, before the balloon
            DrawFallingBags(canvas, state, camX, camAlt);
            DrawBalloon(canvas, state, camX, camAlt);
            DrawCelebration(canvas, state, camX, camAlt);            // I12: post-delivery star sparkle
            DrawHud(canvas, level, state);
            DrawPauseTab(canvas);
            DrawLandingFeedback(canvas, level, state, camX, camAlt);
            canvas.Outline(new Rectangle(0, 0, Sim.DesignWidth, Sim.DesignHeight), Theme.Ink);
        }

        /// <summary>The pause tab, drawn like every other raised thing: ink, wood, lit when pointed at.</summary>
        void DrawPauseTab(DesignCanvas canvas)
        {
            canvas.CutRectangle(PauseTab.X, PauseTab.Y, PauseTab.Width, PauseTab.Height, 2, Theme.Ink);
            canvas.CutRectangle(PauseTab.X + 1, PauseTab.Y + 1, PauseTab.Width - 2, PauseTab.Height - 2, 1,
                _pauseHover ? Theme.WoodLight : Theme.WoodMid);
            canvas.Fill(PauseTab.X + 8, PauseTab.Y + 6, 4, 16, Theme.TagCream);
            canvas.Fill(PauseTab.X + 18, PauseTab.Y + 6, 4, 16, Theme.TagCream);
        }

        /// <summary>
        /// The two moments the landing rule has to speak for itself: a bounce says why in one red tag,
        /// and a delivery stops the world behind a panel with the three stars it earned (PLAN I5 — the
        /// reason the player failed or what they won, always on screen).
        /// </summary>
        void DrawLandingFeedback(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            int font=Kit.Body(canvas);
            if (!state.Complete) {
                string approach=FlightGuidance.Approach(state,level);
                string message=state.Recoveries>0 && state.Tick-state.RecoveryTick<180 ? "TRY ANOTHER APPROACH"
                    : approach=="landed" ? "LANDED: DRIFT IN" : approach=="fast" ? "TAP HEAT TO BRAKE" : approach=="rising" ? "RELEASE EARLY TO SETTLE" : approach=="gentle" ? "GENTLE APPROACH" : "";
                if(message.Length>0) {
                    Kit.Panel(canvas,new Rectangle(96,282,448,40));
                    PixelFont.DrawCenteredShadowed(canvas,message,320,290,approach is "gentle" or "landed"?Theme.GrassLight:Theme.TagCream,font);
                }
                if(state.Bounces>0 && state.Tick-state.BounceTick<SimTuning.BounceReasonSteps)
                    PixelFont.DrawCenteredShadowed(canvas,"TOO FAST!",Round(ScreenX(state.X,camX)),Round(ScreenY(state.Altitude,camAlt))-100,Theme.FlameRed,font);
                return;
            }
            canvas.Fill(0,0,640,360,Theme.PauseVeil);
            Kit.Panel(canvas,new Rectangle(96,72,448,232));
            PixelFont.DrawCenteredShadowed(canvas,state.Delivered?"DELIVERY!":"TAKE A BREATHER",320,88,Theme.TagCream,Kit.Heading(canvas));
            PixelFont.DrawCenteredShadowed(canvas,state.Delivered?level.DeliveredMessage:"CHARLOTTE NEEDS A REST",320,128,Theme.TagCream,font);
            if(state.Delivered) {
                int seconds=state.CompletedAtTick/Sim.StepHz;
                PixelFont.DrawCenteredShadowed(canvas,"TIME "+seconds+"S / PAR "+level.ParSeconds+"S",320,164,Theme.SignCream,font);
                for(int i=0;i<3;i++) Sprites.Draw(canvas,i<state.Stars?"star-on":"star-off",268+i*40,200,scale:4);
            } else {
                Sprites.Draw(canvas,"balloon-worried-0",290,154);
            }
            Kit.Button(canvas,UiLayout.Result,state.Delivered?(state.LevelId==LevelCatalog.Levels[2].Id?"HOMEWARD >":"NEXT SKY >"):"TRY AGAIN",
                _resultHover?Kit.UiState.Hover:Kit.UiState.Rest,focused:true);
        }

        /// <summary>
        /// Camera centre in world coordinates. The only smoothing the game needs: the balloon's own
        /// velocity leads the view, and the edges of the world hold it back politely.
        /// </summary>
        static (float camX, float camAlt) Camera(SimState state, LevelDef level)
        {
            // Reduced Motion (I7 setting): the look-ahead goes away, so the camera only ever sits where
            // the balloon is — no lead, no sway, the calmest framing the game can show.
            float look = AppSettings.ReducedMotion ? 0f : LookAheadSeconds;
            float worldLeft = -WorldMargin;
            float worldRight = level.CourseLength + WorldMargin;
            float targetX = state.X + state.VelocityX * look;

            // Reserve the instrument strip above the envelope, including on a fast approach.
            // Bounded look-ahead keeps the camera calm and never hides the passenger behind HUD.
            float verticalLead = Clamp(state.VelocityY * look * 0.5f, -6f, 6f);
            float camAlt = Clamp(state.Altitude + verticalLead + 24f, 0f, level.WorldHeight + 32f);

            // Framing bias toward the goal: until the delivery is behind the balloon, the camera leans
            // downwind, so the course ahead stays in frame and the islands are visible from launch.
            // Pure state again — no render memory, byte-stable screenshots.
            float courseBias = state.X < level.CourseLength ? Sim.DesignWidth / 4 : 0;
            float camX = Clamp(targetX + courseBias, worldLeft + Sim.DesignWidth / 2, worldRight - Sim.DesignWidth / 2);

            // I9 hurt shake: one pixel, half a second, a pure function of (Tick - HurtTick) so a
            // stepped frame stays byte-stable. It is a bruise, not an earthquake — and Reduced
            // Motion keeps it out entirely, leaving the balloon's own flicker to carry the news.
            int hurtAge = state.Tick - state.HurtTick;
            if (!AppSettings.ReducedMotion && state.Hearts < SimTuning.HeartsStart
                && hurtAge >= 0 && hurtAge < SimTuning.ShakeTicks)
                camX += (hurtAge / 3 % 2 == 0 ? 1f : -1f) * (1f - hurtAge / (float)SimTuning.ShakeTicks);

            return (camX, camAlt);
        }

        static float ScreenX(float worldX, float camX) => worldX - camX + Sim.DesignWidth / 2f;
        static float ScreenY(float altitude, float camAlt) => Sim.DesignHeight / 2f - (altitude - camAlt);

        public static string SignsStatus(SimState state)
        {
            LevelDef level=LevelCatalog.Get(state.LevelId);
            (float camX,float camAlt)=Camera(state,level);
            float groundY=ScreenY(0,camAlt);
            var signs=new System.Collections.Generic.List<string>{
                WorldSigns.Island("HOME",ScreenX(0,camX),groundY).Json("HOME"),
                WorldSigns.Island(level.DestinationName,ScreenX(level.CourseLength,camX),groundY).Json(level.DestinationName)};
            foreach(RestDeck deck in level.Platforms)signs.Add(WorldSigns.Perch(ScreenX(deck.X,camX),ScreenY(deck.Altitude,camAlt)).Json("BREEZE PERCH"));
            return "["+string.Join(",",signs)+"]";
        }

        /// <summary>Text and only text keeps the coarse grid: type must not wobble by half a pixel.</summary>
        static int Round(float v) => (int)MathF.Round(v);

        static void DrawWindStreaks(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            // One chevron train per band, locked to the world grid: count reads strength, the point
            // reads direction, and the ink under-draw keeps them legible on every sky value.
            for (int b = 0; b < level.Bands.Length; b++)
            {
                WindBand band = level.Bands[b];
                Color color = BandTint(b);
                float middle = (band.FromAltitude + band.ToAltitude) / 2f;
                float y = ScreenY(middle - (middle - camAlt) * 0.08f, camAlt);   // drift toward the camera plane
                if (y < 6 || y > Sim.DesignHeight - 6)
                    continue;

                // Signed slow drift, wrapped into one 160 px cell: the trains repeat every 160 px of
                // world, so wrapping the phase never visibly jumps — and it carries the band's sign,
                // so each train slides the way its air blows at its own crawl.
                float travel = (AppSettings.ReducedMotion ? 0 : Clock(state)) / (float)Sim.StepHz * LevelDef.WindSpeed(band.Strength) * 0.04f * band.Direction;
                float phase = WrapF(travel, 160f);
                for (int wx = -640; wx <= (int)level.CourseLength + 640; wx += 160)
                {
                    float x = ScreenX(wx, camX) + phase;
                    if (x < -40 || x > Sim.DesignWidth + 10)
                        continue;
                    Kit.WindChevrons(canvas, x, y, band.Direction, band.Strength, color, 2);
                }
            }
        }

        static void DrawIslands(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            float groundY = ScreenY(0f, camAlt);

            DrawIsland(canvas, level, state, camX, groundY, worldX: 0, name: "HOME", launch: true);
            DrawIsland(canvas, level, state, camX, groundY, worldX: (int)level.CourseLength, name: level.DestinationName, launch: false);
            foreach (RestDeck deck in level.Platforms)
            {
                float x = ScreenX(deck.X, camX), y = ScreenY(deck.Altitude, camAlt);
                Sprites.Draw(canvas, WorldArt.Perch(level), x - deck.HalfWidth, y - 3);
                WindBand wind = level.BandAt(deck.Altitude + 1f);
                Kit.Windsock(canvas, x + 20, y, wind.Direction, wind.Strength);
                WorldSigns.Perch(x,y).Draw(canvas);
            }
        }

        static void DrawIsland(
            DesignCanvas canvas, LevelDef level, SimState state, float camX, float groundY,
            float worldX, string name, bool launch)
        {
            float x = ScreenX(worldX, camX);
            if (x < -90 || x > Sim.DesignWidth + 90)
                return;

            // The sheet island: plank deck (exactly as wide as the sim's landing tolerance), trees
            // standing on the surface, grass cap, rock tapering to a point — delivery carries gold
            // bunting and a waterfall. The deck line sits 16 design px below the sprite's top edge,
            // so the draw offset puts it where it always was: groundY - 38.
            Sprites.Draw(canvas, launch ? "island-launch" : "island-delivery", x - 60f, groundY - 54f);
            // Buildings sit on the rear edge; the clear timber deck in front is the landing surface.
            Sprites.Draw(canvas, WorldArt.Landmark(level, launch, Clock(state)), x - 6f, groundY - 105f);

            WorldSigns.Island(name,x,groundY).Draw(canvas);
            if (launch && state.CargoStatus == "waiting")
                Sprites.Draw(canvas, WorldArt.Parcel(level.Package), x - 36, groundY - 53);
            if (!launch)
            {
                Sprites.Draw(canvas, "recipient", state.Delivered ? 1 : 0, x + 37, groundY - 64);
                if (state.Delivered) Sprites.Draw(canvas, WorldArt.Parcel(level.Package), x + 22, groundY - 53);
                WorldSigns.Recipient(canvas,level,state.Delivered,x+10,groundY-120);
            }

            // The windsock flies off the windward post of the deck — the truth about this altitude,
            // in world. The sheet sock inflates from its post downwind; a band blowing left mirrors it.
            WindBand band = level.BandAt(1f);
            // Two frames of cloth snap, only for bands strong enough to snap cloth; the frozen
            // results clock stops the flutter with everything else, because the world stands still.
            string sock = band.Strength switch
            {
                1 => "windsock-1",
                _ => $"windsock-{band.Strength}-{WorldArt.SockFrame(state)}",
            };
            Vector2 sockSize = Sprites.Size(sock);
            float postX = x + (band.Direction >= 0 ? -50 : 50);
            Sprites.Draw(canvas, sock, band.Direction >= 0 ? postX - 1 : postX - sockSize.X + 1,
                         groundY - 37, band.Direction < 0);
        }

        /// <summary>
        /// Every dropped sandbag falls where it was released: the sim recorded the release (tick,
        /// position), and a constant visual fall rate derived from the clock draws it — the player
        /// must see the bag leave the basket, because the bag IS the brake, and an invisible brake
        /// teaches nothing. Gone at the ground or after a few seconds, whichever screen first.
        /// </summary>
        static void DrawFallingBags(DesignCanvas canvas, SimState state, float camX, float camAlt)
        {
            const float FallSpeed = 70f;      // design px/s, visual only — no physics pretence
            int clock = Clock(state);
            for (int i = 0; i < state.DropTick.Length; i++)
            {
                if (state.DropsSeen <= i)
                    continue;                 // slot never used
                int age = clock - state.DropTick[i];
                if (age < 0 || age > clock)   // clock is frozen on the results screen: a bag released
                    continue;                 // before the freeze hangs on screen, as the world stops
                float alt = state.DropAltitude[i] - FallSpeed * age / Sim.StepHz;
                if (alt < 0f)
                    continue;                 // it landed off-screen; the ground owns it now
                Vector2 size = Sprites.Size("bag-falling");
                float bx = ScreenX(state.DropX[i], camX) - size.X / 2;
                float by = ScreenY(alt, camAlt) - size.Y / 2;
                Sprites.Draw(canvas, "bag-falling", bx, by);

                // I12 sand puffs: the release shakes a three-mote cloud that spreads for a fifth of a
                // second, and the fall trails a single grain every other tick — the sand is visibly
                // leaving the basket, which is the brake being spent. Pure clock math, no new state.
                if (AppSettings.ReducedMotion)continue;
                if (age < 12)
                {
                    float ry = ScreenY(state.DropAltitude[i], camAlt);
                    float spread = 1f + age / 4f;
                    int[] offX = { -4, 3, 0 }, offY = { -2, -3, 4 };
                    for (int p = 0; p < 3; p++)
                        canvas.Fill(ScreenX(state.DropX[i], camX) + offX[p] * spread,
                            ry + offY[p] * spread, 2, 2, age < 6 ? Theme.SandTan : Theme.WickerDark);
                }
                else if (age % 2 == 0)
                {
                    canvas.Fill(bx + size.X / 2 - 1, by - 3, 1, 1, Theme.SandTan);
                }
            }
        }

        /// <summary>
        /// I9 storm banks: the art <em>is</em> the box — the row of angry clouds is drawn from the
        /// zone's own bounds, so where the sprites end is exactly where the buffet stops. Beneath
        /// the middle, a three-chevron arrow shows which way the bank is pushing this very tick,
        /// read from the same square wave the sim is feeling.
        /// </summary>
        static void DrawStormBanks(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            for (int i = 0; i < level.Storms.Length; i++)
            {
                StormZone zone = level.Storms[i];
                float midAlt = (zone.FromAltitude + zone.ToAltitude) / 2f;
                Vector2 size = Sprites.Size("storm-cloud");
                for (float cx = zone.FromX + size.X / 2f; cx < zone.ToX; cx += size.X - 10f)
                    Sprites.Draw(canvas, "storm-cloud",
                        ScreenX(cx, camX) - size.X / 2f, ScreenY(midAlt, camAlt) - size.Y / 2f);

                float push = level.StormWindAt((zone.FromX + zone.ToX) / 2f, midAlt, state.Tick);
                float centre=ScreenX((zone.FromX+zone.ToX)/2f,camX);
                PixelFont.DrawCenteredShadowed(canvas,"GUST",Round(centre),Round(ScreenY(zone.ToAltitude,camAlt)-28),Theme.StarGold,2);
                Kit.WindChevrons(canvas,centre-14,ScreenY(zone.FromAltitude,camAlt)+8,push>=0?1:-1,3,Theme.StarGold,2);
            }
        }

        /// <summary>
        /// I9 rocks and birds. Both drawn from the same closed-form tick functions the collision
        /// test calls, so a rock's sprite and its temper are the same number, and the bird flaps on
        /// the shared decoration clock — stopped when the run is, like everything else.
        /// </summary>
        static void DrawHazards(DesignCanvas canvas, LevelDef level, SimState state, float camX, float camAlt)
        {
            for (int i = 0; i < level.Rocks.Length; i++)
            {
                (float x, float alt) = level.RockAt(i, state.Tick);
                Vector2 size = Sprites.Size("rock");
                Sprites.Draw(canvas, "rock", ScreenX(x, camX) - size.X / 2f, ScreenY(alt, camAlt) - size.Y / 2f);
            }
            for (int i = 0; i < level.Birds.Length; i++)
            {
                (float x, float alt, int dir) = level.BirdAt(i, state.Tick);
                Vector2 size = Sprites.Size("bird-0");
                Sprites.Draw(canvas, "bird", WorldArt.BirdFrame(state),
                    ScreenX(x, camX) - size.X / 2f, ScreenY(alt, camAlt) - size.Y / 2f, flip: dir < 0);
            }
        }

        static void DrawBalloon(DesignCanvas canvas, SimState state, float camX, float camAlt)
        {
            // The craft sits at its true world position; when the camera leads it, the offset reads as
            // speed. The sheet balloon is anchored by its basket bottom on the altitude line, so it
            // visibly touches the deck at touchdown instead of hovering a hand above it. The burner
            // flickers only while the sim says heat is on — three frames, clock-driven, deterministic.
            float x = ScreenX(state.X, camX);
            float foot = ScreenY(state.Altitude, camAlt) + 1f;
            int frame = WorldArt.BurnerFrame(state);
            Vector2 size = Sprites.Size("balloon-0");

            // I12 basket sway: the load leans one px into fast climbs and the other into fast drops,
            // still when the air is still — velocity in, so it is the same pixel in every replay, and
            // off entirely under Reduced Motion.
            if (!AppSettings.ReducedMotion && !state.Complete)
                x += MathF.Round(Math.Clamp(state.VelocityY / -120f, -1f, 1f));

            // I9 invulnerability: for the grace window after a hit the balloon blinks, from a pure
            // (Tick - HurtTick) phase so stepped frames stay byte-stable. Reduced Motion dims her
            // through the window instead of blinking it — same news, gentler for the eyes.
            int hurtAge = state.Tick - state.HurtTick;
            bool hurtWindow = state.Hearts < SimTuning.HeartsStart && hurtAge >= 0 && hurtAge < SimTuning.InvulnTicks;
            if (hurtWindow && !AppSettings.ReducedMotion && hurtAge / 4 % 2 == 1)
                return;
            Color? tint = hurtWindow && AppSettings.ReducedMotion ? Theme.HurtDim : null;

            string expression = FlightGuidance.Expression(state);
            string sprite = expression == "happy" ? "balloon-happy" : expression == "worried" ? "balloon-worried" : "balloon";
            Sprites.Draw(canvas, sprite, frame, x - size.X / 2, foot - size.Y, tint: tint);
            for(int bag=0;bag<state.BallastHeld;bag++)
                Sprites.Draw(canvas, "bag-falling", x + (bag == 0 ? -22 : 16), foot - 12);
            if (state.Cargo != CargoKind.None)
                Sprites.Draw(canvas, WorldArt.Parcel(state.Cargo), x + 25, foot - 14);

            // I12 burner puffs: while heat is on, three small puffs vent from the envelope crown on a
            // rolling 30-tick cycle — pure from the decoration clock, palette clouds, and gone the
            // moment the burner is. They read as hot air escaping, which is exactly what is happening.
            if (state.Heating && !state.Complete && !AppSettings.ReducedMotion)
            {
                int clock = Clock(state);
                float crown = foot - size.Y - 1f;
                for (int p = 0; p < 3; p++)
                {
                    int age = (clock + p * 10) % 30;
                    float jitter = ((p * 7919 + age * 31) % 7) - 3;
                    float w = 1.5f + age * 0.09f;
                    Color puff = age < 10 ? Theme.CloudWhite : age < 20 ? Theme.CloudLavender : Theme.CloudShade;
                    canvas.Fill(x + jitter - w / 2f, crown - age * 0.55f, w, w, puff);
                }
            }
        }

        /// <summary>Sparkle offsets: eight honest integer directions, no sines needed for a party.</summary>
        static readonly (int Dx, int Dy)[] SparkleDirs =
            { (2, 1), (2, -1), (-2, 1), (-2, -1), (1, 2), (1, -2), (-1, 2), (-1, -2) };

        /// <summary>
        /// The delivery party (I12): for a few seconds after a landing goes well, star motes drift out
        /// from around the envelope — gold, cream and cloud-white, blinking on a tick parity. Runs off
        /// <c>Tick - CompletedAtTick</c>, so it animates while the world is live and freezes exactly
        /// where a paused screenshot caught it. Reduced Motion draws the ring steady: same celebration,
        /// no twinkle.
        /// </summary>
        static void DrawCelebration(DesignCanvas canvas, SimState state, float camX, float camAlt)
        {
            if (!state.Complete || !state.Delivered)
                return;
            int age = state.Tick - state.CompletedAtTick;
            if (age < 0 || age > 240)
                return;

            bool calm = AppSettings.ReducedMotion;
            float bx = ScreenX(state.X, camX);
            float by = ScreenY(state.Altitude + 34f, camAlt);   // envelope crown height, where the joy is
            for (int i = 0; i < SparkleDirs.Length; i++)
            {
                if (!calm && (age + i) % 6 >= 4)
                    continue;                                   // each mote hides two ticks in six
                int phase = calm ? 6 : (age + i * 9) % 18;
                float r = 16f + phase * 2.4f;
                int w = phase > 8 ? 2 : 1;
                Color c = i % 3 == 0 ? Theme.StarGold : i % 3 == 1 ? Theme.TagCream : Theme.CloudWhite;
                canvas.Fill(bx + SparkleDirs[i].Dx * r * 0.6f - w / 2f,
                            by + SparkleDirs[i].Dy * r * 0.5f - w / 2f, w, w, c);
            }
        }

        /// <summary>
        /// The clock the decorations run on: live while flying, stopped the instant the run completes.
        /// The sim keeps ticking after a delivery (its own determinism), but a results screen behind
        /// which clouds still sail and the flame still flickers tells the player the run is not over.
        /// </summary>
        static int Clock(SimState state) => WorldArt.Clock(state);

        // Hearts: 3 at launch, one per rock or bird (I9). "heart-hud-off" is the empty slot the
        // moment the sim spends one.
        const int HeartsTotal = 3;

        /// <summary>
        /// The instrument panel of the concept sheet: a wooden crate panel top-left carrying Charlotte's
        /// portrait, hearts, the star rating and a gift-box delivery count; the wind readout top-centre;
        /// and the burner's own heat gauge down the right edge, capped with a live flame. Every number
        /// shown is mirrored in the __GAME state — heating, temperature, ballastHeld, stars, delivered,
        /// level, and the altitude that picks the band — so tests assert the HUD's data, not its pixels.
        /// </summary>
        static void DrawHud(DesignCanvas canvas, LevelDef level, SimState state)
        {
            int clock = Clock(state);

            // ---- crate panel, top left: who is flying, and how she is doing ----
            Kit.Panel(canvas, new Rectangle(8, 8, 118, 44));
            string expression = FlightGuidance.Expression(state);
            Sprites.Draw(canvas, expression == "happy" ? "portrait-happy" : expression == "worried" ? "portrait-worried" : "portrait", 13, 11);
            canvas.CutRectangle(39, 10, 68, 32, 1, Theme.Dim);              // inner well behind the counters
            for (int i = 0; i < HeartsTotal; i++)
                Sprites.Draw(canvas, i < state.Hearts ? "heart-hud" : "heart-hud-off", 42 + i * 16, 12);
            int font=Kit.Body(canvas);
            Sprites.Draw(canvas, WorldArt.Parcel(level.Package), 104, 28);
            Kit.WindLadder(canvas,state,level);
            Kit.Panel(canvas,new Rectangle(140,8,324,64));
            PixelFont.DrawShadowed(canvas,state.Delivered?level.DeliveredMessage:state.Cargo==CargoKind.None?level.CollectObjective:level.CarryObjective,
                146,12,Theme.TagCream,font);
            if(!state.Complete) PixelFont.DrawShadowed(canvas,FlightGuidance.ControlHint(state,level),146,42,Theme.TagCream,font);

            // ---- heat gauge, right edge: a brass instrument capped with a live flame, the way the
            // sheet draws the burner's own thermometer ----
            var gauge = new Rectangle(Sim.DesignWidth - 26, 142, 16, 74);
            canvas.CutRectangle(gauge.X, gauge.Y, gauge.Width, gauge.Height, 2, Theme.Ink);
            canvas.Fill(gauge.X + 1, gauge.Y + 1, gauge.Width - 2, gauge.Height - 2, Theme.Dim);
            canvas.Fill(gauge.X + 2, gauge.Y + 1, 1, gauge.Height - 2, Theme.RockSlate);           // metal rim
            canvas.Fill(gauge.Right - 3, gauge.Y + 1, 1, gauge.Height - 2, Theme.RockDark);

            float fill = (state.Temperature - SimTuning.TempAmbient) / (SimTuning.TempMax - SimTuning.TempAmbient);
            int inner = gauge.Height - 4;
            int filled = (int)Math.Round(MathHelper.Clamp(fill, 0f, 1f) * inner);
            for (int row = 0; row < filled; row++)
            {
                // Gradient along the fill: how close the fabric is to too hot, the way any thermometer reads.
                float position = row / (float)Math.Max(1, inner);
                Color color = position > 0.72f ? Theme.FlameRed : position > 0.4f ? Theme.FlameOrange : Theme.FlameYellow;
                canvas.Fill(gauge.X + 2, gauge.Bottom - 2 - row, gauge.Width - 4, 1, color);
                canvas.Fill(gauge.Right - 3, gauge.Bottom - 2 - row, 1, 1, Color.Lerp(color, Theme.Ink, 0.35f)); // glass shade
            }

            for (int tick = 0; tick <= 4; tick++)
                canvas.Fill(gauge.X + 2, gauge.Bottom - 2 - inner * tick / 4, 2, 1, Theme.Cream);
            float neutral = (SimTuning.Gravity + state.BallastHeld * SimTuning.BallastPerBag) / SimTuning.LiftGain;
            int neutralY = gauge.Bottom - 2 - (int)((neutral - SimTuning.TempAmbient) / (SimTuning.TempMax - SimTuning.TempAmbient) * inner);
            canvas.Fill(gauge.X - 3, neutralY, gauge.Width + 6, 1, Theme.Cream);

            canvas.Fill(gauge.X + 5, gauge.Bottom, 6, 2, Theme.RockSlate);   // burner nozzle under the well
            canvas.Fill(gauge.X + 6, gauge.Bottom + 2, 4, 1, Theme.RockDark);

            // The cap flame is still at rest and flickers while the burner fires; it rides on the
            // decoration clock, so a results screen behind a frozen run keeps a frozen flame.
            // Reduced Motion holds it to the still frame — flicker is motion too.
            int flameFrame = state.Heating && !AppSettings.ReducedMotion && clock % 12 >= 6 ? 1 : 0;
            Sprites.Draw(canvas, "flame-hud", flameFrame, gauge.X + 1, gauge.Y - 12);
            PixelFont.DrawShadowed(canvas, "HEAT", 582, 224, Theme.TagCream, 2);

            // ---- ballast, bottom left: the actual sandbags aboard, in things you could touch. The
            // dropped slots keep their shape — an empty bag is still a bag on the rope. ----
            for(int i=0;i<SimTuning.BagCapacity;i++)
                Sprites.Draw(canvas,"bag-falling",12+i*16,324,scale:2,tint:i<state.BallastHeld?(Color?)null:Theme.WoodDark);
            PixelFont.DrawShadowed(canvas,"B: LIFT",52,330,Theme.Ink,font);
            const int left=220,width=350,y=344;
            canvas.Fill(left,y+3,width,2,Theme.RockDark);
            canvas.Fill(left+width-2,y-3,3,10,Theme.StarGold);
            int mark=left+(int)(Clamp(state.X,0,level.CourseLength)/level.CourseLength*(width-4));
            canvas.Fill(mark,y,4,8,Theme.Cream);
        }


        static int Index(LevelDef level, WindBand band)
        {
            for (int i = 0; i < level.Bands.Length; i++)
                if (level.Bands[i].FromAltitude == band.FromAltitude)
                    return i;
            return 0;
        }

        static Color BandTint(int bandIndex) =>
            bandIndex <= 0 ? Theme.WindLowBand : bandIndex == 1 ? Theme.WindMidBand : Theme.WindHighBand;

        static int Hash(int i, int salt)
        {
            unchecked
            {
                uint h = (uint)i * 2654435761u + (uint)salt * 40503u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (int)(h & 0x7FFFFFFF);
            }
        }

        static int Wrap(int value, int span)
        {
            int wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }

        static float WrapF(float value, float span)
        {
            float wrapped = value % span;
            return wrapped < 0 ? wrapped + span : wrapped;
        }

        static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
    }
}
