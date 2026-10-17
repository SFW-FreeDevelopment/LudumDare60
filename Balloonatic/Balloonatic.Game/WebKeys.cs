using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Balloonatic
{
    /// <summary>
    /// The WebGL head's keyboard. KNI's own Blazor key pipeline goes quiet after the first mouse
    /// click (the DOM listeners survive, but the key state stops updating through the sync interop
    /// the window module uses) — and every player clicks the canvas before they press anything, so
    /// the browser head keeps its own key table instead: shell.js forwards raw keyCodes through the
    /// bridge's async path, which a probe proved survives clicks. The desktop head never touches
    /// this; it has a real keyboard.
    ///
    /// A fast press (a test's synthetic tap, a snatched keystroke) can arrive and leave between two
    /// 60 Hz polls and vanish unseen, so downs also <em>latch</em>: every press is visible to at
    /// least one Update, which the game prunes once per frame after reading it.
    /// </summary>
    public static class WebKeys
    {
        static readonly HashSet<int> DownCodes = new HashSet<int>();
        static readonly HashSet<int> FreshCodes = new HashSet<int>();

        /// <summary>Record a press or release by DOM keyCode.</summary>
        public static void Set(int keyCode, bool isDown)
        {
            if (isDown)
            {
                DownCodes.Add(keyCode);
                FreshCodes.Add(keyCode);
            }
            else
            {
                DownCodes.Remove(keyCode);
            }
        }

        /// <summary>Forget the fresh-press latches — the game calls this once per Update, after it
        /// has read every key, so a press can only ever bridge the poll it landed between.</summary>
        public static void PruneFresh() => FreshCodes.Clear();

        /// <summary>Forget every key — the browser fires this when the tab loses focus, so keys held
        /// through an alt-tab cannot stick pressed forever.</summary>
        public static void Clear()
        {
            DownCodes.Clear();
            FreshCodes.Clear();
        }

        public static bool IsDown(Keys key) => Held(DownCodes, key);

        /// <summary>A press seen since the last PruneFresh, even if the key is already back up.</summary>
        public static bool FreshDown(Keys key) => Held(FreshCodes, key);

        static bool Held(HashSet<int> codes, Keys key)
        {
            int code = key switch
            {
                Keys.Enter => 13,
                Keys.Escape => 27,
                Keys.Space => 32,
                Keys.Back => 8,
                Keys.Up => 38,
                Keys.Down => 40,
                Keys.Left => 37,
                Keys.Right => 39,
                Keys.W => 87,
                Keys.A => 65,
                Keys.S => 83,
                Keys.D => 68,
                Keys.B => 66,
                Keys.R => 82,
                _ => -1,
            };
            return code >= 0 && codes.Contains(code);
        }
    }
}
