using System.Globalization;

namespace Balloonatic.Core
{
    /// <summary>
    /// Player options the menu owns (I7). These are not simulation state: the music and sound volumes
    /// touch no physics, reduced motion is a renderer promise, and the one setting the sim can feel —
    /// Wind Assist — is copied into <see cref="SimState.WindAssist"/> when a run is created so a seeded
    /// replay stays a seeded replay. Kept in the pure core so the desktop head, the WebGL head and the
    /// test bridge all read and write one copy.
    /// </summary>
    public static class AppSettings
    {
        public const int VolumeDefault = 8;
        public const int VolumeMax = 10;

        /// <summary>0..10, normalized against VolumeMax by the independent audio buses.</summary>
        public static int MusicVolume = VolumeDefault;
        public static int SoundVolume = VolumeDefault;

        /// <summary>Wind Assist: gusts are ignored, band winds still steer.</summary>
        public static bool WindAssist;

        /// <summary>
        /// Reduced motion: the camera gives up its velocity look-ahead and decorative flicker stops.
        /// Motion-sickness accommodation is a requirement, not a nicety (AGENTS.md).
        /// </summary>
        public static bool ReducedMotion;

        /// <summary>The mirrored form, read by <c>__GAME.settings()</c> so tests can assert what the screen shows.</summary>
        public static string Describe() =>
            "music=" + MusicVolume.ToString(CultureInfo.InvariantCulture)
            + ";sfx=" + SoundVolume.ToString(CultureInfo.InvariantCulture)
            + ";assist=" + (WindAssist ? 1 : 0)
            + ";motion=" + (ReducedMotion ? 1 : 0);

        /// <summary>Applies one <c>key=value</c> pair. Returns null when the pair was good.</summary>
        public static string? TrySet(string key, string value)
        {
            switch (key)
            {
                case "music":
                case "sfx":
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int volume)
                        || volume < 0 || volume > VolumeMax)
                        return "error:bad-arg:" + key;
                    if (key == "music") MusicVolume = volume; else SoundVolume = volume;
                    return null;
                case "assist":
                    if (!TryBool(value, out bool assist)) return "error:bad-arg:assist";
                    WindAssist = assist;
                    return null;
                case "motion":
                    if (!TryBool(value, out bool motion)) return "error:bad-arg:motion";
                    ReducedMotion = motion;
                    return null;
                default:
                    return "error:unknown-key:" + key;
            }
        }

        static bool TryBool(string value, out bool on)
        {
            on = value == "1" || value == "true" || value == "on";
            bool off = value == "0" || value == "false" || value == "off";
            return on || off;
        }

        /// <summary>Back to defaults — the menu's RESTORE DEFAULTS row.</summary>
        public static void RestoreDefaults()
        {
            MusicVolume = VolumeDefault;
            SoundVolume = VolumeDefault;
            WindAssist = false;
            ReducedMotion = false;
        }
    }
}
