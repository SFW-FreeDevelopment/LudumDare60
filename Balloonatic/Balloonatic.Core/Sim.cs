namespace Balloonatic.Core
{
    /// <summary>
    /// Simulation-wide constants. Rendering and input convert to these units; nothing else picks
    /// magic numbers. Design space is 640x360 logical pixels (see docs/STYLE.md).
    /// </summary>
    public static class Sim
    {
        /// <summary>Fixed simulation rate. Visuals interpolate/derive from <see cref="SimState.Tick"/>, never wall clock.</summary>
        public const int StepHz = 60;

        /// <summary>Seconds per simulation step.</summary>
        public const float StepSeconds = 1f / StepHz;

        public const int DesignWidth = 640;
        public const int DesignHeight = 360;

        /// <summary>Bridge protocol version; the Playwright suite and docs quote this value.</summary>
        public const string ProtocolVersion = "i3";

        internal static ulong NextSeed(ulong seed)
        {
            // xorshift64*: small, branch-free, identical on every platform and runtime.
            seed ^= seed >> 12;
            seed ^= seed << 25;
            seed ^= seed >> 27;
            return seed * 2685821657736338717UL;
        }
    }
}
