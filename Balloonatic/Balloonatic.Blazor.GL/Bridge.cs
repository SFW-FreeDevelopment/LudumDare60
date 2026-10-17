#nullable enable

namespace Balloonatic
{
    /// <summary>
    /// The WebGL head's side of the test bridge. JavaScript reaches the harness through exactly one
    /// static entry point, so <c>window.__GAME</c> can never drift from the protocol that xunit already
    /// pins down (see <c>Balloonatic.Core.TestHarness</c>).
    /// </summary>
    public static class Bridge
    {
        [Microsoft.JSInterop.JSInvokable]
        public static string BridgeCall(string call, string arg)
        {
            BalloonaticGame? game = BalloonaticGame.Instance;
            string name = (call ?? string.Empty).Trim().ToLowerInvariant();

            if (game == null)
                return name == "ready" ? "0" : "error:not-ready";

            // Frame counters and geometry describe the renderer, not the simulation, so they live here
            // rather than in the harness: the sim must stay free of anything a head can affect.
            if (name == "frames")
                return game.FrameCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (name == "metrics")
                return game.RenderMetrics();
            if (name == "surface")
            {
                string[] parts = (arg ?? string.Empty).Split('x');
                if (parts.Length != 2
                    || !int.TryParse(parts[0], out int width)
                    || !int.TryParse(parts[1], out int height))
                    return "error:bad-arg:surface";
                game.SetSurfaceSize(width, height);
                return "ok:" + width + "x" + height;
            }
            if (name == "ready")
                return game.FirstFrameDrawn ? "1" : "0";

            return game.Harness.Handle(name, arg);
        }
    }
}
