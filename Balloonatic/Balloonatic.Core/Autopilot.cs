using System;

namespace Balloonatic.Core
{
    /// <summary>
    /// A pilot that only does what a player is allowed to do: choose heat, and thereby choose altitude,
    /// and spend sandbags. Two uses, one code path. Every level ships with a solvability test that flies
    /// it, so no authored wind layout can be unbeatable (PLAN.md 10); and when Wind Assist is on in I7,
    /// this closes the loop gently so a player who holds the burner gets carried rather than punished.
    /// </summary>
    public static class Autopilot
    {        /// <summary>
        /// Pulse-width heat control toward an altitude target: a PD loop around the hover temperature
        /// (the one where lift equals weight, ballast included). The commanded temperature rises with
        /// altitude error and falls with vertical speed — start easing off before the band arrives,
        /// because the envelope keeps going, but do not ease off until the *approach* says so, because
        /// the envelope stores heat. Bang-bang badly overshoots and a pure P-loop orbits below target;
        /// this is also the controller behind Wind Assist in I7.
        /// </summary>
        public static SimInput TowardAltitude(SimState state, float targetAltitude)
        {
            float hover = HoverTemperature(state);
            float desired = hover
                + PositionGain * (targetAltitude - state.Altitude)
                - VelocityGain * state.VelocityY;
            return new SimInput { Heat = state.Temperature < desired };
        }

        /// <summary>Temperature at which lift equals weight for the current loadout.</summary>
        static float HoverTemperature(SimState state) =>
            (SimTuning.Gravity + state.BallastHeld * SimTuning.BallastPerBag) / SimTuning.LiftGain;

        /// <summary>Extra commanded temperature per design pixel of altitude error: 140 px short ≈ full burner.</summary>
        const float PositionGain = 0.0025f;

        /// <summary>Commanded temperature shed per px/s of climb: damps the approach so the hold settles in the band.</summary>
        const float VelocityGain = 0.005f;

        /// <summary>
        /// Flies <paramref name="state"/> at the strongest downwind band of <paramref name="level"/> for
        /// up to <paramref name="maxSeconds"/> and reports the downwind distance reached. A level is
        /// solvable when this clears its course length with margin.
        /// </summary>
        public static float FlyDownwind(SimState state, LevelDef level, float maxSeconds)
        {
            WindBand carrying = CarryingBand(level);
            float target = (carrying.FromAltitude + carrying.ToAltitude) / 2f;

            int steps = (int)(maxSeconds * Sim.StepHz);
            for (int i = 0; i < steps; i++)
            {
                SimInput input = TowardAltitude(state, target);
                SimRules.Step(state, input, level, hazards: false);   // a wind proof, not a dodge
                if (state.X >= level.CourseLength)
                    return state.X;
            }

            return state.X;
        }

        static WindBand CarryingBand(LevelDef level)
        {
            WindBand best = level.Bands[0];
            foreach (WindBand band in level.Bands)
            {
                if (band.Direction > 0 && LevelDef.WindSpeed(band.Strength) > LevelDef.WindSpeed(best.Strength))
                    best = band;
            }
            return best;
        }

        /// <summary>
        /// Mid-altitude of the strongest contrary (upwind) band, for flying back to a deck after an
        /// overshoot. Falls back to <paramref name="fallback"/> (cruise) when the level has no return air.
        /// </summary>
        static float ReturnBandTarget(LevelDef level, float fallback)
        {
            WindBand best = default;
            bool found = false;
            foreach (WindBand band in level.Bands)
            {
                if (band.Direction < 0 && (!found || LevelDef.WindSpeed(band.Strength) > LevelDef.WindSpeed(best.Strength)))
                {
                    best = band;
                    found = true;
                }
            }
            return found ? (best.FromAltitude + best.ToAltitude) / 2f : fallback;
        }

        static float BandCenter(WindBand band) => (band.FromAltitude + band.ToAltitude) / 2f;

        /// <summary>
        /// Flies <paramref name="state"/> onto the delivery deck, using only burner heat and (at most)
        /// one sandbag, and reports whether it landed there within <paramref name="maxSeconds"/>. It is
        /// both the solvability proof for a level's landing (DeliveryTests) and the teaching ghost's
        /// flight in I9.
        ///
        /// There is no flyable glideslope in this physics — descent rate is fixed by loadout and the
        /// wind owns the horizontal — so no hand-drawn pattern survives contact with a zigzag level.
        /// What does survive is what good pilots actually do at every deck: decide by imagining the
        /// alternatives. From each moment the autopilot rolls the next few seconds forward once per
        /// candidate idea — hold the carrying air, hold the lowest air, wait just above the deck,
        /// commit under it, climb to the return band — scores each imagined ending, and flies the
        /// winner one beat before imagining again. The score is the landing read: where the basket
        /// would touch down, how high is left to fall, and whether the imagined flight even reaches
        /// the deck at all. One sandbag is spent on entering the final zone: the loaded sink outruns
        /// the soft-landing rule, and arriving soft is the point — one bag stays aboard, because a
        /// lighter balloon has a gentler final descent. Every loadout remains landable.
        /// </summary>
        public static bool Deliver(SimState state, LevelDef level, float maxSeconds, bool hazards = false, Action<SimInput>? record = null)
        {
            float cruise = BandCenter(CarryingBand(level));
            // A homeward floor must first be crossed in carrying air; a landing-only horizon
            // otherwise keeps choosing to hover near the launch. Use the first forward band.
            bool homewardFloor = level.Bands[0].Direction < 0;
            if (homewardFloor)
                foreach (WindBand band in level.Bands)
                    if (band.Direction > 0) { cruise = BandCenter(band); break; }
            float lowest = Math.Max(BandCenter(level.Bands[0]), SimTuning.DeckTopAltitude + LowestFloor);
            float returnAlt = ReturnBandTarget(level, cruise);

            // The candidate ideas. Everything the autopilot can imagine is an altitude to hold — the
            // same verbs the player has. Planes of air, a hover a hand's width over the deck, the
            // committed fall under it, and the way home.
            float[] ideas = { cruise, lowest, SimTuning.DeckTopAltitude + DeckHover, SimTuning.DeckTopAltitude - DeckSink, returnAlt };

            int steps = (int)(maxSeconds * Sim.StepHz);
            bool braked = false;
            float chosen = lowest;
            int decideIn = 0;
            for (int i = 0; i < steps; i++)
            {
                if (decideIn <= 0)
                {
                    chosen = homewardFloor && state.X < level.CourseLength - 220f ? cruise : BestIdea(state, level, ideas, hazards);
                    decideIn = ReplanSeconds * Sim.StepHz;
                }
                decideIn--;

                SimInput input = TowardAltitude(state, chosen);

                // One sandbag is spent crossing into the final zone, wherever in it the balloon first
                // arrives: the loaded descent outruns the soft-landing rule. From here down the
                // envelope flies light.
                float toDeck = level.CourseLength - state.X;
                if (!braked && toDeck <= FinalApproachLength && state.BallastHeld > 1)
                {
                    input.DropBallast = true;
                    braked = true;
                }

                record?.Invoke(input);
                SimRules.Step(state, input, level, hazards);
                if (state.Delivered)
                    return true;
            }

            return state.Delivered;
        }

        /// <summary>
        /// Roll each candidate idea forward from <paramref name="state"/> and return the altitude of
        /// the one whose imagined ending scores best. The rollouts are the same deterministic steps as
        /// everything else; nothing here reads anything but state and level, so the chosen flight path
        /// is reproducible to the step.
        /// </summary>
        static float BestIdea(SimState state, LevelDef level, float[] ideas, bool hazards)
        {
            float bestScore = float.MinValue;
            float bestAlt = ideas[0];
            int horizon = LookaheadSeconds * Sim.StepHz;
            foreach (float alt in ideas)
            {
                SimState sim = state.Clone();
                for (int k = 0; k < horizon && !sim.Delivered; k++)
                    SimRules.Step(sim, TowardAltitude(sim, alt), level, hazards);
                float score = ScoreLanding(sim, level) - (state.Hearts - sim.Hearts) * 2000f;
                if (sim.Complete && !sim.Delivered) score -= 20000f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestAlt = alt;
                }
            }
            return bestAlt;
        }

        /// <summary>
        /// The landing read of an imagined moment: how far the touchdown point is from the deck, how
        /// much useful height is left, and how close to the deck the air is being flown. Landing on
        /// the ground short of the deck is a failure of imagination; touching down on the deck is
        /// worth everything.
        /// </summary>
        static float ScoreLanding(SimState s, LevelDef level)
        {
            if (s.Delivered)
                return 4000f - s.Tick * 0.02f;   // earlier is better, but landing is what matters

            float deckCentre = level.CourseLength;
            float score = -Math.Abs(s.X - deckCentre) * 0.04f;

            if (s.Altitude <= SimTuning.GroundAltitude)
                return score - 900f;             // arrived on the dirt: the read said this idea misses

            // Where the basket would touch if the burner stayed shut from here (lag-aware: the fall
            // starts at the speed it has, not at terminal).
            float sink = SinkRate(s);
            float effSink = 0.5f * (sink + Math.Max(0f, -s.VelocityY));
            float fall = effSink > 2f ? Math.Max(0f, s.Altitude - SimTuning.DeckTopAltitude) / effSink : 999f;
            float touchdown = s.X + s.VelocityX * fall;
            if (level.Bands[0].Direction < 0) {
                // Coasting crosses opposing bands: extrapolating today's horizontal speed
                // invents a landing point. Predict through the same cooling/wind rules instead.
                SimState coast=s.Clone();
                for(int k=0;k<20*Sim.StepHz && coast.Altitude>SimTuning.DeckTopAltitude && !coast.Complete;k++)
                    SimRules.Step(coast,SimInput.None,level,hazards:false);
                touchdown=coast.X;
            }
            score -= Math.Abs(touchdown - deckCentre);

            // Height is only useful as landing material; too high, and it is just weather.
            score -= Math.Abs(s.Altitude - (SimTuning.DeckTopAltitude + LowUsefulHeight)) * 0.6f;
            score -= s.Bounces * 120f;
            if (touchdown > 0f)
                score += 200f;                   // any live read beats drifting forever at altitude
            return score;
        }

        /// <summary>How far ahead the imagined flight is rolled. Long enough to see a whole final —
        /// a committed fall from the low bands to the deck takes under a dozen honest seconds.</summary>
        const int LookaheadSeconds = 12;

        /// <summary>How many real seconds an idea is trusted before the world is imagined again.</summary>
        const int ReplanSeconds = 1;

        /// <summary>Altitude above the deck that counts as "useful height" — enough to land from, not enough to be weather.</summary>
        const float LowUsefulHeight = 45f;

        /// <summary>The waiting hover: high enough to see the deck come, low enough to drop onto it.</summary>
        const float DeckHover = 22f;

        /// <summary>Current loaded sink rate if the burner stays shut: 0 when buoyant. The number the
        /// landing is planned around, and the reason sandbags are how a pilot lands.</summary>
        static float SinkRate(SimState state) =>
            Math.Max(0f, (SimTuning.Gravity + state.BallastHeld * SimTuning.BallastPerBag
                          - state.Temperature * SimTuning.LiftGain) / SimTuning.Drag);

        /// <summary>Course length beyond which the landing brake is not yet worth spending; inside it,
        /// the balloon flies light so the descent rate is one the soft-landing rule can be met with.</summary>
        const float FinalApproachLength = 450f;

        /// <summary>The lowest band's centre is only trusted as a final altitude this far above the deck.</summary>
        const float LowestFloor = 15f;

        /// <summary>How far below the deck plane the final aim point sits: enough that the basket plane,
        /// not the envelope, is what stops against the deck.</summary>
        const float DeckSink = 8f;
    }
}
