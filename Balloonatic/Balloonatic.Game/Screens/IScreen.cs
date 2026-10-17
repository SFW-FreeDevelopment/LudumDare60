using Balloonatic.Core;
using Balloonatic.Rendering;

namespace Balloonatic.Screens
{
    /// <summary>
    /// A screen draws itself from simulation state alone, in design pixels. Nothing reads wall clock
    /// and nothing writes state, which is what lets a Playwright run step to tick N and get the exact
    /// same pixels every time (AGENTS.md, verification loop).
    /// </summary>
    interface IScreen
    {
        /// <summary>Id reported by <c>window.__GAME.screen</c>.</summary>
        string Id { get; }

        void Draw(DesignCanvas canvas, SimState state);
    }
}
