using System;
using System.Globalization;
using System.Text;

namespace Balloonatic.Core
{
    /// <summary>
    /// Everything the simulation knows. Mutable, advanced only by <see cref="SimRules.Step"/>, and hashed
    /// so tests can prove determinism. Altitudes and velocities are design pixels at 60 Hz; temperature is
    /// 0..1 where <see cref="SimTuning.TempAmbient"/> is the air's own floor.
    /// </summary>
    public sealed class SimState
    {
        public const string ScreenScaffold = "scaffold";
        public const string ScreenStyle = "style";
        public const string ScreenWorld = "world";

        public ulong Seed;
        public int Tick;

        /// <summary>Envelope temperature, 0..1. The single number the burner moves.</summary>
        public float Temperature = SimTuning.TempAmbient;

        /// <summary>Vertical velocity, design px per second. Positive is up.</summary>
        public float VelocityY;

        /// <summary>Height above the ground plane, design px. Never negative.</summary>
        public float Altitude;

        /// <summary>Sandbags still aboard. Dropping one is irreversible for the level.</summary>
        public int BallastHeld = SimTuning.BagCapacity;

        /// <summary>Downwind distance travelled from the launch column, design px. Wind owns this, not the player.</summary>
        public float X;

        /// <summary>Horizontal velocity, design px/s. Chases the current band's wind with inertia (I3).</summary>
        public float VelocityX;

        /// <summary>Which authored level this run is on. Bands, gusts and the course length come from it.</summary>
        public string LevelId = LevelCatalog.DefaultId;

        /// <summary>Heat as applied on the last step. Kept in state so rendering can show the flame
        /// without reading a device, and so the hash covers it.</summary>
        public bool Heating;

        public string Screen = ScreenScaffold;

        /// <summary>
        /// A screen the caller asked for that the game has not applied yet. Deliberately outside the hash:
        /// it is pending input rather than state, and it is always empty after a step.
        /// </summary>
        public string? RequestedScreen;

        public SimState Clone() => new SimState
        {
            Seed = Seed,
            Tick = Tick,
            Temperature = Temperature,
            VelocityY = VelocityY,
            Altitude = Altitude,
            BallastHeld = BallastHeld,
            X = X,
            VelocityX = VelocityX,
            LevelId = LevelId,
            Heating = Heating,
            Screen = Screen,
        };
    }

    /// <summary>
    /// One frame of player intent. There is deliberately no horizontal axis: the balloon has no
    /// left/right control (docs/CONCEPT.md §4, critical design rule).
    /// </summary>
    public struct SimInput
    {
        public bool Heat;
        public bool DropBallast;

        public static readonly SimInput None = default;
    }

    public static class SimRules
    {
        /// <summary>
        /// Advances one fixed step of the balloon model: burner heat, cooling toward ambient, lift from
        /// temperature, weight including ballast, drag, and integration. Pure arithmetic on floats at a
        /// fixed step — no transcendental calls and no reads of anything outside <paramref name="state"/>
        /// and <see cref="SimTuning"/>, so the same inputs give bit-identical state on desktop and WASM.
        ///
        /// Horizontal velocity arrives in I3 with wind bands; the player still cannot steer sideways.
        /// </summary>
        public static void Step(SimState state, SimInput input) => Step(state, input, LevelCatalog.Get(state.LevelId));

        public static void Step(SimState state, SimInput input, LevelDef level)
        {
            float dt = Sim.StepSeconds;
            state.Seed = Sim.NextSeed(state.Seed);
            state.Tick++;
            state.Heating = input.Heat;

            if (input.Heat)
            {
                state.Temperature += SimTuning.HeatRate * dt;
                if (state.Temperature > SimTuning.TempMax)
                    state.Temperature = SimTuning.TempMax;
            }
            else
            {
                float aboveAmbient = state.Temperature - SimTuning.TempAmbient;
                if (aboveAmbient > 0f)
                {
                    // Linear in the temperature difference: cools quickly while hot, then eases off, and
                    // stops at ambient instead of freezing below it.
                    float cooling = SimTuning.CoolBase + SimTuning.CoolProportional * aboveAmbient;
                    state.Temperature -= cooling * dt;
                    if (state.Temperature < SimTuning.TempAmbient)
                        state.Temperature = SimTuning.TempAmbient;
                }
            }

            if (input.DropBallast && state.BallastHeld > 0)
                state.BallastHeld--;

            float lift = state.Temperature * SimTuning.LiftGain;
            float weight = SimTuning.Gravity + state.BallastHeld * SimTuning.BallastPerBag;
            float drag = state.VelocityY * SimTuning.Drag;

            state.VelocityY += (lift - weight - drag) * dt;
            state.Altitude += state.VelocityY * dt;

            if (state.Altitude <= SimTuning.GroundAltitude)
            {
                // On the ground the balloon sits: descending into the ground stops it, and the landing
                // judgement (soft vs crash) is I5's rule on top of this clamp.
                state.Altitude = SimTuning.GroundAltitude;
                if (state.VelocityY < 0f)
                    state.VelocityY = 0f;
            }

            // Horizontal: the air decides, with inertia. The balloon's velocity chases the wind of the
            // band it is currently inside; entering a new band is a settle over ~1 s, never a snap. This
            // is why altitude is the only steering — the player chooses which air to be in, not where to go.
            float wind = level.WindAt(state.Altitude);
            state.VelocityX += (wind - state.VelocityX) * SimTuning.WindFollowRate * dt;
            state.X += state.VelocityX * dt;
        }

        /// <summary>
        /// FNV-1a over every state field, floats mixed as their exact bit patterns. Order-sensitive on
        /// purpose: any state change moves the hash, and 0.1 px of altitude is a visible change.
        /// </summary>
        public static ulong Hash(SimState state)
        {
            ulong h = OffsetBasis;
            h = Mix(h, state.Seed);
            h = Mix(h, (ulong)state.Tick);
            h = Mix(h, Bits(state.Temperature));
            h = Mix(h, Bits(state.VelocityY));
            h = Mix(h, Bits(state.Altitude));
            h = Mix(h, (ulong)state.BallastHeld);
            h = Mix(h, Bits(state.X));
            h = Mix(h, Bits(state.VelocityX));
            h = Mix(h, state.Heating ? 1UL : 0UL);
            foreach (char c in state.Screen)
                h = Mix(h, c);
            foreach (char c in state.LevelId)
                h = Mix(h, c);
            return h;
        }

        const ulong OffsetBasis = 14695981039346656037UL;
        const ulong Prime = 1099511628211UL;

        static ulong Bits(float value) => (ulong)BitConverter.SingleToInt32Bits(value);

        static ulong Mix(ulong h, ulong value)
        {
            for (int byteIndex = 0; byteIndex < 8; byteIndex++)
            {
                h ^= (value >> (byteIndex * 8)) & 0xFF;
                h *= Prime;
            }
            return h;
        }

        public static string HashHex(SimState state) => Hash(state).ToString("x16", CultureInfo.InvariantCulture);
    }
}
