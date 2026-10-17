using System.Globalization;
using System.Text;

namespace Balloonatic.Core
{
    /// <summary>
    /// Everything the simulation knows. Mutable, advanced only by <see cref="Sim.Step"/>, and hashed
    /// so tests can prove determinism. Fields for the balloon model land in initiative I2; the I0
    /// harness needs just enough live state to prove the bridge, the hash and the draw path work.
    /// </summary>
    public sealed class SimState
    {
        public const string ScreenScaffold = "scaffold";
        public const string ScreenStyle = "style";

        public ulong Seed;
        public int Tick;
        public int HeatTicks;
        public int BallastDropped;

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
            HeatTicks = HeatTicks,
            BallastDropped = BallastDropped,
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
        /// Advances one fixed step. Initiative I2 replaces the body with the balloon model (temperature
        /// to lift, cooling, gravity, drag, ballast weight, wind coupling); I0 only proves that a step
        /// is deterministic, observable through the hash, and driven by injected input.
        /// </summary>
        public static void Step(SimState state, SimInput input)
        {
            state.Seed = Sim.NextSeed(state.Seed);
            state.Tick++;
            if (input.Heat)
                state.HeatTicks++;
            if (input.DropBallast && state.BallastDropped < int.MaxValue)
                state.BallastDropped++;
        }

        /// <summary>FNV-1a over every state field. Order-sensitive on purpose: any state change moves the hash.</summary>
        public static ulong Hash(SimState state)
        {
            ulong h = OffsetBasis;
            h = Mix(h, state.Seed);
            h = Mix(h, (ulong)state.Tick);
            h = Mix(h, (ulong)state.HeatTicks);
            h = Mix(h, (ulong)state.BallastDropped);
            h = Mix(h, state.Heating ? 1UL : 0UL);
            foreach (char c in state.Screen)
                h = Mix(h, c);
            return h;
        }

        const ulong OffsetBasis = 14695981039346656037UL;
        const ulong Prime = 1099511628211UL;

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
