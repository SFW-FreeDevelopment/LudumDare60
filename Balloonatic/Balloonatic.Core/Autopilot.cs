using System;

namespace Balloonatic.Core
{
    /// <summary>
    /// A pilot that only does what a player is allowed to do: choose heat, and thereby choose altitude.
    /// Two uses, one code path. Every level ships with a solvability test that flies it, so no authored
    /// wind layout can be unbeatable (PLAN.md 10); and when Wind Assist is on in I7, this closes the
    /// loop gently so a player who holds the burner gets carried rather than punished.
    /// </summary>
    public static class Autopilot
    {
        /// <summary>
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
                SimRules.Step(state, input, level);
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
    }
}
