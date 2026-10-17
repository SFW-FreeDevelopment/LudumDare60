using System.Collections.Generic;

namespace Balloonatic.Screens
{
    /// <summary>
    /// The voyage as the session remembers it (I10): which courses have been carried home, and with
    /// how many stars. This is UI-side rendering state like focus and hover — the sim hash never sees
    /// it, and only the world map reads it. A page reload starts the voyage fresh, which is exactly
    /// what a session-scoped scrap of chart paper should do.
    /// </summary>
    static class AppProgress
    {
        static readonly Dictionary<string, int> BestStars = new(System.StringComparer.OrdinalIgnoreCase);

        public static void Mark(string levelId, int stars)
        {
            BestStars.TryGetValue(levelId, out int best);
            BestStars[levelId] = stars > best ? stars : best;
        }

        public static bool Delivered(string levelId) => BestStars.ContainsKey(levelId);

        public static int StarsFor(string levelId) =>
            BestStars.TryGetValue(levelId, out int stars) ? stars : 0;
    }
}
